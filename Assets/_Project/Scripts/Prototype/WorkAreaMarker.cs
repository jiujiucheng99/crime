using UnityEngine;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Small bobbing world marker used to make work stations easy to find.
    /// </summary>
    public class WorkAreaMarker : MonoBehaviour
    {
        private Vector3 _baseLocalPosition;
        private SpriteRenderer _renderer;
        private float _phase;

        public static GameObject Create(Transform parent, string name, Vector3 localPosition, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = GetArrowSprite();
            renderer.color = color;
            renderer.sortingOrder = 5000;

            WorkAreaMarker marker = go.AddComponent<WorkAreaMarker>();
            marker._baseLocalPosition = localPosition;
            marker._renderer = renderer;
            marker._phase = Random.value * 6f;
            return go;
        }

        private void Update()
        {
            transform.localPosition = _baseLocalPosition +
                new Vector3(0f, Mathf.Sin(Time.time * 3f + _phase) * 0.12f, 0f);
            if (_renderer != null)
            {
                Color color = _renderer.color;
                color.a = 0.78f + Mathf.Sin(Time.time * 3f + _phase) * 0.16f;
                _renderer.color = color;
            }
        }

        private static Sprite _arrowSprite;
        private static Sprite GetArrowSprite()
        {
            if (_arrowSprite != null) return _arrowSprite;
            Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[32 * 32];
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    int dx = Mathf.Abs(x - 15);
                    int top = 24 - y;
                    bool arrow = y >= 5 && y <= 21 && dx <= (y - 5) / 2 + 2;
                    bool stem = x >= 13 && x <= 18 && y >= 20 && y <= 28;
                    if (arrow || stem)
                    {
                        bool edge = dx >= (y - 5) / 2 || y <= 7 || y >= 27;
                        pixels[y * 32 + x] = edge
                            ? new Color(0.30f, 0.20f, 0.08f, 1f)
                            : new Color(1f, 0.86f, 0.34f, 1f);
                    }
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            _arrowSprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            _arrowSprite.name = "WorkAreaArrow";
            return _arrowSprite;
        }
    }
}
