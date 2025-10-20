using Godot;
using System;

namespace Automalithic.Rendering
{
    /// <summary>
    /// Bridge-Klasse, um Mesh-Daten in Godot MeshInstance3D zu packen.
    /// Optional, da im MVP direkt im Chunk BuildMesh erzeugt wird.
    /// Dateipfad: res://Automalithic/src/Rendering/ChunkRenderer.cs
    /// </summary>
    public partial class ChunkRenderer : Node3D
    {
        private MeshInstance3D _meshInstance;

        public override void _Ready()
        {
            _meshInstance = new MeshInstance3D();
            AddChild(_meshInstance);
        }

        /// <summary>
        /// Aktualisiert das Mesh der Instanz.
        /// </summary>
        /// <param name="mesh">Das neue Mesh.</param>
        public void UpdateMesh(Mesh mesh)
        {
            if (_meshInstance == null)
            {
                _meshInstance = new MeshInstance3D();
                AddChild(_meshInstance);
            }
            _meshInstance.Mesh = mesh;
        }
    }
}