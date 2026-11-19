using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Locked safe interaction: hold C for five seconds, then swap to the open
    /// sprite and expose the storage container. Only used on other people's safes.
    /// </summary>
    public class SafeLockController : MonoBehaviour
    {
        private const float Range = 1.35f;
        private const float HoldDuration = 5f;

        private Transform _player;
        private SpriteRenderer _renderer;
        private Sprite _closedSprite;
        private Sprite _openSprite;
        private StorageContainer _storage;
        private PlayerWallet _wallet;
        private bool _unlocked;
        private float _hold;
        private GUIStyle _progressTextStyle;

        public bool IsLocked => !_unlocked;

        public void Configure(Transform player, SpriteRenderer renderer, Sprite openSprite,
            StorageContainer storage, PlayerWallet wallet = null)
        {
            _player = player;
            _renderer = renderer;
            _storage = storage;
            _wallet = wallet;
            if (_renderer != null) _closedSprite = _renderer.sprite;
            _openSprite = openSprite;
        }

        private void Update()
        {
            if (_unlocked || _player == null) return;

            if (StorageInteraction.AnyPanelOpen || BusTravelController.IsOpen ||
                DialogueChoiceController.IsOpen || StoreShopUI.IsOpen)
            {
                _hold = 0f;
                return;
            }

            bool near = Vector2.Distance(_player.position, transform.position) <= Range;
            bool holding = near && Keyboard.current != null && Keyboard.current.cKey.isPressed;

            if (holding)
            {
                _hold = Mathf.Min(HoldDuration, _hold + Time.deltaTime);
                if (_hold >= HoldDuration) Unlock();
            }
            else
            {
                _hold = 0f;
            }
        }

        private void Unlock()
        {
            _unlocked = true;
            _hold = HoldDuration;
            if (_renderer != null && _openSprite != null) _renderer.sprite = _openSprite;
            if (_storage != null) _storage.TryAdd(StolenGoods.CreateRandomStack());
            if (_wallet != null) _wallet.Add(Random.Range(12, 31));
        }

        private void OnGUI()
        {
            if (_player == null) return;

            bool near = !_unlocked && Vector2.Distance(_player.position, transform.position) <= Range;
            InteractionPromptUI.SetPrompt("safe", near ? "保险箱" : string.Empty);
            if (!near || _hold <= 0f) return;

            EnsureStyle();
            float progress = Mathf.Clamp01(_hold / HoldDuration);
            Rect box = new Rect(Screen.width * 0.5f - 150f, Screen.height - 168f, 300f, 36f);
            DrawSolid(box, new Color(0.055f, 0.065f, 0.08f, 0.94f));
            DrawBorder(box, new Color(0.62f, 0.52f, 0.32f, 1f), 1f);
            Rect fill = new Rect(box.x + 4f, box.y + 4f, (box.width - 8f) * progress, box.height - 8f);
            DrawSolid(fill, new Color(0.48f, 0.38f, 0.20f, 0.95f));
            GUI.Label(box, "开锁中 " + _hold.ToString("F1") + " / 5.0", _progressTextStyle);
        }

        private void EnsureStyle()
        {
            if (_progressTextStyle != null) return;
            Font font = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei", "Microsoft YaHei UI", "SimHei", "Noto Sans CJK SC", "Arial" }, 16);
            _progressTextStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.90f, 0.74f) }
            };
        }

        private static void DrawSolid(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static void DrawBorder(Rect rect, Color color, float thickness)
        {
            DrawSolid(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawSolid(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            DrawSolid(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawSolid(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }
    }
}
