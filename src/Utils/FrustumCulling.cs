// File: src/Utils/FrustumCulling.cs

using Godot;
using System;

namespace Automalithic.Utils
{
    /// <summary>
    /// Utility-Klasse für Frustum-Culling.
    /// Prüft, ob eine AABB (Axis-Aligned Bounding Box) im Frustum der Kamera liegt.
    /// </summary>
    public static class FrustumCulling
    {
        /// <summary>
        /// Prüft, ob eine AABB im Sichtfeld der Kamera liegt.
        /// </summary>
        /// <param name="camera">Die Kamera (Camera3D).</param>
        /// <param name="aabb">Die zu prüfende AABB (Godot.Aabb).</param>
        /// <returns>True, wenn sichtbar, sonst false.</returns>
        public static bool IsAabbVisible(Camera3D camera, Aabb aabb)
        {
            // Godot 4: Camera3D.GetFrustumPlanes() liefert 6 Planes (Godot.Plane[]).
            Plane[] planes = camera.GetFrustumPlanes();

            // Prüfe jede Frustum-Plane.
            foreach (Plane plane in planes)
            {
                // Berechne den Punkt der AABB, der am weitesten in Richtung der Plane liegt.
                Vector3 positiveVertex = aabb.Position;
                if (plane.Normal.X > 0)
                    positiveVertex.X += aabb.Size.X;
                if (plane.Normal.Y > 0)
                    positiveVertex.Y += aabb.Size.Y;
                if (plane.Normal.Z > 0)
                    positiveVertex.Z += aabb.Size.Z;

                // Liegt dieser Punkt außerhalb der Plane, ist die Box komplett außerhalb des Frustums.
                if (plane.DistanceTo(positiveVertex) < 0)
                    return false;
            }

            // Keine Plane hat die Box ausgeschlossen: Sie ist (zumindest teilweise) sichtbar.
            return true;
        }
    }
}