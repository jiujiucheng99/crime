using UnityEngine;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Small runtime pixel-art props for the Brick Row street slice.
    /// These are intentionally simple; replace with Aseprite finals later.
    /// </summary>
    public static class StreetPropFactory
    {
        public static GameObject Streetlight(Transform parent, Vector2 position)
        {
            if (TrySpawnGenerated(parent, "Streetlight", "streetlight", position, out GameObject generated)) return generated;

            Texture2D texture = NewTexture(16, 48);
            Ellipse(texture, 8, 3, 6, 2, new Color32(24, 22, 24, 255));
            Fill(texture, 7, 4, 2, 34, new Color32(45, 48, 54, 255));
            Fill(texture, 8, 37, 6, 2, new Color32(38, 40, 45, 255));
            Fill(texture, 12, 39, 4, 5, new Color32(32, 34, 38, 255));
            Fill(texture, 13, 40, 2, 3, new Color32(245, 190, 70, 255));
            Fill(texture, 12, 44, 4, 2, new Color32(245, 190, 70, 90));
            return Spawn(parent, "Streetlight", position, MakeSprite(texture, "Streetlight", new Vector2(0.5f, 0f)));
        }

        public static GameObject TrashCan(Transform parent, Vector2 position)
        {
            if (TrySpawnGenerated(parent, "TrashCan", "trashcan", position, out GameObject generated)) return generated;

            Texture2D texture = NewTexture(16, 24);
            Fill(texture, 3, 3, 10, 15, new Color32(55, 73, 55, 255));
            Fill(texture, 2, 18, 12, 4, new Color32(38, 43, 38, 255));
            Fill(texture, 4, 21, 8, 2, new Color32(28, 30, 28, 255));
            for (int x = 5; x <= 11; x += 2) Fill(texture, x, 5, 1, 12, new Color32(38, 51, 38, 255));
            Fill(texture, 7, 19, 2, 2, new Color32(25, 28, 25, 255));
            return Spawn(parent, "TrashCan", position, MakeSprite(texture, "TrashCan", new Vector2(0.5f, 0f)));
        }

        public static GameObject Dumpster(Transform parent, Vector2 position)
        {
            Texture2D texture = NewTexture(32, 24);
            Fill(texture, 2, 3, 28, 16, new Color32(47, 74, 58, 255));
            Fill(texture, 1, 18, 30, 4, new Color32(66, 89, 72, 255));
            Fill(texture, 4, 1, 4, 2, new Color32(24, 24, 24, 255));
            Fill(texture, 24, 1, 4, 2, new Color32(24, 24, 24, 255));
            Fill(texture, 4, 6, 24, 1, new Color32(38, 58, 46, 255));
            Fill(texture, 4, 10, 24, 1, new Color32(38, 58, 46, 255));
            Fill(texture, 15, 18, 2, 4, new Color32(30, 38, 32, 255));
            return Spawn(parent, "Dumpster", position, MakeSprite(texture, "Dumpster", new Vector2(0.5f, 0f)));
        }

        public static GameObject BenchBack(Transform parent, Vector2 position)
        {
            if (TrySpawnGenerated(parent, "BenchBack", "bench_back", position, out GameObject generated)) return generated;
            return Bench(parent, position);
        }

        public static GameObject Hydrant(Transform parent, Vector2 position)
        {
            if (TrySpawnGenerated(parent, "Hydrant", "hydrant", position, out GameObject generated)) return generated;

            Texture2D texture = NewTexture(12, 20);
            Fill(texture, 4, 2, 4, 12, new Color32(168, 50, 42, 255));
            Fill(texture, 3, 13, 6, 3, new Color32(190, 64, 52, 255));
            Fill(texture, 1, 8, 3, 5, new Color32(168, 50, 42, 255));
            Fill(texture, 8, 8, 3, 5, new Color32(168, 50, 42, 255));
            Fill(texture, 2, 1, 8, 2, new Color32(95, 35, 30, 255));
            Fill(texture, 5, 15, 2, 2, new Color32(210, 80, 64, 255));
            return Spawn(parent, "Hydrant", position, MakeSprite(texture, "Hydrant", new Vector2(0.5f, 0f)));
        }

        public static GameObject Bench(Transform parent, Vector2 position)
        {
            if (TrySpawnGenerated(parent, "Bench", "bench", position, out GameObject generated)) return generated;

            Texture2D texture = NewTexture(32, 16);            Fill(texture, 3, 1, 3, 6, new Color32(58, 45, 32, 255));
            Fill(texture, 26, 1, 3, 6, new Color32(58, 45, 32, 255));
            Fill(texture, 1, 6, 30, 4, new Color32(120, 77, 44, 255));
            Fill(texture, 2, 10, 28, 2, new Color32(150, 97, 55, 255));
            Fill(texture, 3, 12, 26, 2, new Color32(120, 77, 44, 255));
            return Spawn(parent, "Bench", position, MakeSprite(texture, "Bench", new Vector2(0.5f, 0f)));
        }

        public static GameObject ParkingMeter(Transform parent, Vector2 position)
        {
            Texture2D texture = NewTexture(8, 24);
            Fill(texture, 3, 1, 2, 14, new Color32(72, 76, 80, 255));
            Fill(texture, 1, 14, 6, 8, new Color32(48, 52, 58, 255));
            Fill(texture, 2, 17, 4, 3, new Color32(225, 160, 55, 255));
            Fill(texture, 2, 1, 4, 2, new Color32(40, 42, 46, 255));
            return Spawn(parent, "ParkingMeter", position, MakeSprite(texture, "ParkingMeter", new Vector2(0.5f, 0f)));
        }

        public static GameObject AcUnit(Transform parent, Vector2 position)
        {
            Texture2D texture = NewTexture(24, 16);
            Fill(texture, 1, 1, 22, 13, new Color32(105, 110, 114, 255));
            Fill(texture, 2, 2, 20, 11, new Color32(82, 87, 92, 255));
            for (int y = 4; y <= 10; y += 2) Fill(texture, 4, y, 16, 1, new Color32(58, 62, 66, 255));
            Ellipse(texture, 19, 7, 4, 4, new Color32(58, 62, 66, 255));
            return Spawn(parent, "AcUnit", position, MakeSprite(texture, "AcUnit", new Vector2(0.5f, 0f)));
        }

        public static GameObject PowerBox(Transform parent, Vector2 position)
        {
            Texture2D texture = NewTexture(16, 24);
            Fill(texture, 2, 2, 12, 19, new Color32(72, 89, 72, 255));
            Fill(texture, 3, 3, 10, 17, new Color32(58, 72, 58, 255));
            Fill(texture, 5, 14, 6, 3, new Color32(225, 180, 55, 255));
            Fill(texture, 7, 3, 2, 11, new Color32(38, 48, 38, 255));
            return Spawn(parent, "PowerBox", position, MakeSprite(texture, "PowerBox", new Vector2(0.5f, 0f)));
        }

        public static GameObject StreetSign(Transform parent, Vector2 position)
        {
            Texture2D texture = NewTexture(10, 34);
            Fill(texture, 4, 1, 2, 26, new Color32(72, 76, 80, 255));
            Fill(texture, 1, 26, 8, 6, new Color32(46, 92, 58, 255));
            Fill(texture, 2, 27, 6, 4, new Color32(82, 130, 86, 255));
            Fill(texture, 2, 29, 6, 1, new Color32(220, 220, 200, 255));
            return Spawn(parent, "StreetSign", position, MakeSprite(texture, "StreetSign", new Vector2(0.5f, 0f)));
        }

        public static GameObject CardboardBox(Transform parent, Vector2 position)
        {
            Texture2D texture = NewTexture(16, 16);
            Fill(texture, 2, 2, 12, 11, new Color32(122, 85, 50, 255));
            Fill(texture, 2, 2, 12, 2, new Color32(150, 108, 64, 255));
            Fill(texture, 7, 2, 2, 11, new Color32(180, 140, 88, 255));
            Fill(texture, 2, 11, 12, 1, new Color32(92, 62, 38, 255));
            return Spawn(parent, "CardboardBox", position, MakeSprite(texture, "CardboardBox", new Vector2(0.5f, 0f)));
        }

        public static GameObject Manhole(Transform parent, Vector2 position)
        {
            Texture2D texture = NewTexture(16, 16);
            Ellipse(texture, 8, 8, 7, 7, new Color32(48, 50, 52, 255));
            Ellipse(texture, 8, 8, 5, 5, new Color32(62, 64, 66, 255));
            Fill(texture, 3, 7, 10, 1, new Color32(38, 40, 42, 255));
            Fill(texture, 7, 3, 1, 10, new Color32(38, 40, 42, 255));
            return Spawn(parent, "Manhole", position, MakeSprite(texture, "Manhole", new Vector2(0.5f, 0.5f)), -860, false);
        }

        public static GameObject Puddle(Transform parent, Vector2 position, bool oil = false)
        {
            Texture2D texture = NewTexture(24, 10);
            Color c = oil
                ? new Color32(28, 24, 22, 120)
                : new Color32(55, 84, 96, 105);
            Ellipse(texture, 12, 5, 11, 4, c);
            Ellipse(texture, 10, 5, 7, 3, oil ? new Color32(18, 16, 15, 150) : new Color32(78, 112, 122, 90));
            return Spawn(parent, oil ? "OilStain" : "Puddle", position, MakeSprite(texture, oil ? "OilStain" : "Puddle", new Vector2(0.5f, 0.5f)), -855, false);
        }

        public static GameObject LampGlow(Transform parent, Vector2 position)
        {
            Texture2D texture = NewTexture(48, 24);
            Ellipse(texture, 24, 12, 22, 9, new Color32(245, 190, 70, 26));
            Ellipse(texture, 24, 12, 14, 6, new Color32(245, 190, 70, 34));
            return Spawn(parent, "LampGlow", position, MakeSprite(texture, "LampGlow", new Vector2(0.5f, 0.5f)), -854, false);
        }

        public static GameObject Mailbox(Transform parent, Vector2 position)
        {
            Texture2D texture = NewTexture(10, 16);
            Fill(texture, 3, 1, 2, 7, new Color32(50, 52, 56, 255));
            Fill(texture, 1, 7, 8, 7, new Color32(38, 54, 72, 255));
            Fill(texture, 2, 8, 6, 3, new Color32(58, 78, 98, 255));
            Fill(texture, 3, 12, 4, 1, new Color32(210, 180, 90, 255));
            return Spawn(parent, "Mailbox", position, MakeSprite(texture, "Mailbox", new Vector2(0.5f, 0f)));
        }

        public static GameObject TrashBag(Transform parent, Vector2 position)
        {
            Texture2D texture = NewTexture(12, 10);
            Ellipse(texture, 6, 4, 5, 4, new Color32(28, 28, 30, 255));
            Ellipse(texture, 4, 5, 3, 3, new Color32(38, 38, 40, 255));
            Fill(texture, 5, 7, 2, 2, new Color32(20, 20, 22, 255));
            return Spawn(parent, "TrashBag", position, MakeSprite(texture, "TrashBag", new Vector2(0.5f, 0f)));
        }

        private static bool TrySpawnGenerated(Transform parent, string name, string fileName, Vector2 position, out GameObject go)
        {
            Sprite sprite = null;
            Sprite[] available = Resources.LoadAll<Sprite>("Environment/BrickRow/Props");
            foreach (Sprite candidate in available)
            {
                string lowerName = candidate.name.ToLowerInvariant();
                if (fileName == "bench" && lowerName.Contains("back")) continue;
                if (lowerName.Contains(fileName))
                {
                    sprite = candidate;
                    break;
                }
            }

            if (sprite == null)
            {
                go = null;
                return false;
            }

            float targetHeight = fileName switch
            {
                "streetlight" => 2f,
                "trashcan" => 1f,
                "bench" => 0.75f,
                "bench_back" => 0.75f,
                "hydrant" => 0.75f,
                _ => 0f
            };

            go = Spawn(parent, name, position, sprite, null, true, targetHeight > 0f ? targetHeight : (float?)null);
            return true;
        }

        private static GameObject Spawn(
            Transform parent,
            string name,
            Vector2 position,
            Sprite sprite,
            int? fixedOrder = null,
            bool alignBottom = true,
            float? targetHeight = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            float scale = targetHeight.HasValue && sprite.bounds.size.y > 0.001f
                ? targetHeight.Value / sprite.bounds.size.y
                : 1f;
            go.transform.localScale = Vector3.one * scale;

            // Put the sprite's bottom edge on the prop position.
            float y = alignBottom ? position.y - sprite.bounds.min.y * scale : position.y;
            go.transform.position = new Vector3(position.x, y, 0f);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = fixedOrder ?? Mathf.RoundToInt(-position.y * 100f);
            return go;
        }

        private static Texture2D NewTexture(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;
            texture.SetPixels(pixels);
            return texture;
        }

        private static Sprite MakeSprite(Texture2D texture, string name, Vector2 pivot)
        {
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, 32f);
            sprite.name = name;
            return sprite;
        }

        private static void Fill(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            for (int yy = y; yy < y + height; yy++)
            {
                for (int xx = x; xx < x + width; xx++)
                {
                    if (xx >= 0 && xx < texture.width && yy >= 0 && yy < texture.height)
                    {
                        texture.SetPixel(xx, yy, color);
                    }
                }
            }
        }

        private static void Ellipse(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
            for (int y = -radiusY; y <= radiusY; y++)
            {
                for (int x = -radiusX; x <= radiusX; x++)
                {
                    float nx = x / (float)radiusX;
                    float ny = y / (float)radiusY;
                    if (nx * nx + ny * ny <= 1f)
                    {
                        int px = centerX + x;
                        int py = centerY + y;
                        if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                        {
                            texture.SetPixel(px, py, color);
                        }
                    }
                }
            }
        }
    }
}
