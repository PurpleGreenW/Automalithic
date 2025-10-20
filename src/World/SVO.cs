using Godot;
using System;
using System.Collections.Generic;
using Automalithic.World.Enums;

namespace Automalithic.World
{
    /// <summary>
    /// Sparse Voxel Octree (SVO) - Hauptdatenstruktur für Voxel-Speicherung pro Chunk.
    /// Optimiert für sparsame Speichernutzung bei 20.48m Chunks mit 2cm Leaf-Auflösung.
    /// </summary>
    public class SVO
    {
        /// <summary>
        /// Einzelner Knoten im Octree
        /// </summary>
        public class Node
        {
            // Ist dies ein Leaf-Knoten (keine Kinder)?
            public bool IsLeaf;
            
            // Voxel-Daten (nur relevant für Leaf-Knoten)
            public EVoxelType VoxelType = EVoxelType.Air;
            public float Density = 0.0f; // Positiv: innen, Negativ: außen
            
            // Zusätzliche Eigenschaften für Umweltsimulation
            public byte GasConcentration = 0; // 0-255 für Gaskonzentration
            public byte Temperature = 128;     // 0-255 (128 = normal)
            public byte Moisture = 0;          // 0-255 Feuchtigkeit
            
            // Kinder (nur wenn !IsLeaf)
            public Node[] Children = null;
            
            /// <summary>
            /// Erstellt die 8 Kinder für diesen Knoten
            /// </summary>
            public void Subdivide()
            {
                if (!IsLeaf) return;
                
                IsLeaf = false;
                Children = new Node[8];
                for (int i = 0; i < 8; i++)
                {
                    Children[i] = new Node 
                    { 
                        IsLeaf = true,
                        VoxelType = this.VoxelType,
                        Density = this.Density,
                        GasConcentration = this.GasConcentration,
                        Temperature = this.Temperature,
                        Moisture = this.Moisture
                    };
                }
            }
        }

        public Node Root;
        public const int MAX_DEPTH = 10; // 2^10 = 1024 Leaves pro Achse
        
        public SVO()
        {
            Root = new Node { IsLeaf = true, VoxelType = EVoxelType.Air };
        }

        /// <summary>
        /// Setzt einen Voxel an der gegebenen Position
        /// </summary>
        public void SetVoxel(Vector3I localPos, EVoxelType type, float density = 1.0f)
        {
            SetVoxelRecursive(Root, localPos, 0, MAX_DEPTH, type, density);
        }

        private void SetVoxelRecursive(Node node, Vector3I pos, int depth, int maxDepth, 
            EVoxelType type, float density)
        {
            if (depth == maxDepth)
            {
                node.VoxelType = type;
                node.Density = density;
                return;
            }

            if (node.IsLeaf)
            {
                // Wenn alle Kinder den gleichen Wert hätten, keine Subdivision nötig
                if (node.VoxelType == type && Mathf.Abs(node.Density - density) < 0.01f)
                    return;
                
                node.Subdivide();
            }

            int childIndex = GetChildIndex(pos, depth);
            Vector3I childPos = GetChildPosition(pos, depth);
            
            SetVoxelRecursive(node.Children[childIndex], childPos, depth + 1, maxDepth, type, density);
        }

        /// <summary>
        /// Gibt den Voxel an der gegebenen Position zurück
        /// </summary>
        public (EVoxelType type, float density) GetVoxel(Vector3I localPos)
        {
            return GetVoxelRecursive(Root, localPos, 0, MAX_DEPTH);
        }

        private (EVoxelType type, float density) GetVoxelRecursive(Node node, Vector3I pos, 
            int depth, int maxDepth)
        {
            if (node.IsLeaf || depth == maxDepth)
            {
                return (node.VoxelType, node.Density);
            }

            int childIndex = GetChildIndex(pos, depth);
            Vector3I childPos = GetChildPosition(pos, depth);
            
            return GetVoxelRecursive(node.Children[childIndex], childPos, depth + 1, maxDepth);
        }

        /// <summary>
        /// Berechnet den Child-Index basierend auf Position und Tiefe
        /// </summary>
        private int GetChildIndex(Vector3I pos, int depth)
        {
            int size = 1 << (MAX_DEPTH - depth - 1);
            int x = (pos.X / size) & 1;
            int y = (pos.Y / size) & 1;
            int z = (pos.Z / size) & 1;
            return x + (y << 1) + (z << 2);
        }

        /// <summary>
        /// Berechnet die relative Position innerhalb des Child-Bereichs
        /// </summary>
        private Vector3I GetChildPosition(Vector3I pos, int depth)
        {
            int size = 1 << (MAX_DEPTH - depth - 1);
            return new Vector3I(
                pos.X & (size - 1),
                pos.Y & (size - 1),
                pos.Z & (size - 1)
            );
        }

        /// <summary>
        /// Prüft ob der Baum leer ist (nur Air)
        /// </summary>
        public bool IsEmpty => Root.IsLeaf && Root.VoxelType == EVoxelType.Air;

        /// <summary>
        /// Findet alle Oberflächen-Kreuzungen für Dual Contouring
        /// </summary>
        public IEnumerable<(Vector3 position, Vector3 normal)> GetSurfaceCrossings(
            Vector3 origin, float voxelSize)
        {
            var crossings = new List<(Vector3, Vector3)>();
            TraverseSurfaceCrossings(Root, origin, voxelSize * (1 << MAX_DEPTH), 0, crossings);
            return crossings;
        }

        private void TraverseSurfaceCrossings(Node node, Vector3 nodeOrigin, float nodeSize, 
            int depth, List<(Vector3, Vector3)> crossings)
        {
            if (node.IsLeaf)
            {
                // Prüfe Nachbarn für Oberflächen-Kreuzungen
                if (node.VoxelType.IsSolid())
                {
                    // Vereinfachte Version: Füge Zentrum als Kreuzung hinzu
                    // TODO: Implementiere echte Edge-Kreuzungen mit Nachbar-Sampling
                    Vector3 center = nodeOrigin + Vector3.One * (nodeSize * 0.5f);
                    Vector3 normal = Vector3.Up; // Platzhalter
                    crossings.Add((center, normal));
                }
                return;
            }

            float childSize = nodeSize * 0.5f;
            for (int i = 0; i < 8; i++)
            {
                if (node.Children[i] != null)
                {
                    Vector3 childOffset = new Vector3(
                        (i & 1) * childSize,
                        ((i >> 1) & 1) * childSize,
                        ((i >> 2) & 1) * childSize
                    );
                    TraverseSurfaceCrossings(node.Children[i], nodeOrigin + childOffset, 
                        childSize, depth + 1, crossings);
                }
            }
        }
    }
}