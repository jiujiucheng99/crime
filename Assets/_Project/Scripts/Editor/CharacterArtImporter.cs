#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AmberCity.EditorTools
{
    public sealed class CharacterArtImporter : AssetPostprocessor
    {
        private const string Root = "Assets/Resources/Characters/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            Apply((TextureImporter)assetImporter);
        }

        [InitializeOnLoadMethod]
        private static void EnsureSettings()
        {
            string[] paths =
            {
                Root + "BarPatrons/patron_01.png",
                Root + "BarPatrons/patron_02.png",
                Root + "BarPatrons/patron_03.png",
                Root + "Police/police_officer_front.png",
                Root + "Police/police_officer_back.png",
                Root + "Police/police_officer_side.png"
            };

            foreach (string path in paths)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                if (importer.textureType != TextureImporterType.Sprite ||
                    importer.spritePixelsPerUnit != 32f ||
                    importer.filterMode != FilterMode.Point ||
                    importer.textureCompression != TextureImporterCompression.Uncompressed ||
                    !importer.alphaIsTransparency)
                {
                    Apply(importer);
                    importer.SaveAndReimport();
                }
            }
        }

        private static void Apply(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 512;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
        }
    }
}
#endif
