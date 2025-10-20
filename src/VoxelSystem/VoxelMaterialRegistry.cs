using Godot;
using System.Collections.Generic;
using Automalithic.World.Enums;
using System.Linq;

namespace Automalithic.VoxelSystem
{
    /// <summary>
    /// Zentrale Registry für alle VoxelType-Materialien
    /// Lädt Texturen aus Material Maker und weist Fallback-Farben zu
    /// </summary>
    public partial class VoxelMaterialRegistry : Node
    {
        private static VoxelMaterialRegistry _instance;
        public static VoxelMaterialRegistry Instance => _instance;
        
        // Material-Cache
        private Dictionary<EVoxelType, VoxelMaterial> _materials = new();
        private Dictionary<EVoxelType, StandardMaterial3D> _standardMaterials = new();
        private Dictionary<EVoxelType, ShaderMaterial> _shaderMaterials = new();
        
        // Pfade zu Texturen
        [Export] private string _textureBasePath = "res://assets/textures/voxels/";
        [Export] private bool _useShaderMaterials = false;
        [Export] private Shader _voxelShader;
        
        public override void _Ready()
        {
            _instance = this;
            LoadAllMaterials();
        }
        
        /// <summary>
        /// Lädt alle Materialien und Texturen für die VoxelTypes
        /// </summary>
        private void LoadAllMaterials()
        {
            // Definiere Materialien für jeden BlockType
            var materialDefinitions = new Dictionary<EVoxelType, (string textureName, Color fallbackColor, float roughness, float metallic)>
            {
                // Luft/Gase
                { EVoxelType.Air, ("", Colors.Transparent, 0.0f, 0.0f) },
                { EVoxelType.Fog, ("fog", new Color(0.8f, 0.8f, 0.8f, 0.3f), 0.9f, 0.0f) },
                { EVoxelType.ToxicGas, ("toxic_gas", new Color(0.4f, 0.8f, 0.2f, 0.4f), 0.95f, 0.0f) },
                
                // Gesteine
                { EVoxelType.Bedrock, ("bedrock", new Color(0.1f, 0.1f, 0.1f), 0.9f, 0.1f) },
                { EVoxelType.Stone, ("stone", new Color(0.5f, 0.5f, 0.5f), 0.8f, 0.0f) },
                { EVoxelType.Granite, ("granite", new Color(0.6f, 0.4f, 0.4f), 0.7f, 0.1f) },
                { EVoxelType.Basalt, ("basalt", new Color(0.2f, 0.2f, 0.2f), 0.8f, 0.05f) },
                { EVoxelType.Limestone, ("limestone", new Color(0.8f, 0.8f, 0.7f), 0.6f, 0.0f) },
                { EVoxelType.Slate, ("slate", new Color(0.3f, 0.3f, 0.4f), 0.7f, 0.0f) },
                
                // Erze
                { EVoxelType.IronOre, ("iron_ore", new Color(0.7f, 0.4f, 0.3f), 0.6f, 0.3f) },
                { EVoxelType.CopperOre, ("copper_ore", new Color(0.8f, 0.5f, 0.3f), 0.5f, 0.4f) },
                { EVoxelType.GoldOre, ("gold_ore", new Color(0.9f, 0.8f, 0.3f), 0.3f, 0.8f) },
                { EVoxelType.CoalOre, ("coal_ore", new Color(0.1f, 0.1f, 0.1f), 0.9f, 0.0f) },
                { EVoxelType.ZincOre, ("zinc_ore", new Color(0.6f, 0.6f, 0.7f), 0.5f, 0.6f) },
                { EVoxelType.LeadOre, ("lead_ore", new Color(0.4f, 0.4f, 0.5f), 0.7f, 0.4f) },
                { EVoxelType.SilverOre, ("silver_ore", new Color(0.8f, 0.8f, 0.9f), 0.2f, 0.9f) },
                { EVoxelType.TitaniumOre, ("titanium_ore", new Color(0.7f, 0.7f, 0.8f), 0.4f, 0.7f) },
                { EVoxelType.AluminumOre, ("aluminum_ore", new Color(0.8f, 0.8f, 0.8f), 0.4f, 0.5f) },
                
                // Böden
                { EVoxelType.Dirt, ("dirt", new Color(0.4f, 0.3f, 0.2f), 0.9f, 0.0f) },
                { EVoxelType.Grass, ("grass", new Color(0.3f, 0.6f, 0.2f), 0.8f, 0.0f) },
                { EVoxelType.DryGrass, ("dry_grass", new Color(0.6f, 0.5f, 0.3f), 0.85f, 0.0f) },
                { EVoxelType.WetGrass, ("wet_grass", new Color(0.2f, 0.5f, 0.1f), 0.7f, 0.0f) },
                { EVoxelType.SwampGrass, ("swamp_grass", new Color(0.2f, 0.4f, 0.2f), 0.9f, 0.0f) },
                { EVoxelType.Mud, ("mud", new Color(0.3f, 0.25f, 0.2f), 0.95f, 0.0f) },
                { EVoxelType.Clay, ("clay", new Color(0.6f, 0.4f, 0.3f), 0.8f, 0.0f) },
                
                // Schnee/Eis
                { EVoxelType.Snow, ("snow", new Color(0.95f, 0.95f, 0.95f), 0.9f, 0.0f) },
                { EVoxelType.Ice, ("ice", new Color(0.7f, 0.9f, 1.0f), 0.1f, 0.0f) },
                
                // Holz
                { EVoxelType.OakWood, ("oak_wood", new Color(0.5f, 0.3f, 0.2f), 0.7f, 0.0f) },
                { EVoxelType.BirchWood, ("birch_wood", new Color(0.8f, 0.7f, 0.6f), 0.6f, 0.0f) },
                { EVoxelType.TropicalWood, ("tropical_wood", new Color(0.6f, 0.4f, 0.3f), 0.65f, 0.0f) },
                { EVoxelType.Leaves, ("leaves", new Color(0.2f, 0.5f, 0.1f), 0.8f, 0.0f) },
                
                // Flüssigkeiten
                { EVoxelType.Water, ("water", new Color(0.2f, 0.4f, 0.8f, 0.8f), 0.0f, 0.0f) },
                { EVoxelType.Lava, ("lava", new Color(1.0f, 0.3f, 0.0f), 0.0f, 0.0f) },
                { EVoxelType.Acid, ("acid", new Color(0.4f, 1.0f, 0.2f, 0.7f), 0.0f, 0.0f) },
                
                // Sande
                { EVoxelType.Sand, ("sand", new Color(0.9f, 0.8f, 0.6f), 0.8f, 0.0f) },
                { EVoxelType.Sandstone, ("sandstone", new Color(0.8f, 0.7f, 0.5f), 0.7f, 0.0f) },
                { EVoxelType.IronSand, ("iron_sand", new Color(0.3f, 0.2f, 0.1f), 0.8f, 0.2f) },
                { EVoxelType.BlackSand, ("black_sand", new Color(0.1f, 0.1f, 0.1f), 0.85f, 0.1f) },
                
                // Kristalle
                { EVoxelType.Quartz, ("quartz", new Color(0.9f, 0.9f, 0.95f), 0.2f, 0.1f) },
                { EVoxelType.Emerald, ("emerald", new Color(0.2f, 0.8f, 0.4f), 0.1f, 0.2f) },
                { EVoxelType.Ruby, ("ruby", new Color(0.9f, 0.2f, 0.3f), 0.1f, 0.2f) },
                { EVoxelType.Diamond, ("diamond", new Color(0.8f, 0.9f, 1.0f), 0.0f, 0.3f) },
            };
            
            // Lade Materialien für jeden BlockType
            foreach (var (blockType, (textureName, fallbackColor, roughness, metallic)) in materialDefinitions)
            {
                var material = new VoxelMaterial
                {
                    AlbedoColor = fallbackColor,
                    Roughness = roughness,
                    Metallic = metallic
                };
                
                // Versuche Texturen zu laden wenn vorhanden
                if (!string.IsNullOrEmpty(textureName))
                {
                    LoadTexturesForMaterial(material, textureName);
                }
                
                _materials[blockType] = material;
                
                // Erstelle gecachte Materialien
                if (_useShaderMaterials && _voxelShader != null)
                {
                    _shaderMaterials[blockType] = material.CreateShaderMaterial(_voxelShader);
                }
                else
                {
                    _standardMaterials[blockType] = material.CreateStandardMaterial();
                }
            }
            
            GD.Print($"VoxelMaterialRegistry: {_materials.Count} Materialien geladen");
        }
        
