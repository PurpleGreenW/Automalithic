using System;
using System.Collections.Generic;
using Godot;
using Automalithic.Utils; // Make sure this matches your Vector3Int namespace

namespace Automalithic.World
{
    /// <summary>
    /// Sparse Voxel Octree (MVP): Simple, memory-efficient density grid for a chunk.
    /// </summary>
    public class Svo
    {
        // Constants
        private const int DefaultRes = 8; // MVP: 8x8x8 density grid

        // Fields
        private Dictionary<Vector3Int, float> _densityGrid;
        private int _resolution = DefaultRes;

        // Properties
        public int Resolution => _resolution;
        public bool IsEmpty => _densityGrid == null || _densityGrid.Count == 0;

        // Methods

        /// <summary>
        /// Generates a simple density grid using noise.
        /// </summary>
        /// <param name="origin">World origin of the chunk.</param>
        /// <param name="resolution">Grid resolution per axis.</param>
        public void GenerateDensity(Vector3 origin, int resolution)
        {
            _resolution = resolution;
            _densityGrid = new Dictionary<Vector3Int, float>(resolution * resolution * resolution);

            for (int x = 0; x < resolution; x++)
            for (int y = 0; y < resolution; y++)
            for (int z = 0; z < resolution; z++)
            {
                Vector3 cellCenter = origin + new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) * (28.48f / resolution);
                // Example noise function (replace with real Perlin/Simplex as needed)
                float noiseValue = Mathf.Perlin3D(cellCenter * 0.1f);
                _densityGrid[new Vector3Int(x, y, z)] = noiseValue;
            }
        }

        /// <summary>
        /// Gets the density at a given grid coordinate.
        /// </summary>
        public float GetDensity(int x, int y, int z)
        {
            var key = new Vector3Int(x, y, z);
            return _densityGrid.GetValueOrDefault(key, 0.0f);
        }

        /// <summary>
        /// Returns a few demo surface crossings for MVP mesh generation.
        /// </summary>
        public IEnumerable<Vector3> GetSurfaceCrossings(Vector3 origin, float voxelSize)
        {
            // Placeholder: generate a few points for demonstration
            yield return origin + new Vector3(voxelSize * 0.5f, voxelSize * 0.5f, voxelSize * 0.5f);
            yield return origin + new Vector3(voxelSize * 1.0f, voxelSize * 0.5f, voxelSize * 0.5f);
            yield return origin + new Vector3(voxelSize * 0.5f, voxelSize * 1.0f, voxelSize * 0.5f);
        }
    }
}