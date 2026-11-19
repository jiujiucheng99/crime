#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AmberCity.EditorTools
{
    public sealed class ItemArtImporter : AssetPostprocessor
    {
        private const string Root = "Assets/Resources/Items/";

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
                Root + "first_aid.png",
                Root + "flashlight.png",
                Root + "wrench.png",
                Root + "gloves.png",
                Root + "paperclip.png",
                Root + "bun.png",
                Root + "patty.png",
                Root + "lettuce.png",
                Root + "tomato.png",
                Root + "cheese.png",
                Root + "pasta.png",
                Root + "tire.png",
                Root + "battery.png",
                Root + "brake_pads.png",
                Root + "spark_plug.png",
                Root + "watch.png",
                Root + "jewelry.png",
                Root + "electronics.png",
                Root + "collectible.png"
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
            importer.maxTextureSize = 256;

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
