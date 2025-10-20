using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automalithic.Utils;
using Automalithic.Rendering;
using Automalithic.World.DC;
using Automalithic.Player;

namespace Automalithic.World
{
    /// <summary>
    /// Hauptklasse für die Voxel-Welt. Verwaltet Chunks, Streaming und Updates.
    /// Wird an die VoxelWorld Node3D in Godot angehängt.
    /// </summary>
    public partial class VoxelWorld : Node3D
    {
        // Konstanten für Voxel- und Chunk-Größen
        public const float VOXEL_LEAF_SIZE_M = 0.02f; // 2cm pro Voxel
        public const int CHUNK_DEPTH = 10; // Depth 10 für SVO
        public const int CHUNK_LEAVES_PER_AXIS = 1 << CHUNK_DEPTH; // 2^10 = 1024
        public const float CHUNK_SIZE_M = CHUNK_LEAVES_PER_AXIS * VOXEL_LEAF_SIZE_M; // 20.48m

        // Player-bezogene Konstanten
        public const int PLAYER_CHUNK_RADIUS = 1; // 3x3x3 Grid um Player
        
        // Node-Referenzen
        [Export] private NodePath playerPath = "../Player";
        private Node3D playerNode;
        private Camera3D playerCamera;
        
        // Chunk-Verwaltung
        private Dictionary<Vector3I, Chunk> loadedChunks = new Dictionary<Vector3I, Chunk>();
        private Dictionary<Vector3I, ChunkRenderer> chunkRenderers = new Dictionary<Vector3I, ChunkRenderer>();
        private HashSet<Vector3I> chunksInGeneration = new HashSet<Vector3I>();
        
        // Thread-Pool für parallele Chunk-Generierung
        private readonly object chunkLock = new object();
        
        /// <summary>
        /// Initialisierung beim Start
        /// </summary>
        public override void _Ready()
        {
            GD.Print("VoxelWorld: Initialisierung gestartet...");
            
            // Player-Referenz holen
            playerNode = GetNode<Node3D>(playerPath);
            playerCamera = playerNode.GetNode<Camera3D>("Camera3D");
            
            if (playerNode == null || playerCamera == null)
            {
                GD.PrintErr("VoxelWorld: Player oder Camera nicht gefunden!");
                return;
            }
            
            GD.Print($"VoxelWorld: Bereit. Chunk-Größe: {CHUNK_SIZE_M}m, Voxel-Größe: {VOXEL_LEAF_SIZE_M}m");
            
            // Initiale Chunk-Generierung
            UpdateChunksAroundPlayer();
        }
        
        /// <summary>
        /// Update-Loop - prüft Player-Position und lädt/entlädt Chunks
        /// </summary>
        public override void _Process(double delta)
        {
            if (playerNode == null) return;
            
            // Alle 10 Frames Chunk-Update prüfen
            if (Engine.GetProcessFrames() % 10 == 0)
            {
                UpdateChunksAroundPlayer();
            }
        }
        
        /// <summary>
        /// Aktualisiert Chunks basierend auf Player-Position
        /// </summary>
        private void UpdateChunksAroundPlayer()
        {
            Vector3 playerWorldPos = playerNode.GlobalPosition;
            Vector3I playerChunkCoord = WorldToChunkCoord(playerWorldPos);
            
            // Sammle alle Chunks, die geladen sein sollten
            HashSet<Vector3I> chunksToKeep = new HashSet<Vector3I>();
            
            // Iteriere über alle Chunks im Radius
            for (int x = -PLAYER_CHUNK_RADIUS; x <= PLAYER_CHUNK_RADIUS; x++)
            {
                for (int y = -PLAYER_CHUNK_RADIUS; y <= PLAYER_CHUNK_RADIUS; y++)
                {
                    for (int z = -PLAYER_CHUNK_RADIUS; z <= PLAYER_CHUNK_RADIUS; z++)
                    {
                        Vector3I chunkCoord = playerChunkCoord + new Vector3I(x, y, z);
                        chunksToKeep.Add(chunkCoord);
                        
                        // Lade Chunk wenn noch nicht vorhanden
                        if (!loadedChunks.ContainsKey(chunkCoord) && !chunksInGeneration.Contains(chunkCoord))
                        {
                            LoadChunkAsync(chunkCoord);
                        }
                    }
                }
            }
            
            // Entlade Chunks außerhalb des Radius
            List<Vector3I> chunksToUnload = new List<Vector3I>();
            foreach (var kvp in loadedChunks)
            {
                if (!chunksToKeep.Contains(kvp.Key))
                {
                    chunksToUnload.Add(kvp.Key);
                }
            }
            
            foreach (var coord in chunksToUnload)
            {
                UnloadChunk(coord);
            }
        }
        
        /// <summary>
        /// Lädt einen Chunk asynchron
        /// </summary>
        private async void LoadChunkAsync(Vector3I coord)
        {
            lock (chunkLock)
            {
                if (chunksInGeneration.Contains(coord)) return;
                chunksInGeneration.Add(coord);
            }
            
            // Chunk auf Background-Thread generieren
            var chunk = await Task.Run(() => GenerateChunk(coord));
            
            // Zurück zum Main-Thread für Godot-Operationen
            CallDeferred(nameof(FinalizeChunkLoading), coord, chunk);
        }
        
