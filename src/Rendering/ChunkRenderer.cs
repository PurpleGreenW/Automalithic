using Godot;
using System.Collections.Generic;
using System.Linq;
using Automalithic.Rendering;
using Automalithic.World;

namespace Automalithic.Rendering
{
    /// <summary>
    /// Renderer-Komponente für einen einzelnen Chunk.
    /// Verwaltet Mesh-Darstellung, Kollision und Debug-Visualisierung.
    /// </summary>
    public partial class ChunkRenderer : Node3D
    {
        // ==================== Private Felder ====================
        /// <summary>
        /// MeshInstance für die visuelle Darstellung
        /// </summary>
        private MeshInstance3D _meshInstance;
        
        /// <summary>
        /// Kollisionskörper für Physik
        /// </summary>
        private StaticBody3D _staticBody;
        
        /// <summary>
        /// Kollisionsform
        /// </summary>
        private CollisionShape3D _collisionShape;
        
        /// <summary>
        /// Material für den Chunk
        /// </summary>
        private StandardMaterial3D _chunkMaterial;
        
        /// <summary>
        /// Debug-Wireframe-Darstellung
        /// </summary>
        private MeshInstance3D _debugWireframe;
        
        /// <summary>
        /// Flag für Debug-Darstellung
        /// </summary>
        private bool _showDebugWireframe;
        
        // ==================== Öffentliche Properties ====================
        /// <summary>
        /// Chunk-Koordinaten
        /// </summary>
        public Vector3I ChunkCoord { get; set; }
        
        /// <summary>
        /// Sichtbarkeitsstatus
        /// </summary>
        public new bool IsVisible() => _meshInstance?.Visible ?? false;

        // ==================== Godot Lifecycle ====================
        /// <summary>
        /// Initialisierung der Renderer-Komponenten
        /// </summary>
        public override void _Ready()
        {
            SetupMeshInstance();
            SetupPhysics();
            SetupDebugVisualization();
        }

        // ==================== Setup-Methoden ====================
        /// <summary>
        /// Erstellt und konfiguriert die MeshInstance
        /// </summary>
        private void SetupMeshInstance()
        {
            _meshInstance = new MeshInstance3D
            {
                Name = "ChunkMesh",
                CastShadow = GeometryInstance3D.ShadowCastingSetting.On
            };
            AddChild(_meshInstance);
            
            // Standard-Material erstellen
            _chunkMaterial = new StandardMaterial3D
            {
                VertexColorUseAsAlbedo = true,
                AlbedoColor = Colors.White,
                Roughness = 0.8f,
                Metallic = 0.0f
            };
        }

        /// <summary>
        /// Erstellt Physik-Komponenten für Kollision
        /// </summary>
        private void SetupPhysics()
        {
            _staticBody = new StaticBody3D
            {
                Name = "ChunkPhysics",
                CollisionLayer = 1, // Terrain-Layer
                CollisionMask = 0xFFFFFFFF // Mit allem kollidieren
            };
            AddChild(_staticBody);
            
            _collisionShape = new CollisionShape3D
            {
                Name = "ChunkCollider"
            };
            _staticBody.AddChild(_collisionShape);
        }

