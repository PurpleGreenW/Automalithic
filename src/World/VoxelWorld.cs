using Godot;
using System;
using System.Collections.Generic;

namespace Automalithic.World
{
    /// <summary>
    /// World-Manager: verwaltet Chunks, lädt/generiert sie basierend auf der Spielerposition.
    /// </summary>
    public partial class VoxelWorld : Node3D
    {
        // Chunk-Größe in Metern (Startwert: 20.48 m entsprechend 1024 Leaves bei 0.02 m Leaf-Größe)
        public const float ChunkSizeM = 20.48f;
        public const int SvoRes = 1024; // SVO-Resolution pro Chunk (Depth 10)
        public const int PlayerChunkRadius = 1; // Radius um den Spieler (in Chunks)

        // Datenspeicher
        private readonly Dictionary<Vector3Int, Chunk> _chunks = new();
        private Node3D _chunksRoot;

        public override void _Ready()
        {
            GD.Print("Automalithic: VoxelWorld ready. Chunk-Grid auf Empfang vorbereitet.");

            // Root-Node für alle Chunks (optional, für Ordnung in der Scene)
            _chunksRoot = new Node3D { Name = "ChunksRoot" };
            AddChild(_chunksRoot);
        }

        /// <summary>
        /// Aktualisiert die geladenen Chunks basierend auf der Spielerposition.
        /// </summary>
        /// <param name="worldPos">Weltposition des Spielers</param>
        public void UpdatePlayerPosition(Vector3 worldPos)
        {
            Vector3Int playerChunk = new(
                Mathf.FloorToInt(worldPos.X / ChunkSizeM),
                Mathf.FloorToInt(worldPos.Y / ChunkSizeM),
                Mathf.FloorToInt(worldPos.Z / ChunkSizeM)
            );

            // Lade Chunks im Radius um den Spieler
            for (int dx = -PlayerChunkRadius; dx <= PlayerChunkRadius; dx++)
            {
                for (int dy = -PlayerChunkRadius; dy <= PlayerChunkRadius; dy++)
                {
                    for (int dz = -PlayerChunkRadius; dz <= PlayerChunkRadius; dz++)
                    {
                        var coord = new Vector3Int(playerChunk.x + dx, playerChunk.y + dy, playerChunk.z + dz);
                        if (!_chunks.ContainsKey(coord))
                        {
                            // Erzeuge Chunk-Node und bw.
                            var chunk = new Chunk();
                            chunk.Initialize(coord, ChunkSizeM, LeafSizeMeters);
                            _chunks.Add(coord, chunk);
                            // Chunk als Child der Welt hinzufügen (Chunk ist jetzt ein Node3D)
                            AddChild(chunk);
                        }
                    }
                }
            }

            // Optional: Chunks außerhalb des Radius entladen
        }
    }
}