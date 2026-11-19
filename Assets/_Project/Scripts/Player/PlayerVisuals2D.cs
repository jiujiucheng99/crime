using System.IO;
using System.Text;
using UnityEngine;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Static-sprite movement presentation for the first playable test.
    /// Uses front/back/side images, side mirroring, a small bob, lean and a blob shadow.
    /// Walk frames can be added later without changing the motor.
    /// </summary>
    public sealed class PlayerVisuals2D : MonoBehaviour
    {
        [SerializeField] private PlayerMotor2D motor;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private SpriteRenderer shadowRenderer;

        [Header("Presentation")]
        [SerializeField] private float targetHeight = 1.5f; // 48 px at 32 PPU
        [SerializeField] private float bobHeight = 0.045f;
        [SerializeField] private float bobSpeed = 10f;
        [SerializeField] private float leanAngle = 2.5f;

        public static string DebugSummary { get; private set; } = "sprites: not loaded";

        private Sprite _front;
        private Sprite _back;
        private Sprite _side;
        private Vector3 _baseVisualLocalPosition;
        private Direction _direction = Direction.Down;
        private bool _sideFacesLeft = true;

        private enum Direction
        {
            Down,
            Up,
            Side
        }

        private void Awake()
        {
            if (motor == null) motor = GetComponent<PlayerMotor2D>();

            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                GameObject visual = new GameObject("Visual");
                visual.transform.SetParent(transform, false);
                spriteRenderer = visual.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sortingOrder = 100;
            LoadSprites();
            CreateShadow();
            ApplySprite(_front);
        }

        private void LateUpdate()
        {
            if (motor == null || spriteRenderer == null) return;

            Vector2 input = motor.MoveInput;
            if (input.sqrMagnitude > 0.001f)
            {
                if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                {
                    _direction = Direction.Side;
                    _sideFacesLeft = input.x < 0f;
                }
                else
                {
                    _direction = input.y >= 0f ? Direction.Up : Direction.Down;
                }
            }

            Sprite target = _direction switch
            {
                Direction.Up => _back,
                Direction.Side => _side,
                _ => _front
            };

            if (spriteRenderer.sprite != target)
            {
                ApplySprite(target);
            }

            bool moving = motor.Velocity.sqrMagnitude > 0.02f;
            float bob = moving ? Mathf.Abs(Mathf.Sin(Time.time * bobSpeed)) * bobHeight : 0f;
            float lean = moving ? -input.x * leanAngle : 0f;

            spriteRenderer.flipX = _direction == Direction.Side && !_sideFacesLeft;
            spriteRenderer.transform.localPosition = _baseVisualLocalPosition + Vector3.up * bob;
            spriteRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, lean);

            // Use the same Y-sort formula as street props. North of a prop's base
            // means a lower order (behind the prop); south means in front.
            int sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f) + 1;
            spriteRenderer.sortingOrder = sortingOrder;

            if (shadowRenderer != null)
            {
                shadowRenderer.transform.localRotation = Quaternion.identity;
                shadowRenderer.transform.localScale = new Vector3(1.1f - bob * 2f, 0.45f - bob, 1f);
                shadowRenderer.sortingOrder = sortingOrder - 1;
            }
        }

        private void LoadSprites()
        {
            Sprite[] all = Resources.LoadAll<Sprite>("Characters/Kai");
            StringBuilder log = new StringBuilder();
            log.AppendLine("PlayerVisuals2D sprite load");
            log.AppendLine("count=" + all.Length);

            foreach (Sprite sprite in all)
            {
                string lower = sprite.name.ToLowerInvariant();
                log.AppendLine(sprite.name + " | rect=" + sprite.rect + " | bounds=" + sprite.bounds);

                if (lower.Contains("down") || lower.Contains("front"))
                {
                    _front = sprite;
                }
                else if (lower.Contains("up") || lower.Contains("back"))
                {
                    _back = sprite;
                }
                else if (lower.Contains("side") || lower.Contains("left"))
                {
                    _side = sprite;
                }
            }

            if (_front == null && all.Length > 0) _front = all[0];
            if (_back == null) _back = _front;
            if (_side == null) _side = _front;

            if (_front == null)
            {
                _front = CreatePlaceholder(new Color(0.20f, 0.45f, 0.65f), "Placeholder_Front");
                _back = CreatePlaceholder(new Color(0.18f, 0.28f, 0.42f), "Placeholder_Back");
                _side = CreatePlaceholder(new Color(0.16f, 0.38f, 0.58f), "Placeholder_Side");
                log.AppendLine("No sprites found under Resources/Characters/Kai. Placeholders created.");
            }

            DebugSummary = "sprites: " + all.Length + " | front=" + (_front != null ? _front.name : "null");
            WriteLog(log.ToString());
        }

        private void ApplySprite(Sprite sprite)
        {
            if (sprite == null || spriteRenderer == null) return;

            spriteRenderer.sprite = sprite;
            float spriteHeight = Mathf.Max(0.001f, sprite.bounds.size.y);
            float scale = targetHeight / spriteHeight;
            spriteRenderer.transform.localScale = Vector3.one * scale;

            float footOffset = -sprite.bounds.min.y * scale;
            _baseVisualLocalPosition = new Vector3(0f, footOffset, 0f);
            spriteRenderer.transform.localPosition = _baseVisualLocalPosition;
        }

        private void CreateShadow()
        {
            GameObject shadow = new GameObject("Shadow");
            shadow.transform.SetParent(transform, false);
            shadow.transform.localPosition = new Vector3(0f, -0.02f, 0f);
            shadow.transform.localScale = new Vector3(1.1f, 0.45f, 1f);

            shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = CreateShadowSprite();
            shadowRenderer.color = new Color(0f, 0f, 0f, 0.35f);
            shadowRenderer.sortingOrder = 90;
        }

        private static Sprite CreateShadowSprite()
        {
            const int width = 64;
            const int height = 32;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[width * height];
            Vector2 center = new Vector2(width * 0.5f, height * 0.5f);
            Vector2 radius = new Vector2(width * 0.5f, height * 0.5f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = (x - center.x) / radius.x;
                    float ny = (y - center.y) / radius.y;
                    float distance = Mathf.Sqrt(nx * nx + ny * ny);
                    float alpha = Mathf.Clamp01(1f - distance);
                    alpha = alpha * alpha * 0.55f;
                    pixels[y * width + x] = new Color(0f, 0f, 0f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 32f);
        }

        private static Sprite CreatePlaceholder(Color color, string name)
        {
            const int width = 32;
            const int height = 48;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            for (int y = 6; y < 40; y++)
            {
                for (int x = 8; x < 24; x++) pixels[y * width + x] = color;
            }
            for (int y = 36; y < 46; y++)
            {
                for (int x = 10; x < 22; x++) pixels[y * width + x] = new Color(0.85f, 0.68f, 0.55f);
            }

            texture.SetPixels(pixels);
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0f), 32f);
            sprite.name = name;
            return sprite;
        }

        private static void WriteLog(string message)
        {
            try
            {
                string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/ambercity_movement.log"));
                File.AppendAllText(path, message + "\n");
            }
            catch
            {
                // Logging must never break the prototype.
            }
        }
    }
}
