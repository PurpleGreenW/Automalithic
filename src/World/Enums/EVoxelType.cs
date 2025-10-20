using Godot;

namespace Automalithic.World.Enums
{
    /// <summary>
    /// Definiert alle verfügbaren Voxel-Materialtypen im Spiel.
    /// Jeder Typ hat spezifische physikalische Eigenschaften und Rendering-Charakteristika.
    /// </summary>
    public enum EVoxelType : byte
    {
        // Gase/Luft (0-9)
        Air = 0,            // Leerraum, kein Rendering/Collision
        Fog = 1,            // Nebel, reduzierte Sichtweite
        ToxicGas = 2,       // Faulgase/Methan von Dino-Ausscheidungen
        Smoke = 3,          // Rauch von Feuer
        Steam = 4,          // Wasserdampf
        
        // Grundgestein (10-39)
        Bedrock = 10,       // Unzerstörbares Grundgestein
        Stone = 11,         // Standard-Stein
        Granite = 12,       // Härteres Gestein
        Basalt = 13,        // Vulkanisches Gestein
        Limestone = 14,     // Kalkstein (für spätere Chemie-Reaktionen)
        Sandstone = 15,     // Sandstein
        
        // Erze/Metalle (40-79)
        IronOre = 40,       // Eisenerz
        CopperOre = 41,     // Kupfererz
        GoldOre = 42,       // Golderz
        SilverOre = 43,     // Silbererz
        TinOre = 44,        // Zinnerz (für Bronze)
        Coal = 45,          // Kohle
        
        // Böden (80-99)
        Dirt = 80,          // Erde
        Grass = 81,         // Grasbewachsene Erde
        Mud = 82,           // Matsch (Erde + Wasser)
        Clay = 83,          // Lehm
        
        // Sande (100-119)
        Sand = 100,         // Normaler Sand
        BlackSand = 101,    // Eisenhaltiger Sand
        GoldSand = 102,     // Goldhaltiger Sand
        
        // Holz (120-139)
        Wood = 120,         // Standard-Holz
        OakWood = 121,      // Eichenholz
        BirchWood = 122,    // Birkenholz
        TropicalWood = 123, // Tropisches Holz
        
        // Flüssigkeiten (140-159)
        Water = 140,        // Wasser
        Lava = 141,         // Lava
        Acid = 142,         // Säure
        
        // Eis/Schnee (160-179)
        Ice = 160,          // Eis
        Snow = 161,         // Schnee
        PackedIce = 162,    // Verdichtetes Eis
        
        // Spezial (180-255)
        DinoEgg = 180,      // Dino-Eier
        Artifact = 181,     // Archäologische Artefakte
    }

    /// <summary>
    /// Hilfsklasse für VoxelType-bezogene Operationen
    /// </summary>
    public static class VoxelTypeExtensions
    {
        /// <summary>
        /// Prüft ob dieser VoxelType ein Gas ist
        /// </summary>
        public static bool IsGas(this EVoxelType type)
        {
            return (byte)type >= 0 && (byte)type <= 9;
        }

        /// <summary>
        /// Prüft ob dieser VoxelType eine Flüssigkeit ist
        /// </summary>
        public static bool IsLiquid(this EVoxelType type)
        {
            return (byte)type >= 140 && (byte)type <= 159;
        }

        /// <summary>
        /// Prüft ob dieser VoxelType solide ist
        /// </summary>
        public static bool IsSolid(this EVoxelType type)
        {
            return !IsGas(type) && !IsLiquid(type);
        }

        /// <summary>
        /// Gibt die Basisdichte für diesen VoxelType zurück
        /// </summary>
        public static float GetBaseDensity(this EVoxelType type)
        {
            return type switch
            {
                EVoxelType.Air => 0.0f,
                EVoxelType.Bedrock => 1.0f,
                EVoxelType.Stone => 0.9f,
                EVoxelType.Granite => 0.95f,
                EVoxelType.Sand => 0.6f,
                EVoxelType.Dirt => 0.5f,
                EVoxelType.Water => 0.3f,
                _ => 0.8f
            };
        }

        /// <summary>
        /// Gibt die Stabilitätsschwelle für diesen VoxelType zurück
        /// </summary>
        public static int GetStabilityThreshold(this EVoxelType type)
        {
            return type switch
            {
                EVoxelType.Bedrock => 100,
                EVoxelType.Stone => 80,
                EVoxelType.Granite => 90,
                EVoxelType.Sand => 20,
                EVoxelType.Dirt => 30,
                _ when type.IsGas() => 0,
                _ when type.IsLiquid() => 5,
                _ => 50
            };
        }
    }
}