        /// <summary>
        /// Lädt PBR-Texturen aus Material Maker Export
        /// </summary>
        private void LoadTexturesForMaterial(VoxelMaterial material, string textureName)
        {
            var basePath = _textureBasePath + textureName + "/";
            
            // Material Maker Standard Export Namen
            var textureFiles = new Dictionary<string, string>
            {
                { "albedo", basePath + textureName + "_albedo.png" },
                { "normal", basePath + textureName + "_normal.png" },
                { "roughness", basePath + textureName + "_roughness.png" },
                { "metallic", basePath + textureName + "_metallic.png" },
                { "ao", basePath + textureName + "_ao.png" },
                { "height", basePath + textureName + "_height.png" },
                // Alternative Namen die Material Maker nutzen könnte
                { "albedo_alt", basePath + textureName + "_diffuse.png" },
                { "roughness_alt", basePath + textureName + "_rough.png" },
                { "metallic_alt", basePath + textureName + "_metal.png" },
                { "ao_alt", basePath + textureName + "_occlusion.png" },
                { "height_alt", basePath + textureName + "_displacement.png" }
            };
            
            // Lade Albedo
            if (ResourceLoader.Exists(textureFiles["albedo"]))
            {
                material.AlbedoTexture = GD.Load<Texture2D>(textureFiles["albedo"]);
            }
            else if (ResourceLoader.Exists(textureFiles["albedo_alt"]))
            {
                material.AlbedoTexture = GD.Load<Texture2D>(textureFiles["albedo_alt"]);
            }
            
            // Lade Normal Map
            if (ResourceLoader.Exists(textureFiles["normal"]))
            {
                material.NormalTexture = GD.Load<Texture2D>(textureFiles["normal"]);
            }
            
            // Lade Roughness
            if (ResourceLoader.Exists(textureFiles["roughness"]))
            {
                material.RoughnessTexture = GD.Load<Texture2D>(textureFiles["roughness"]);
            }
            else if (ResourceLoader.Exists(textureFiles["roughness_alt"]))
            {
                material.RoughnessTexture = GD.Load<Texture2D>(textureFiles["roughness_alt"]);
            }
            
            // Lade Metallic
            if (ResourceLoader.Exists(textureFiles["metallic"]))
            {
                material.MetallicTexture = GD.Load<Texture2D>(textureFiles["metallic"]);
            }
            else if (ResourceLoader.Exists(textureFiles["metallic_alt"]))
            {
                material.MetallicTexture = GD.Load<Texture2D>(textureFiles["metallic_alt"]);
            }
            
            // Lade AO
            if (ResourceLoader.Exists(textureFiles["ao"]))
            {
                material.AoTexture = GD.Load<Texture2D>(textureFiles["ao"]);
            }
            else if (ResourceLoader.Exists(textureFiles["ao_alt"]))
            {
                material.AoTexture = GD.Load<Texture2D>(textureFiles["ao_alt"]);
            }
            
            // Lade Height/Displacement
            if (ResourceLoader.Exists(textureFiles["height"]))
            {
                material.HeightTexture = GD.Load<Texture2D>(textureFiles["height"]);
            }
            else if (ResourceLoader.Exists(textureFiles["height_alt"]))
            {
                material.HeightTexture = GD.Load<Texture2D>(textureFiles["height_alt"]);
            }
        }
        
