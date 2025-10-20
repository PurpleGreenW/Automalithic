using Godot;
using System.Collections.Generic;

namespace Automalithic.VoxelSystem
{
    /// <summary>
    /// Verwaltet Materialien und Texturen für verschiedene VoxelTypes
    /// Unterstützt PBR-Texturen aus Material Maker und Fallback-Vertex-Farben
    /// </summary>
    public partial class VoxelMaterial : Resource
    {
        // Textur-Pfade für PBR-Workflow
        [Export] public Texture2D AlbedoTexture { get; set; }
        [Export] public Texture2D NormalTexture { get; set; }
        [Export] public Texture2D RoughnessTexture { get; set; }
        [Export] public Texture2D MetallicTexture { get; set; }
        [Export] public Texture2D AoTexture { get; set; } // Ambient Occlusion
        [Export] public Texture2D HeightTexture { get; set; } // Für Parallax/Displacement
        
        // ORM Texture (Occlusion, Roughness, Metallic) - Godot 4 preferred workflow
        [Export] public Texture2D OrmTexture { get; set; }
        
        // Fallback-Farben wenn keine Texturen vorhanden
        [Export] public Color AlbedoColor { get; set; } = Colors.Gray;
        [Export] public float Roughness { get; set; } = 0.5f;
        [Export] public float Metallic { get; set; } = 0.0f;
        [Export] public float AoLightAffect { get; set; } = 1.0f;
        
        // Material-Eigenschaften
        [Export] public float TextureScale { get; set; } = 1.0f;
        [Export] public bool UseTriplanarMapping { get; set; } = true;
        [Export] public float TriplanarSharpness { get; set; } = 1.0f;
        
        /// <summary>
        /// Erstellt ein StandardMaterial3D aus den VoxelMaterial-Eigenschaften
        /// </summary>
        public StandardMaterial3D CreateStandardMaterial()
        {
            var material = new StandardMaterial3D();
            
            // Setze Texturen oder Fallback-Farben
            if (AlbedoTexture != null)
            {
                material.AlbedoTexture = AlbedoTexture;
            }
            else
            {
                material.AlbedoColor = AlbedoColor;
            }
            
            if (NormalTexture != null)
            {
                material.NormalEnabled = true;
                material.NormalTexture = NormalTexture;
            }
            
            // In Godot 4 gibt es zwei Workflows:
            // 1. ORM Texture (bevorzugt) - alle drei Kanäle in einer Textur
            // 2. Separate Texturen für Roughness und Metallic
            
            if (OrmTexture != null)
            {
                // ORM Workflow: Eine Textur mit Occlusion (R), Roughness (G), Metallic (B)
                material.OrmTexture = OrmTexture;
                material.AOLightAffect = AoLightAffect;
            }
            else
            {
                // Separate Texturen Workflow
                if (RoughnessTexture != null)
                {
                    material.RoughnessTexture = RoughnessTexture;
                }
                else
                {
                    material.Roughness = Roughness;
                }
                
                if (MetallicTexture != null)
                {
                    material.MetallicTexture = MetallicTexture;
                }
                else
                {
                    material.Metallic = Metallic;
                }
                
                // Wenn wir eine separate AO Textur haben, müssen wir sie 
                // in eine ORM Textur konvertieren oder per Shader verwenden
                // StandardMaterial3D unterstützt keine separate AO Textur
                if (AoTexture != null)
                {
                    GD.PrintErr("Separate AO textures are not directly supported in StandardMaterial3D. " +
                              "Consider using ORM texture or a custom shader.");
                }
            }
            
            if (HeightTexture != null)
            {
                material.HeightmapEnabled = true;
                material.HeightmapTexture = HeightTexture;
                material.HeightmapScale = 0.05f; // Anpassbar
            }
            
            // Triplanar Mapping für bessere Texturierung auf Voxel-Oberflächen
            if (UseTriplanarMapping)
            {
                material.Uv1Triplanar = true;
                material.Uv1TriplanarSharpness = TriplanarSharpness;
                material.Uv1Scale = Vector3.One * TextureScale;
            }
            
            // Weitere Einstellungen
            material.TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic;
            material.DiffuseMode = BaseMaterial3D.DiffuseModeEnum.Burley;
            material.SpecularMode = BaseMaterial3D.SpecularModeEnum.SchlickGgx;
            
            return material;
        }
        
        /// <summary>
        /// Erstellt ein Shader-Material für erweiterte Effekte
        /// Unterstützt separate AO Texturen
        /// </summary>
        public ShaderMaterial CreateShaderMaterial(Shader voxelShader)
        {
            var material = new ShaderMaterial();
            material.Shader = voxelShader;
            
            // Setze Shader-Parameter
            material.SetShaderParameter("albedo_texture", AlbedoTexture);
            material.SetShaderParameter("albedo_color", AlbedoColor);
            material.SetShaderParameter("normal_texture", NormalTexture);
            material.SetShaderParameter("roughness_texture", RoughnessTexture);
            material.SetShaderParameter("roughness_value", Roughness);
            material.SetShaderParameter("metallic_texture", MetallicTexture);
            material.SetShaderParameter("metallic_value", Metallic);
            material.SetShaderParameter("ao_texture", AoTexture);
            material.SetShaderParameter("height_texture", HeightTexture);
            material.SetShaderParameter("texture_scale", TextureScale);
            material.SetShaderParameter("use_triplanar", UseTriplanarMapping);
            material.SetShaderParameter("triplanar_sharpness", TriplanarSharpness);
            
            // ORM texture support
            material.SetShaderParameter("orm_texture", OrmTexture);
            material.SetShaderParameter("ao_light_affect", AoLightAffect);
            
            return material;
        }
        
        /// <summary>
        /// Hilfsmethode um separate Texturen in eine ORM Textur zu kombinieren
        /// Dies kann in einem Editor-Tool verwendet werden
        /// </summary>
        public static ImageTexture CombineToOrmTexture(Texture2D aoTex, Texture2D roughnessTex, Texture2D metallicTex)
        {
            // Hole die Bilder
            var aoImage = aoTex?.GetImage() ?? Image.Create(256, 256, false, Image.Format.Rgb8);
            var roughnessImage = roughnessTex?.GetImage() ?? Image.Create(256, 256, false, Image.Format.Rgb8);
            var metallicImage = metallicTex?.GetImage() ?? Image.Create(256, 256, false, Image.Format.Rgb8);
            
            // Erstelle ORM Image
            var width = Mathf.Max(aoImage.GetWidth(), roughnessImage.GetWidth(), metallicImage.GetWidth());
            var height = Mathf.Max(aoImage.GetHeight(), roughnessImage.GetHeight(), metallicImage.GetHeight());
            
            var ormImage = Image.Create(width, height, false, Image.Format.Rgb8);
            
            // Resize alle Bilder auf gleiche Größe
            aoImage.Resize(width, height);
            roughnessImage.Resize(width, height);
            metallicImage.Resize(width, height);
            
            // Kombiniere die Kanäle
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var ao = aoImage.GetPixel(x, y).R;
                    var roughness = roughnessImage.GetPixel(x, y).R;
                    var metallic = metallicImage.GetPixel(x, y).R;
                    
                    ormImage.SetPixel(x, y, new Color(ao, roughness, metallic));
                }
            }
            
            return ImageTexture.CreateFromImage(ormImage);
        }
    }
}