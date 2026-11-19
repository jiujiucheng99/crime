using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Timing-bar cooking minigame. Space stops the pointer; green center gives
    /// the best quality and largest tip.
    /// </summary>
    public class CookingMinigameUI : MonoBehaviour
    {
        private const float PointerSpeed = 0.55f;

        public static bool IsOpen => Active != null && Active._open;
        public static CookingMinigameUI Active { get; private set; }

        private Transform _player;
        private PlayerMotor2D _motor;
        private CookingRecipe _recipe;
        private Action<int> _onComplete;
        private bool _open;
        private float _pointer;
        private float _greenHalf;
        private float _resultTimer;
        private int _resultQuality;
        private int _reward;
        private GUIStyle _titleStyle;
        private GUIStyle _textStyle;
        private GUIStyle _smallStyle;

        public void Configure(Transform player)
        {
            _player = player;
            _motor = player != null ? player.GetComponent<PlayerMotor2D>() : null;
        }

        public void Open(CookingRecipe recipe, int cookingLevel, Action<int> onComplete)
        {
            if (_open) return;
            Active = this;
            _recipe = recipe;
            _onComplete = onComplete;
            _open = true;
            _pointer = 0f;
            _resultTimer = 0f;
            _greenHalf = SkillEffects.CookingGreenHalf(cookingLevel);
            if (_motor != null) _motor.SetInputEnabled(false);
        }

        private void Update()
        {
            if (!_open) return;

            if (_resultQuality > 0)
            {
                _resultTimer -= Time.deltaTime;
                if (_resultTimer <= 0f) FinishAndClose();
                return;
            }

            _pointer = Mathf.MoveTowards(_pointer, 1f, PointerSpeed * Time.deltaTime);
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                Evaluate();
            }
            else if (_pointer >= 1f)
            {
                Evaluate();
            }
        }

        private void Evaluate()
        {
            float distance = Mathf.Abs(_pointer - 0.5f);
            if (distance <= _greenHalf)
            {
                _resultQuality = 3;
                _reward = Mathf.RoundToInt(_recipe.BaseReward * 1.5f);
            }
            else if (distance <= _greenHalf + 0.13f)
            {
                _resultQuality = 2;
                _reward = _recipe.BaseReward;
            }
            else
            {
                _resultQuality = 1;
                _reward = Mathf.RoundToInt(_recipe.BaseReward * 0.5f);
            }
            _resultTimer = 1.6f;
        }

        private void FinishAndClose()
        {
            int reward = _reward;
            _open = false;
            Active = null;
            if (_motor != null) _motor.SetInputEnabled(true);
            Action<int> callback = _onComplete;
            _onComplete = null;
            callback?.Invoke(reward);
        }

        private void OnGUI()
        {
            if (!_open) return;
            EnsureStyles();

            Rect panel = new Rect(Screen.width * 0.5f - 260f, Screen.height * 0.5f - 90f, 520f, 180f);
            DrawSolid(panel, new Color(0.055f, 0.065f, 0.075f, 0.99f));
            DrawBorder(panel, new Color(0.62f, 0.52f, 0.32f, 1f), 2f);
            GUI.Label(new Rect(panel.x + 18f, panel.y + 10f, 260f, 28f), "烹饪：" + _recipe.Name, _titleStyle);
            GUI.Label(new Rect(panel.x + 18f, panel.y + 38f, panel.width - 36f, 22f),
                "指针到绿色区域时按空格", _smallStyle);

            Rect bar = new Rect(panel.x + 24f, panel.y + 72f, panel.width - 48f, 42f);
            DrawBar(bar);

            float pointerX = bar.x + bar.width * _pointer;
            DrawSolid(new Rect(pointerX - 2f, bar.y - 6f, 4f, bar.height + 12f),
                new Color(1f, 1f, 1f, 0.95f));

            if (_resultQuality > 0)
            {
                string quality = _resultQuality == 3 ? "完美" : (_resultQuality == 2 ? "不错" : "失败");
                GUI.Label(new Rect(panel.x + 18f, panel.y + 124f, panel.width - 36f, 28f),
                    quality + "   获得 " + _reward + " 块", _titleStyle);
            }
            else
            {
                GUI.Label(new Rect(panel.x + 18f, panel.y + 124f, panel.width - 36f, 22f),
                    "空格停止指针", _textStyle);
            }
        }

        private void DrawBar(Rect bar)
        {
            float greenStart = 0.5f - _greenHalf;
            float greenEnd = 0.5f + _greenHalf;
            float yellowStart = greenStart - 0.13f;
            float yellowEnd = greenEnd + 0.13f;

            DrawSegment(bar, 0f, yellowStart, new Color(0.62f, 0.16f, 0.16f));
            DrawSegment(bar, yellowStart, greenStart, new Color(0.72f, 0.58f, 0.18f));
            DrawSegment(bar, greenStart, greenEnd, new Color(0.24f, 0.62f, 0.28f));
            DrawSegment(bar, greenEnd, yellowEnd, new Color(0.72f, 0.58f, 0.18f));
            DrawSegment(bar, yellowEnd, 1f, new Color(0.62f, 0.16f, 0.16f));
            DrawBorder(bar, new Color(0.72f, 0.66f, 0.50f, 1f), 1f);
        }

        private void DrawSegment(Rect bar, float start, float end, Color color)
        {
            if (end <= start) return;
            DrawSolid(new Rect(bar.x + bar.width * start, bar.y, bar.width * (end - start), bar.height), color);
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null) return;
            Font font = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei", "Microsoft YaHei UI", "SimHei", "Noto Sans CJK SC", "Arial" }, 18);
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                font = font, fontSize = 18, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.96f, 0.91f, 0.78f) }
            };
            _textStyle = new GUIStyle(GUI.skin.label)
            {
                font = font, fontSize = 15, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.92f, 0.90f, 0.82f) }
            };
            _smallStyle = new GUIStyle(GUI.skin.label)
            {
                font = font, fontSize = 13,
                normal = { textColor = new Color(0.68f, 0.72f, 0.70f) }
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