        /// <summary>
        /// Hole Material für einen BlockType
        /// </summary>
        public Material GetMaterial(EVoxelType blockType)
        {
            if (_useShaderMaterials && _shaderMaterials.ContainsKey(blockType))
            {
                return _shaderMaterials[blockType];
            }
            else if (_standardMaterials.ContainsKey(blockType))
            {
                return _standardMaterials[blockType];
            }
            
            // Fallback zu Stone Material
            GD.PushWarning($"VoxelMaterialRegistry: Kein Material für {blockType} gefunden, nutze Stone");
            return GetMaterial(EVoxelType.Stone);
        }
        
        /// <summary>
        /// Hole VoxelMaterial Daten für einen BlockType
        /// </summary>
        public VoxelMaterial GetVoxelMaterial(EVoxelType blockType)
        {
            if (_materials.ContainsKey(blockType))
            {
                return _materials[blockType];
            }
            
            // Fallback
            return _materials[EVoxelType.Stone];
        }
        
        /// <summary>
        /// Hole nur die Fallback-Farbe für Vertex Colors
        /// </summary>
        public Color GetFallbackColor(EVoxelType blockType)
        {
            if (_materials.ContainsKey(blockType))
            {
                return _materials[blockType].AlbedoColor;
            }
            
            return Colors.Gray;
        }
        
        /// <summary>
        /// Aktualisiere Material zur Laufzeit (z.B. für Material Editor)
        /// </summary>
        public void UpdateMaterial(EVoxelType blockType, VoxelMaterial newMaterial)
        {
            _materials[blockType] = newMaterial;
            
            // Update gecachte Materialien
            if (_useShaderMaterials && _voxelShader != null)
            {
                _shaderMaterials[blockType] = newMaterial.CreateShaderMaterial(_voxelShader);
            }
            else
            {
                _standardMaterials[blockType] = newMaterial.CreateStandardMaterial();
            }
        }
    }
}