        /// <summary>
        /// Generiert einen Chunk (kann auf Background-Thread laufen)
        /// </summary>
        private Chunk GenerateChunk(Vector3I coord)
        {
            var chunk = new Chunk(coord);
            
            // Noise-basierte Generierung
            Vector3 worldOffset = ChunkToWorldPos(coord);
            chunk.GenerateFromNoise(worldOffset);
            
            return chunk;
        }
        
        /// <summary>
        /// Finalisiert Chunk-Loading auf Main-Thread
        /// </summary>
        private void FinalizeChunkLoading(Vector3I coord, Chunk chunk)
        {
            lock (chunkLock)
            {
                chunksInGeneration.Remove(coord);
                loadedChunks[coord] = chunk;
            }
            
            // Erstelle Renderer für den Chunk
            CreateChunkRenderer(coord, chunk);
        }
        
        /// <summary>
        /// Erstellt einen ChunkRenderer für Mesh-Darstellung
        /// </summary>
        private void CreateChunkRenderer(Vector3I coord, Chunk chunk)
        {
            // Prüfe Sichtbarkeit mit Frustum Culling
            AABB chunkBounds = GetChunkAABB(coord);
            if (!FrustumCulling.IsAABBVisible(playerCamera, chunkBounds))
            {
                return; // Nicht sichtbar, kein Mesh generieren
            }
            
            // Erstelle Renderer-Node
            var renderer = new ChunkRenderer();
            renderer.Name = $"ChunkRenderer_{coord.X}_{coord.Y}_{coord.Z}";
            renderer.Position = ChunkToWorldPos(coord);
            AddChild(renderer);
            
            chunkRenderers[coord] = renderer;
            
            // Starte Mesh-Generierung
            GenerateMeshAsync(coord, chunk, renderer);
        }
        
        /// <summary>
        /// Generiert Mesh asynchron via Dual Contouring
        /// </summary>
        private async void GenerateMeshAsync(Vector3I coord, Chunk chunk, ChunkRenderer renderer)
        {
            // Mesh-Generierung auf Background-Thread
            var meshData = await Task.Run(() =>
            {
                DualContourMeshGenerator.GenerateMeshFromSVO(
                    chunk.SvoRoot,
                    Vector3.Zero, // Relativ zum Chunk-Ursprung
                    VOXEL_LEAF_SIZE_M,
                    out var vertices,
                    out var indices,
                    out var normals
                );
                
                return new { Vertices = vertices, Indices = indices, Normals = normals };
            });
            
            // Update Mesh auf Main-Thread
            CallDeferred(nameof(UpdateChunkMesh), renderer, meshData.Vertices, meshData.Indices, meshData.Normals);
        }
        
        /// <summary>
        /// Aktualisiert das Mesh eines Chunks
        /// </summary>
        private void UpdateChunkMesh(ChunkRenderer renderer, List<Vector3> vertices, List<int> indices, List<Vector3> normals)
        {
            if (vertices.Count == 0 || indices.Count == 0)
            {
                GD.Print($"Chunk hat keine Geometrie");
                return;
            }
            
            renderer.UpdateMesh(vertices, indices, normals);
        }
        
        /// <summary>
        /// Entlädt einen Chunk
        /// </summary>
        private void UnloadChunk(Vector3I coord)
        {
            lock (chunkLock)
            {
                loadedChunks.Remove(coord);
            }
            
            // Entferne Renderer
            if (chunkRenderers.TryGetValue(coord, out var renderer))
            {
                renderer.QueueFree();
                chunkRenderers.Remove(coord);
            }
        }
        
        /// <summary>
        /// Konvertiert Welt-Position zu Chunk-Koordinate
        /// </summary>
        private Vector3I WorldToChunkCoord(Vector3 worldPos)
        {
            return new Vector3I(
                Mathf.FloorToInt(worldPos.X / CHUNK_SIZE_M),
                Mathf.FloorToInt(worldPos.Y / CHUNK_SIZE_M),
                Mathf.FloorToInt(worldPos.Z / CHUNK_SIZE_M)
            );
        }
        
        /// <summary>
        /// Konvertiert Chunk-Koordinate zu Welt-Position (linke untere Ecke)
        /// </summary>
        private Vector3 ChunkToWorldPos(Vector3I chunkCoord)
        {
            return new Vector3(
                chunkCoord.X * CHUNK_SIZE_M,
                chunkCoord.Y * CHUNK_SIZE_M,
                chunkCoord.Z * CHUNK_SIZE_M
            );
        }
        
        /// <summary>
        /// Gibt AABB eines Chunks zurück
        /// </summary>
        private AABB GetChunkAABB(Vector3I coord)
        {
            Vector3 origin = ChunkToWorldPos(coord);
            return new AABB(origin, Vector3.One * CHUNK_SIZE_M);
        }
    }
}