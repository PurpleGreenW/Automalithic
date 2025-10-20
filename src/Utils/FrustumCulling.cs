using Godot;
using System;

namespace Automalithic.Utils
{
    /// <summary>
    /// Utility-Klasse für Frustum Culling Berechnungen.
    /// Bestimmt ob Objekte im Sichtfeld der Kamera sind.
    /// </summary>
    public static class FrustumCulling
    {
        /// <summary>
        /// Prüft ob eine AABB im Frustum der Kamera sichtbar ist (einfache Version)
        /// </summary>
        /// <param name="camera">Die Kamera für das Frustum</param>
        /// <param name="aabb">Die zu prüfende Axis-Aligned Bounding Box</param>
        /// <returns>True wenn sichtbar, False wenn außerhalb</returns>
        public static bool IsAabbVisible(Camera3D camera, Aabb aabb)
        {
            if (camera == null) return false;
            
            // Frustum-Test mit Godot's eingebauter Methode
            return camera.IsPositionInFrustum(aabb.GetCenter());
        }

        /// <summary>
        /// Erweiterte AABB-Sichtbarkeitsprüfung mit vollständigem Frustum-Test
        /// </summary>
        /// <param name="camera">Die Kamera</param>
        /// <param name="aabb">Die AABB</param>
        /// <param name="margin">Zusätzlicher Rand für Sichtbarkeit</param>
        /// <returns>True wenn sichtbar</returns>
        public static bool IsAabbVisibleExtended(Camera3D camera, Aabb aabb, float margin = 0.0f)
        {
            if (camera == null) return false;
            
            // AABB mit Margin erweitern
            if (margin > 0)
            {
                aabb = aabb.Grow(margin);
            }
            
            // Eckpunkte der AABB
            Vector3[] corners = new Vector3[8];
            Vector3 min = aabb.Position;
            Vector3 max = aabb.Position + aabb.Size;
            
            corners[0] = new Vector3(min.X, min.Y, min.Z);
            corners[1] = new Vector3(max.X, min.Y, min.Z);
            corners[2] = new Vector3(min.X, max.Y, min.Z);
            corners[3] = new Vector3(max.X, max.Y, min.Z);
            corners[4] = new Vector3(min.X, min.Y, max.Z);
            corners[5] = new Vector3(max.X, min.Y, max.Z);
            corners[6] = new Vector3(min.X, max.Y, max.Z);
            corners[7] = new Vector3(max.X, max.Y, max.Z);
            
            // Prüfe ob mindestens eine Ecke im Frustum ist
            foreach (var corner in corners)
            {
                if (camera.IsPositionInFrustum(corner))
                    return true;
            }
            
            // Zusätzlich: Prüfe ob Mittelpunkt sichtbar ist
            return camera.IsPositionInFrustum(aabb.GetCenter());
        }

        /// <summary>
        /// Optimierte AABB-Sichtbarkeitsprüfung mit frühem Ausstieg
        /// </summary>
        public static bool IsAabbVisibleOptimized(Camera3D camera, Aabb aabb)
        {
            if (camera == null) return false;
            
            // Schneller Test: Distanz zur Kamera
            float distanceSq = camera.GlobalPosition.DistanceSquaredTo(aabb.GetCenter());
            float farSq = camera.Far * camera.Far;
            
            // Zu weit weg?
            if (distanceSq > farSq)
                return false;
            
            // Zu nah? (hinter Near Plane)
            if (distanceSq < camera.Near * camera.Near * 0.25f) // 0.25 für Sicherheit
                return true; // Sehr nahe Objekte immer rendern
            
            // Frustum-Test
            return IsAabbVisibleExtended(camera, aabb);
        }

        /// <summary>
        /// Berechnet die Distanz einer AABB zur Kamera (für LOD)
        /// </summary>
        public static float GetDistanceToCamera(Camera3D camera, Aabb aabb)
        {
            if (camera == null) return float.MaxValue;
            return camera.GlobalPosition.DistanceTo(aabb.GetCenter());
        }

        /// <summary>
        /// Bestimmt das LOD-Level basierend auf der Distanz
        /// </summary>
        public static int CalculateLodLevel(float distance, float[] lodDistances)
        {
            for (int i = 0; i < lodDistances.Length; i++)
            {
                if (distance <= lodDistances[i])
                    return i;
            }
            return lodDistances.Length; // Höchstes LOD (unsichtbar)
        }

        /// <summary>
        /// Prüft ob ein Punkt im Frustum ist (Helper-Methode)
        /// </summary>
        public static bool IsPointInFrustum(Camera3D camera, Vector3 point)
        {
            return camera?.IsPositionInFrustum(point) ?? false;
        }

        /// <summary>
        /// Berechnet die Bildschirmgröße eines Objekts (für adaptive LOD)
        /// </summary>
        public static float CalculateScreenSize(Camera3D camera, Aabb aabb, Vector2 screenSize)
        {
            if (camera == null) return 0f;
            
            float distance = GetDistanceToCamera(camera, aabb);
            if (distance <= 0) return screenSize.X; // Sehr nah = voller Bildschirm
            
            // Approximation der Bildschirmgröße
            float objectSize = aabb.Size.Length();
            float fov = camera.Fov;
            
            // Berechne ungefähre Pixelgröße
            float screenHeight = screenSize.Y;
            float pixelSize = (objectSize / distance) * (screenHeight / (2f * Mathf.Tan(Mathf.DegToRad(fov * 0.5f))));
            
            return pixelSize;
        }
    }
}