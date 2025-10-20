using Godot;

using Automalithic.World.Enums;

namespace Automalithic.World
{
    /// <summary>
    /// Repräsentiert einen einzelnen Chunk in der Voxel-Welt.
    /// Jeder Chunk hat seine eigene SVO-Struktur und Biom-Information.
    /// </summary>
    public class Chunk
    {
        public Vector3I Coord { get; private set; }
        public SVO SvoRoot { get; private set; }
        public EBiomeType BiomeType { get; set; }
        public bool IsGenerated { get; private set; }
        public bool HasMesh { get; set; }
        
        // Chunk-Bounds für Frustum Culling
        public Aabb Bounds { get; private set; }

        public Chunk(Vector3I coord)
        {
            Coord = coord;
            SvoRoot = new SVO();
            BiomeType = EBiomeType.Plains; // Standard
            IsGenerated = false;
            HasMesh = false;
            
            // Berechne Bounds
            Vector3 worldPos = new Vector3(
                coord.X * VoxelWorld.CHUNK_SIZE_M,
                coord.Y * VoxelWorld.CHUNK_SIZE_M,
                coord.Z * VoxelWorld.CHUNK_SIZE_M
            );
            Bounds = new Aabb(worldPos, Vector3.One * VoxelWorld.CHUNK_SIZE_M);
        }

        /// <summary>
        /// Markiert den Chunk als generiert
        /// </summary>
        public void MarkAsGenerated()
        {
            IsGenerated = true;
        }

        /// <summary>
        /// Setzt einen Voxel relativ zur Chunk-Position
        /// </summary>
        public void SetVoxel(Vector3I localPos, EVoxelType type, float density = 1.0f)
        {
            SvoRoot.SetVoxel(localPos, type, density);
        }

        /// <summary>
        /// Holt einen Voxel relativ zur Chunk-Position
        /// </summary>
        public (EVoxelType type, float density) GetVoxel(Vector3I localPos)
        {
            return SvoRoot.GetVoxel(localPos);
        }
    }
}