        /// <summary>
        /// Erstellt Debug-Visualisierungskomponenten
        /// </summary>
        private void SetupDebugVisualization()
        {
            _debugWireframe = new MeshInstance3D
            {
                Name = "DebugWireframe",
                Visible = false,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
            };
            AddChild(_debugWireframe);
            
            // Wireframe-Material
            var wireframeMaterial = new StandardMaterial3D
            {
                VertexColorUseAsAlbedo = true,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                AlbedoColor = new Color(0, 1, 0, 0.5f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                NoDepthTest = true,
                GrowAmount = 0.01f
            };
            _debugWireframe.MaterialOverride = wireframeMaterial;
        }

        // ==================== Mesh-Update-Methoden ====================
        /// <summary>
        /// Aktualisiert das Mesh mit neuen Vertex-Daten
        /// </summary>
        public void UpdateMesh(List<Vector3> vertices, List<int> indices, 
            List<Vector3> normals = null, List<Vector2> uvs = null, 
            List<Color> colors = null)
        {
            if (vertices == null || vertices.Count == 0 || 
                indices == null || indices.Count == 0)
            {
                ClearMesh();
                return;
            }
            
            // ArrayMesh erstellen
            var arrayMesh = new ArrayMesh();
            var arrays = new Godot.Collections.Array();
            arrays.Resize((int)Mesh.ArrayType.Max);
            
            // Vertex-Daten zuweisen
            arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
            arrays[(int)Mesh.ArrayType.Index] = indices.ToArray();
            
            // Optionale Daten
            if (normals != null && normals.Count == vertices.Count)
                arrays[(int)Mesh.ArrayType.Normal] = normals.ToArray();
            
            if (uvs != null && uvs.Count == vertices.Count)
                arrays[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
            
            if (colors != null && colors.Count == vertices.Count)
                arrays[(int)Mesh.ArrayType.Color] = colors.ToArray();
            
            // Surface hinzufügen
            arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            arrayMesh.SurfaceSetMaterial(0, _chunkMaterial);
            
            // Mesh zuweisen
            _meshInstance.Mesh = arrayMesh;
            
            // Kollision aktualisieren
            UpdateCollision(vertices, indices);
            
            // Debug-Info
            GD.Print($"Chunk {ChunkCoord}: Mesh aktualisiert mit {vertices.Count} Vertices, {indices.Count / 3} Triangles");
        }

        /// <summary>
        /// Aktualisiert das Mesh aus Godot Arrays (für Compute Shader Integration)
        /// </summary>
        public void UpdateMeshFromArrays(Godot.Collections.Array meshArrays)
        {
            if (meshArrays == null || meshArrays.Count == 0)
            {
                ClearMesh();
                return;
            }
            
            // ArrayMesh erstellen
            var arrayMesh = new ArrayMesh();
            
            // Vertices und Indices extrahieren für Kollision
            var vertices = new List<Vector3>();
            var indices = new List<int>();
            
            if (meshArrays[(int)Mesh.ArrayType.Vertex] is Vector3[] vertArray)
            {
                vertices.AddRange(vertArray);
            }
            
            if (meshArrays[(int)Mesh.ArrayType.Index] is int[] indexArray)
            {
                indices.AddRange(indexArray);
            }
            
            // Surface hinzufügen
            arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, meshArrays);
            arrayMesh.SurfaceSetMaterial(0, _chunkMaterial);
            
            // Mesh zuweisen
            _meshInstance.Mesh = arrayMesh;
            
            // Kollision aktualisieren
            if (vertices.Count > 0 && indices.Count > 0)
            {
                UpdateCollision(vertices, indices);
            }
        }

        /// <summary>
        /// Löscht das aktuelle Mesh
        /// </summary>
        private void ClearMesh()
        {
            _meshInstance.Mesh = null;
            _collisionShape.Shape = null;
        }

        /// <summary>
        /// Aktualisiert die Kollisionsform basierend auf dem Mesh
        /// </summary>
        private void UpdateCollision(List<Vector3> vertices, List<int> indices)
        {
            if (vertices.Count < 3 || indices.Count < 3)
            {
                _collisionShape.Shape = null;
                return;
            }
            
            // Trimesh-Kollision erstellen
            var shape = new ConcavePolygonShape3D();
            var faces = new Vector3[indices.Count];
            
            for (int i = 0; i < indices.Count; i++)
            {
                faces[i] = vertices[indices[i]];
            }
            
            shape.SetFaces(faces);
            _collisionShape.Shape = shape;
        }

        // ==================== Sichtbarkeit & LOD ====================
        /// <summary>
        /// Setzt die Sichtbarkeit des Chunks
        /// </summary>
        public new void SetVisible(bool visible)
        {
            if (_meshInstance != null)
                _meshInstance.Visible = visible;
            
            if (_staticBody != null)
                _staticBody.ProcessMode = visible 
                    ? Node.ProcessModeEnum.Inherit 
                    : Node.ProcessModeEnum.Disabled;
        }

        /// <summary>
        /// Aktiviert/Deaktiviert Debug-Wireframe
        /// </summary>
        public void SetDebugWireframe(bool show)
        {
            _showDebugWireframe = show;
            if (_debugWireframe != null)
                _debugWireframe.Visible = show;
        }

        /// <summary>
        /// Erstellt Debug-Wireframe für Chunk-Bounds
        /// </summary>
        public void UpdateDebugBounds(Aabb bounds)
        {
            if (_debugWireframe == null) return;
            
            // Box-Mesh für Bounds erstellen
            var boxMesh = new BoxMesh
            {
                Size = bounds.Size
            };
            
            _debugWireframe.Mesh = boxMesh;
            _debugWireframe.Position = bounds.Position + bounds.Size * 0.5f;
        }

        // ==================== Material & Rendering ====================
        /// <summary>
        /// Setzt ein custom Material für den Chunk
        /// </summary>
        public void SetMaterial(Material material)
        {
            if (_meshInstance?.Mesh != null && _meshInstance.Mesh.GetSurfaceCount() > 0)
            {
                _meshInstance.SetSurfaceOverrideMaterial(0, material);
            }
        }

        /// <summary>
        /// Aktiviert/Deaktiviert Schatten
        /// </summary>
        public void SetCastShadows(bool cast)
        {
            if (_meshInstance != null)
            {
                _meshInstance.CastShadow = cast 
                    ? GeometryInstance3D.ShadowCastingSetting.On 
                    : GeometryInstance3D.ShadowCastingSetting.Off;
            }
        }

        // ==================== Cleanup ====================
        /// <summary>
        /// Aufräumen beim Entfernen
        /// </summary>
        public override void _ExitTree()
        {
            ClearMesh();
            _chunkMaterial?.Dispose();
        }
    }
}