using Godot;
using Automalithic.World.Enums;
using Automalithic.Utils;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Automalithic.World.DC
{
    /// <summary>
    /// Dual Contouring Mesh Generator für Voxel-zu-Mesh Konvertierung.
    /// Verwendet QEF (Quadratic Error Function) basiertes Dual Contouring für hochqualitative Meshes.
    /// Unterstützt parallele Verarbeitung und ist für GPU-Compute-Shader-Migration vorbereitet.
    /// </summary>
    public static class DualContourMeshGenerator
    {
        // Konstanten für die DC-Verarbeitung
        private const float EPSILON = 0.0001f;
        private const int MAX_SOLVER_ITERATIONS = 10;
        
        /// <summary>
        /// Repräsentiert einen Vertex im Dual Contouring Mesh
        /// </summary>
        public struct DCVertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector2 UV;
            public Color Color; // Für Biom-basierte Vertex-Färbung
            
            public DCVertex(Vector3 pos, Vector3 normal)
            {
                Position = pos;
                Normal = normal;
                UV = Vector2.Zero;
                Color = Colors.White;
            }
        }

        /// <summary>
        /// Edge-Information für Hermite-Daten
        /// </summary>
        private struct EdgeInfo
        {
            public Vector3 Point; // Schnittpunkt auf der Kante
            public Vector3 Normal; // Normale am Schnittpunkt
            public bool HasIntersection;
        }

        /// <summary>
        /// QEF-Daten für einen Voxel-Cell
        /// </summary>
        private class QEFData
        {
            public float[,] ATA; // 3x3 Matrix
            public Vector3 ATb;
            public Vector3 MassPoint;
            public int NumPoints;

            public QEFData()
            {
                ATA = new float[3, 3];
                ATb = Vector3.Zero;
                MassPoint = Vector3.Zero;
                NumPoints = 0;
            }

            /// <summary>
            /// Fügt einen Punkt mit Normale zur QEF hinzu
            /// </summary>
            public void AddPoint(Vector3 point, Vector3 normal)
            {
                // Akkumuliere A^T * A
                ATA[0, 0] += normal.X * normal.X;
                ATA[0, 1] += normal.X * normal.Y;
                ATA[0, 2] += normal.X * normal.Z;
                ATA[1, 0] += normal.Y * normal.X;
                ATA[1, 1] += normal.Y * normal.Y;
                ATA[1, 2] += normal.Y * normal.Z;
                ATA[2, 0] += normal.Z * normal.X;
                ATA[2, 1] += normal.Z * normal.Y;
                ATA[2, 2] += normal.Z * normal.Z;

                // Akkumuliere A^T * b
                float dot = normal.Dot(point);
                ATb += normal * dot;

                // Akkumuliere Mass Point
                MassPoint += point;
                NumPoints++;
            }

            /// <summary>
            /// Löst das QEF-System und gibt die optimale Vertex-Position zurück
            /// </summary>
            public Vector3 Solve()
            {
                if (NumPoints == 0) return Vector3.Zero;

                Vector3 massPoint = MassPoint / NumPoints;

                // Pseudo-Inverse mit SVD (vereinfacht für Echtzeit-Performance)
                // Für robuste Lösung: vollständige SVD implementieren
                // Hier: vereinfachte Lösung mit Regularisierung
                
                float[,] regularized = new float[3, 3];
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        regularized[i, j] = ATA[i, j];
                        if (i == j) regularized[i, j] += 0.01f; // Regularisierung
                    }
                }

                // Löse mittels Cramer's Rule (für 3x3)
                Vector3 solution = SolveCramers(regularized, ATb);
                
                // Constraint: Lösung sollte nahe am Mass Point sein
                Vector3 delta = solution - massPoint;
                if (delta.Length() > 0.5f)
                {
                    solution = massPoint + delta.Normalized() * 0.5f;
                }

                return solution;
            }
        }

        /// <summary>
        /// Hauptfunktion: Generiert Mesh aus SVO mit QEF-basiertem Dual Contouring
        /// </summary>
        /// <param name="svo">Sparse Voxel Octree des Chunks</param>
        /// <param name="chunkOrigin">Welt-Position des Chunk-Ursprungs</param>
        /// <param name="voxelSize">Größe eines Voxels in Metern (0.02m)</param>
        /// <param name="densityThreshold">Schwellwert für Isosurface</param>
        /// <param name="outVertices">Ausgabe: Vertex-Liste</param>
        /// <param name="outIndices">Ausgabe: Index-Liste für Triangles</param>
        /// <param name="outNormals">Ausgabe: Normale pro Vertex</param>
        /// <param name="outUVs">Ausgabe: UV-Koordinaten</param>
        /// <param name="outColors">Ausgabe: Vertex-Farben (für Biome)</param>
        public static void GenerateMeshFromSVO(
            SVO svo,
            Vector3 chunkOrigin,
            float voxelSize,
            float densityThreshold,
            out List<Vector3> outVertices,
            out List<int> outIndices,
            out List<Vector3> outNormals,
            out List<Vector2> outUVs,
            out List<Color> outColors)
        {
            // Initialisiere Ausgabe-Listen
            outVertices = new List<Vector3>();
            outIndices = new List<int>();
            outNormals = new List<Vector3>();
            outUVs = new List<Vector2>();
            outColors = new List<Color>();

            if (svo == null || svo.IsEmpty) return;

            // Dictionary für Vertex-Positionen pro Cell
            Dictionary<Vector3Int, int> cellVertexMap = new Dictionary<Vector3Int, int>();
            Dictionary<Vector3Int, DCVertex> cellVertices = new Dictionary<Vector3Int, DCVertex>();

            // Phase 1: Sammle alle aktiven Cells und berechne Vertex-Positionen
            var activeCells = new List<(Vector3Int coord, SVO.Node node, Vector3 center, float size)>();
            var vector3Int = new Vector3Int();
            CollectActiveCells(svo.Root, chunkOrigin, voxelSize, densityThreshold, vector3Int, 0, activeCells);

            // Parallele Verarbeitung für QEF-Berechnung
            Parallel.ForEach(activeCells, cellData =>
            {
                var (coord, node, center, size) = cellData;
                
                // Sammle Hermite-Daten (Edge-Crossings)
                var edges = GatherEdgeIntersections(node, center, size, densityThreshold);
                
                if (edges.Count > 0)
                {
                    // Erstelle QEF-Daten
                    QEFData qef = new QEFData();
                    foreach (var edge in edges)
                    {
                        qef.AddPoint(edge.Point, edge.Normal);
                    }
                    
                    // Löse QEF für optimale Vertex-Position
                    Vector3 vertexPos = qef.Solve();
                    
                    // Berechne gemittelte Normale
                    Vector3 avgNormal = Vector3.Zero;
                    foreach (var edge in edges)
                    {
                        avgNormal += edge.Normal;
                    }
                    avgNormal = avgNormal.Normalized();
                    
                    // Speichere Vertex-Daten (thread-safe)
                    lock (cellVertices)
                    {
                        cellVertices[coord] = new DCVertex
                        {
                            Position = vertexPos,
                            Normal = avgNormal,
                            UV = CalculateUV(vertexPos, chunkOrigin, VoxelWorld.CHUNK_SIZE_M),
                            Color = GetBiomeColor(node) // Biom-basierte Färbung
                        };
                    }
                }
            });

            // Phase 2: Erstelle Mesh-Indizes basierend auf aktiven Edges
            GenerateMeshConnectivity(activeCells, cellVertices, cellVertexMap, 
                                   outVertices, outIndices, outNormals, outUVs, outColors);

            // Optional: Mesh-Optimierung (Vertex-Welding, Normal-Smoothing)
            if (outVertices.Count > 0)
            {
                OptimizeMesh(outVertices, outIndices, outNormals, outUVs, outColors);
            }
        }

        /// <summary>
        /// Sammelt alle aktiven Cells rekursiv aus dem SVO
        /// </summary>
        private static void CollectActiveCells(
            SVO.Node node, 
            Vector3 nodeOrigin, 
            float nodeSize, 
            float threshold,
            Vector3Int coord,
            int depth,
            List<(Vector3Int, SVO.Node, Vector3, float)> activeCells)
        {
            if (node == null) return;

            if (node.IsLeaf)
            {
                // Prüfe ob diese Cell eine Oberfläche schneidet
                if (HasSignChange(node, threshold))
                {
                    activeCells.Add((coord, node, nodeOrigin + Vector3.One * nodeSize * 0.5f, nodeSize));
                }
            }
            else if (node.Children != null)
            {
                float childSize = nodeSize * 0.5f;
                
                // Rekursiv alle Kinder durchgehen
                for (int i = 0; i < 8; i++)
                {
                    if (node.Children[i] != null)
                    {
                        Vector3 childOffset = new Vector3(
                            (i & 1) * childSize,
                            ((i >> 1) & 1) * childSize,
                            ((i >> 2) & 1) * childSize
                        );
                        
                        Vector3Int childCoord = new Vector3Int(
                            coord.X * 2 + (i & 1),
                            coord.Y * 2 + ((i >> 1) & 1),
                            coord.Z * 2 + ((i >> 2) & 1)
                        );
                        
                        CollectActiveCells(node.Children[i], nodeOrigin + childOffset, 
                                         childSize, threshold, childCoord, depth + 1, activeCells);
                    }
                }
            }
        }

        /// <summary>
        /// Prüft ob eine Cell einen Vorzeichenwechsel hat (Oberfläche schneidet)
        /// </summary>
        private static bool HasSignChange(SVO.Node node, float threshold)
        {
            if (node.IsLeaf)
            {
                // Bei Leaf-Nodes: Density-Wert prüfen
                return Mathf.Abs(node.Density - threshold) < 1.0f;
            }
            
            // Bei internen Nodes: Prüfe ob Kinder verschiedene Vorzeichen haben
            bool hasPositive = false;
            bool hasNegative = false;
            
            CheckChildrenSigns(node, threshold, ref hasPositive, ref hasNegative);
            
            return hasPositive && hasNegative;
        }

        /// <summary>
        /// Rekursive Hilfsfunktion für Vorzeichen-Check
        /// </summary>
        private static void CheckChildrenSigns(SVO.Node node, float threshold, 
                                             ref bool hasPositive, ref bool hasNegative)
        {
            if (node.IsLeaf)
            {
                if (node.Density > threshold) hasPositive = true;
                else hasNegative = true;
            }
            else if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    if (child != null && (!hasPositive || !hasNegative))
                    {
                        CheckChildrenSigns(child, threshold, ref hasPositive, ref hasNegative);
                    }
                }
            }
        }

        /// <summary>
        /// Sammelt Edge-Intersections für Hermite-Daten
        /// </summary>
        private static List<EdgeInfo> GatherEdgeIntersections(SVO.Node node, Vector3 center, 
                                                            float size, float threshold)
        {
            var edges = new List<EdgeInfo>();
            float halfSize = size * 0.5f;
            
            // 12 Edges eines Voxel-Würfels
            Vector3[] corners = new Vector3[]
            {
                center + new Vector3(-halfSize, -halfSize, -halfSize),
                center + new Vector3( halfSize, -halfSize, -halfSize),
                center + new Vector3( halfSize,  halfSize, -halfSize),
                center + new Vector3(-halfSize,  halfSize, -halfSize),
                center + new Vector3(-halfSize, -halfSize,  halfSize),
                center + new Vector3( halfSize, -halfSize,  halfSize),
                center + new Vector3( halfSize,  halfSize,  halfSize),
                center + new Vector3(-halfSize,  halfSize,  halfSize)
            };
            
            int[,] edgeTable = new int[,]
            {
                {0,1}, {1,2}, {2,3}, {3,0}, // Bottom edges
                {4,5}, {5,6}, {6,7}, {7,4}, // Top edges  
                {0,4}, {1,5}, {2,6}, {3,7}  // Vertical edges
            };
            
            // Prüfe jede Kante
            for (int i = 0; i < 12; i++)
            {
                int v0 = edgeTable[i, 0];
                int v1 = edgeTable[i, 1];
                
                float d0 = SampleDensity(node, corners[v0], center, size) - threshold;
                float d1 = SampleDensity(node, corners[v1], center, size) - threshold;
                
                // Vorzeichenwechsel auf dieser Kante?
                if (d0 * d1 < 0)
                {
                    // Berechne Schnittpunkt mittels linearer Interpolation
                    float t = d0 / (d0 - d1);
                    Vector3 point = corners[v0].Lerp(corners[v1], t);
                    
                    // Berechne Normale am Schnittpunkt (Gradient)
                    Vector3 normal = CalculateGradient(node, point, center, size);
                    
                    edges.Add(new EdgeInfo 
                    { 
                        Point = point, 
                        Normal = normal.Normalized(),
                        HasIntersection = true
                    });
                }
            }
            
            return edges;
        }

        /// <summary>
        /// Sample Density-Wert an einer Position
        /// </summary>
        private static float SampleDensity(SVO.Node node, Vector3 pos, Vector3 nodeCenter, float nodeSize)
        {
            // Vereinfachte Density-Abfrage
            // In echter Implementation: Trilineare Interpolation zwischen Nachbar-Nodes
            if (node.IsLeaf)
            {
                return node.Density;
            }
            
            // Für nicht-Leaf Nodes: Durchschnitt der Kinder (vereinfacht)
            float sum = 0;
            int count = 0;
            
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    if (child != null)
                    {
                        sum += child.IsLeaf ? child.Density : 0;
                        count++;
                    }
                }
            }
            
            return count > 0 ? sum / count : 0;
        }

        /// <summary>
        /// Berechnet Gradient (Normale) an einer Position mittels zentraler Differenzen
        /// </summary>
        private static Vector3 CalculateGradient(SVO.Node node, Vector3 pos, Vector3 center, float size)
        {
            float h = size * 0.01f; // Small epsilon für numerische Ableitung
            
            float dx = SampleDensity(node, pos + Vector3.Right * h, center, size) - 
                      SampleDensity(node, pos - Vector3.Right * h, center, size);
            float dy = SampleDensity(node, pos + Vector3.Up * h, center, size) - 
                      SampleDensity(node, pos - Vector3.Up * h, center, size);
            float dz = SampleDensity(node, pos + Vector3.Forward * h, center, size) - 
                      SampleDensity(node, pos - Vector3.Forward * h, center, size);
            
            return new Vector3(dx, dy, dz) / (2.0f * h);
        }

        /// <summary>
        /// Generiert Mesh-Konnektivität basierend auf aktiven Cells
        /// </summary>
        private static void GenerateMeshConnectivity(
            List<(Vector3Int coord, SVO.Node node, Vector3 center, float size)> activeCells,
            Dictionary<Vector3Int, DCVertex> cellVertices,
            Dictionary<Vector3Int, int> cellVertexMap,
            List<Vector3> outVertices,
            List<int> outIndices,
            List<Vector3> outNormals,
            List<Vector2> outUVs,
            List<Color> outColors)
        {
            // Erstelle Vertex-Buffer
            foreach (var kvp in cellVertices)
            {
                cellVertexMap[kvp.Key] = outVertices.Count;
                outVertices.Add(kvp.Value.Position);
                outNormals.Add(kvp.Value.Normal);
                outUVs.Add(kvp.Value.UV);
                outColors.Add(kvp.Value.Color);
            }
            
            // Generiere Quads zwischen benachbarten Cells
            foreach (var cellData in activeCells)
            {
                var coord = cellData.coord;
                
                // Prüfe 3 Richtungen für Quad-Generierung (X, Y, Z)
                GenerateQuadIfNeeded(coord, coord + Vector3Int.Right, Vector3Int.Up, Vector3Int.Forward,
                                   cellVertexMap, outIndices);
                GenerateQuadIfNeeded(coord, coord + Vector3Int.Up, Vector3Int.Right, Vector3Int.Forward,
                                   cellVertexMap, outIndices);
                GenerateQuadIfNeeded(coord, coord + Vector3Int.Forward, Vector3Int.Right, Vector3Int.Up,
                                   cellVertexMap, outIndices);
            }
        }

        /// <summary>
        /// Generiert ein Quad zwischen zwei Cells wenn beide aktiv sind
        /// </summary>
        private static void GenerateQuadIfNeeded(
            Vector3Int c0, Vector3Int c1, Vector3Int axis1, Vector3Int axis2,
            Dictionary<Vector3Int, int> cellVertexMap,
            List<int> outIndices)
        {
            // Prüfe ob alle 4 Cells für das Quad existieren
            if (!cellVertexMap.ContainsKey(c0) || !cellVertexMap.ContainsKey(c1)) return;
            
            Vector3Int c2 = c0 + axis1;
            Vector3Int c3 = c1 + axis1;
            
            if (!cellVertexMap.ContainsKey(c2) || !cellVertexMap.ContainsKey(c3)) return;
            
            // Hole Vertex-Indizes
            int v0 = cellVertexMap[c0];
            int v1 = cellVertexMap[c1];
            int v2 = cellVertexMap[c2];
            int v3 = cellVertexMap[c3];
            
            // Erstelle zwei Triangles für das Quad
            // Winding order für korrekte Normalen
            outIndices.Add(v0); outIndices.Add(v1); outIndices.Add(v3);
            outIndices.Add(v0); outIndices.Add(v3); outIndices.Add(v2);
        }

        /// <summary>
        /// Berechnet UV-Koordinaten basierend auf Welt-Position
        /// </summary>
        private static Vector2 CalculateUV(Vector3 worldPos, Vector3 chunkOrigin, float chunkSize)
        {
            // Normalisiere Position relativ zum Chunk
            Vector3 localPos = worldPos - chunkOrigin;
            
            // Triplanares Mapping (vereinfacht: nur XZ-Ebene)
            return new Vector2(
                localPos.X / chunkSize,
                localPos.Z / chunkSize
            );
        }

        /// <summary>
        /// Gibt Biom-basierte Vertex-Farbe zurück
        /// </summary>
        private static Color GetBiomeColor(SVO.Node node)
        {
            // Platzhalter: Verwende Node-Type für Färbung
            // In echter Implementation: Biom-Daten aus Node extrahieren
            if (node.Type == EVoxelType.Stone)
                return new Color(0.5f, 0.5f, 0.5f, 1.0f);
            else if (node.Type == EVoxelType.Dirt)
                return new Color(0.4f, 0.3f, 0.2f, 1.0f);
            else if (node.Type == EVoxelType.Sand)
                return new Color(0.9f, 0.8f, 0.6f, 1.0f);
            else if (node.Type == EVoxelType.Bedrock)
                return new Color(0.2f, 0.2f, 0.2f, 1.0f);
            
            return Colors.White;
        }

        /// <summary>
        /// Mesh-Optimierung: Welding, Smoothing, etc.
        /// </summary>
        private static void OptimizeMesh(
            List<Vector3> vertices,
            List<int> indices,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<Color> colors)
        {
            // Vertex-Welding für duplizierte Vertices
            Dictionary<Vector3, int> uniqueVerts = new Dictionary<Vector3, int>();
            List<Vector3> newVerts = new List<Vector3>();
            List<Vector3> newNormals = new List<Vector3>();
            List<Vector2> newUVs = new List<Vector2>();
            List<Color> newColors = new List<Color>();
            int[] remapTable = new int[vertices.Count];
            
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 v = vertices[i].Round(3); // Runde auf 3 Dezimalstellen
                
                if (!uniqueVerts.ContainsKey(v))
                {
                    uniqueVerts[v] = newVerts.Count;
                    newVerts.Add(vertices[i]);
                    newNormals.Add(normals[i]);
                    newUVs.Add(uvs[i]);
                    newColors.Add(colors[i]);
                }
                
                remapTable[i] = uniqueVerts[v];
            }
            
            // Update Indizes
            for (int i = 0; i < indices.Count; i++)
            {
                indices[i] = remapTable[indices[i]];
            }
            
            // Ersetze Listen mit optimierten Versionen
            vertices.Clear();
            vertices.AddRange(newVerts);
            normals.Clear();
            normals.AddRange(newNormals);
            uvs.Clear();
            uvs.AddRange(newUVs);
            colors.Clear();
            colors.AddRange(newColors);
            
            // Optional: Normal-Smoothing für benachbarte Vertices
            SmoothNormals(vertices, indices, normals);
        }

        /// <summary>
        /// Glättet Normalen für weichere Übergänge
        /// </summary>
        private static void SmoothNormals(List<Vector3> vertices, List<int> indices, List<Vector3> normals)
        {
            // Sammle alle Faces pro Vertex
            Dictionary<int, List<Vector3>> vertexFaceNormals = new Dictionary<int, List<Vector3>>();
            
            // Berechne Face-Normalen und sammle pro Vertex
            for (int i = 0; i < indices.Count; i += 3)
            {
                int i0 = indices[i];
                int i1 = indices[i + 1];
                int i2 = indices[i + 2];
                
                Vector3 v0 = vertices[i0];
                Vector3 v1 = vertices[i1];
                Vector3 v2 = vertices[i2];
                
                Vector3 faceNormal = ((v1 - v0).Cross(v2 - v0)).Normalized();
                
                if (!vertexFaceNormals.ContainsKey(i0)) vertexFaceNormals[i0] = new List<Vector3>();
                if (!vertexFaceNormals.ContainsKey(i1)) vertexFaceNormals[i1] = new List<Vector3>();
                if (!vertexFaceNormals.ContainsKey(i2)) vertexFaceNormals[i2] = new List<Vector3>();
                
                vertexFaceNormals[i0].Add(faceNormal);
                vertexFaceNormals[i1].Add(faceNormal);
                vertexFaceNormals[i2].Add(faceNormal);
            }
            
            // Durchschnitt der Face-Normalen pro Vertex
            for (int i = 0; i < normals.Count; i++)
            {
                if (vertexFaceNormals.ContainsKey(i))
                {
                    Vector3 avg = Vector3.Zero;
                    foreach (var n in vertexFaceNormals[i])
                    {
                        avg += n;
                    }
                    normals[i] = avg.Normalized();
                }
            }
        }

        /// <summary>
        /// Löst 3x3 Gleichungssystem mit Cramer's Rule
        /// </summary>
        private static Vector3 SolveCramers(float[,] A, Vector3 b)
        {
            // Determinante von A
            float det = Determinant3x3(A);
            
            if (Mathf.Abs(det) < EPSILON)
            {
                // Matrix ist singulär, gib b als Fallback zurück
                return b;
            }
            
            // Cramer's Rule für x, y, z
            float[,] Ax = (float[,])A.Clone();
            Ax[0, 0] = b.X; Ax[1, 0] = b.Y; Ax[2, 0] = b.Z;
            
            float[,] Ay = (float[,])A.Clone();
            Ay[0, 1] = b.X; Ay[1, 1] = b.Y; Ay[2, 1] = b.Z;
            
            float[,] Az = (float[,])A.Clone();
            Az[0, 2] = b.X; Az[1, 2] = b.Y; Az[2, 2] = b.Z;
            
            return new Vector3(
                Determinant3x3(Ax) / det,
                Determinant3x3(Ay) / det,
                Determinant3x3(Az) / det
            );
        }

        /// <summary>
        /// Berechnet Determinante einer 3x3 Matrix
        /// </summary>
        private static float Determinant3x3(float[,] m)
        {
            return m[0,0] * (m[1,1] * m[2,2] - m[1,2] * m[2,1]) -
                   m[0,1] * (m[1,0] * m[2,2] - m[1,2] * m[2,0]) +
                   m[0,2] * (m[1,0] * m[2,1] - m[1,1] * m[2,0]);
        }

        /// <summary>
        /// GPU-Compute-Shader Bridge für zukünftige GPU-Beschleunigung
        /// </summary>
        public static class GPUBridge
        {
            private static RenderingDevice renderingDevice;
            private static Rid computeShader;
            
            /// <summary>
            /// Initialisiert GPU-Ressourcen für Compute-Shader-basiertes DC
            /// </summary>
            public static void Initialize()
            {
                renderingDevice = RenderingServer.CreateLocalRenderingDevice();
                
                // Lade Compute Shader (muss in res://shaders/DCCompute.glsl existieren)
                var shaderFile = GD.Load<RDShaderFile>("res://shaders/DCCompute.glsl");
                computeShader = renderingDevice.ShaderCreateFromSpirV(shaderFile.GetSpirV());
                
                GD.Print("DualContourMeshGenerator GPU Bridge initialisiert");
            }

            /// <summary>
            /// Führt DC auf GPU aus (Placeholder für zukünftige Implementation)
            /// </summary>
            public static void GenerateMeshGPU(SVO svo, out List<Vector3> vertices, out List<int> indices)
            {
                // TODO: GPU-basierte DC-Implementation
                vertices = new List<Vector3>();
                indices = new List<int>();
                
                GD.PrintErr("GPU-basiertes DC noch nicht implementiert");
            }

            /// <summary>
            /// Cleanup GPU-Ressourcen
            /// </summary>
            public static void Cleanup()
            {
                if (renderingDevice != null && computeShader.IsValid)
                {
                    renderingDevice.FreeRid(computeShader);
                }
            }
        }
    }
}