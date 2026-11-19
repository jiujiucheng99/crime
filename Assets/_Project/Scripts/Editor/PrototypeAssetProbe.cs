using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AmberCity.EditorTools
{
    public static class PrototypeAssetProbe
    {
        [MenuItem("Tools/Amber City/Probe Character Sprites")]
        public static void Probe()
        {
            StringBuilder builder = new StringBuilder();
            Sprite[] sprites = Resources.LoadAll<Sprite>("Characters/Kai");
            builder.AppendLine("Resources/Characters/Kai sprite count = " + sprites.Length);

            foreach (Sprite sprite in sprites)
            {
                builder.AppendLine(sprite.name + " | rect=" + sprite.rect + " | ppu=" + sprite.pixelsPerUnit + " | bounds=" + sprite.bounds);
                Texture2D texture = sprite.texture;
                builder.AppendLine("  texture=" + texture.width + "x" + texture.height + " format=" + texture.format + " readable=" + texture.isReadable);

                try
                {
                    Color32[] pixels = texture.GetPixels32();
                    int transparent = 0;
                    int opaque = 0;
                    int semi = 0;
                    foreach (Color32 pixel in pixels)
                    {
                        if (pixel.a == 0) transparent++;
                        else if (pixel.a == 255) opaque++;
                        else semi++;
                    }
                    builder.AppendLine("  alpha: transparent=" + transparent + " opaque=" + opaque + " semi=" + semi);
                    builder.AppendLine("  corner alpha TL=" + pixels[(texture.height - 1) * texture.width].a +
                                       " TR=" + pixels[(texture.height - 1) * texture.width + texture.width - 1].a +
                                       " BL=" + pixels[0].a +
                                       " BR=" + pixels[texture.width - 1].a);
                }
                catch (System.Exception exception)
                {
                    builder.AppendLine("  alpha check failed: " + exception.Message);
                }
            }

            string path = @"D:\123\project\anothergame\crime\Temp\character_probe.txt";
            File.WriteAllText(path, builder.ToString());
            Debug.Log(builder.ToString());
        }
    }
}
