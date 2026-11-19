using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Hold-C search time for ordinary containers in the neighbour's apartment.
    /// Unlike the safe, this never changes the container sprite.
    /// </summary>
    public class ContainerSearchController : MonoBehaviour
    {
        private const float Range = 1.35f;
        private const float HoldDuration = 5f;

        private static ContainerSearchController _activePromptOwner;

        private Transform _player;
        private StorageContainer _storage;
        private string _label;
        private bool _searched;
        private float _hold;
        private GUIStyle _textStyle;

        public bool IsSearched => _searched;

        public void Configure(Transform player, StorageContainer storage, string label)
        {
            _player = player;
            _storage = storage;
            _label = label;
        }

        private void Update()
        {
            if (_searched || _player == null) return;

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
                TheftNoise.Add(12f * Time.deltaTime);
                if (_hold >= HoldDuration)
                {
                    _searched = true;
                    TheftNoise.Add(8f);
                }
            }
            else
            {
                _hold = 0f;
            }
        }

        private void OnGUI()
        {
            if (_player == null)
            {
                ClearIfOwner();
                return;
            }

            bool near = Vector2.Distance(_player.position, transform.position) <= Range;
            if (_searched || !near)
            {
                ClearIfOwner();
                return;
            }

            _activePromptOwner = this;
            InteractionPromptUI.SetPrompt("search", _label);
            if (_hold <= 0f) return;

            EnsureStyle();
            float progress = Mathf.Clamp01(_hold / HoldDuration);
            Rect box = new Rect(Screen.width * 0.5f - 150f, Screen.height - 168f, 300f, 36f);
            DrawSolid(box, new Color(0.055f, 0.065f, 0.08f, 0.94f));
            DrawBorder(box, new Color(0.62f, 0.52f, 0.32f, 1f), 1f);
            DrawSolid(new Rect(box.x + 4f, box.y + 4f, (box.width - 8f) * progress, box.height - 8f),
                new Color(0.48f, 0.38f, 0.20f, 0.95f));
            GUI.Label(box, "搜索中 " + _hold.ToString("F1") + " / 5.0", _textStyle);
        }

        private void ClearIfOwner()
        {
            if (_activePromptOwner != this) return;
            _activePromptOwner = null;
            InteractionPromptUI.SetPrompt("search", string.Empty);
        }

        private void OnDisable()
        {
            ClearIfOwner();
        }

        private void EnsureStyle()
        {
            if (_textStyle != null) return;
            Font font = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei", "Microsoft YaHei UI", "SimHei", "Noto Sans CJK SC", "Arial" }, 16);
            _textStyle = new GUIStyle(GUI.skin.label)
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
