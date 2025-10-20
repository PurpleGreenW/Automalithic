using Godot;
using System.Collections.Generic;

namespace Automalithic.World
{
    // Hinweis: Für Editor-Unterstützung ggf. [Tool] hinzufügen, wenn du willst,
    // dass dieses Script direkt im Editor sichtbar ist.

    // Der Chunk ist ein eigenständiger Node3D. Jeder Chunk besitzt eine SVO-Root
    // und rendert sich selbst über MeshInstance3D.
    public partial class Chunk : Node3D
    {
        // Koordinaten des Chunks im Chunk-Grid (Zellenkoordinaten)
        public Vector3I Coord;

        // Sparse Voxel Octree (SVO) Root für diesen Chunk
        public Svo SvoRoot;

        // Mesh-Renderer für diesen Chunk (Kinder-Node)
        public MeshInstance3D MeshInstance;

        private bool _hasMesh = false;

        // Konstruktor ist leer – Godot erstellt das Objekt per new Chunk()
        public Chunk()
        {
            // Mesh-Instanz erzeugen und dem Chunk hinzufügen
            MeshInstance = new MeshInstance3D();
            MeshInstance.Name = "MeshInstance";
            AddChild(MeshInstance);
        }

        // Alternative Initialisierung, wenn der Chunk per Koordinate erstellt wird
        public void Initialize(Vector3I coord)
        {
            Coord = coord;

            // SVO-Wurzel initialisieren
            SvoRoot = new Svo();

            // Position des Chunks im World-Space setzen
            // CHUNK_SIZE_M kommt aus VoxelWorld (z.B. CHUNK_SIZE_M = 20.48f)
            float worldScale = (float)VoxelWorld.ChunkSizeM;
            Position = new Vector3(coord.X * worldScale, coord.Y * worldScale, coord.Z * worldScale);
        }

        public override void _Ready()
        {
            base._Ready();

            // Sicherstellen, dass MeshInstance existiert (falls Initialize() nicht aufgerufen wurde)
            if (MeshInstance == null)
            {
                MeshInstance = new MeshInstance3D();
                MeshInstance.Name = "MeshInstance";
                AddChild(MeshInstance);
            }
        }

        // Generiert SVO-Daten und anschließend das Mesh via Dual Contouring
        // origin: Welt-Offset (Startbasis für die Chunk-Generierung)
        // chunkSize: tatsächliche Chunk-Größe in World-Units
        public void Generate(Vector3 origin, float chunkSize)
        {
            // 1) SVO mit Voxels befüllen (Prototype)
            SvoRoot.GenerateDensity(origin, VoxelWorld.CHUNK_LEAVES_PER_AXIS);

            // 2) Dual Contouring Mesh aus der SVO erzeugen
            DualContourMeshGenerator.GenerateMeshFromSvo(
                SvoRoot,
                origin,
                chunkSize / VoxelWorld.CHUNK_LEAVES_PER_AXIS,
                out List<Vector3> verts,
                out List<int> inds
            );

            // 3) Mesh anwenden
            if (verts.Count > 0 && inds.Count > 0)
                BuildMesh(verts, inds);
            else
                MeshInstance.Mesh = new ArrayMesh();

            _hasMesh = verts.Count > 0 && inds.Count > 0;
        }

        private void BuildMesh(List<Vector3> verts, List<int> inds)
        {
            var am = new ArrayMesh();
            Godot.Collections.Array arrays = new Godot.Collections.Array();

            Vector3[] va = verts.ToArray();
            int[] ia = inds.ToArray();

            // Vorbereitung des Arrays (Vertex/Index, ggf. weitere Infos wie Normals/UVs später hinzufügen)
            arrays.Resize((int)ArrayMesh.ArrayType.Max + 1);
            arrays[(int)ArrayMesh.ArrayType.Vertex] = va;
            arrays[(int)ArrayMesh.ArrayType.Index] = ia;

            am.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

            MeshInstance.Mesh = am;
            _hasMesh = true;
        }

        public bool HasMesh() => _hasMesh;
    }
}