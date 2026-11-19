using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Small bottom-right NPC dialogue chooser. It starts with one option (bus
    /// ride) and is structured so more options can be added later.
    /// </summary>
    public class DialogueChoiceController : MonoBehaviour
    {
        public static DialogueChoiceController Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance._open;

        private Transform _player;
        private PlayerMotor2D _motor;
        private Action _onOption;
        private Action _onOption2;
        private Action _onOption3;
        private string _speaker;
        private string _line;
        private string _optionLabel;
        private string _optionLabel2;
        private string _optionLabel3;
        private bool _open;
        private bool _infoMode;
        private bool _longText;
        private Vector2 _scroll;
        private GUIStyle _speakerStyle;
        private GUIStyle _textStyle;
        private GUIStyle _optionStyle;
        private GUIStyle _closeStyle;

        public void Configure(Transform player)
        {
            Instance = this;
            _player = player;
            _motor = player != null ? player.GetComponent<PlayerMotor2D>() : null;
        }

        public void Open(string speaker, string line, string optionLabel, Action onOption)
        {
            if (_open) return;
            _speaker = speaker;
            _line = line;
            _optionLabel = optionLabel;
            _optionLabel2 = null;
            _optionLabel3 = null;
            _onOption = onOption;
            _onOption2 = null;
            _onOption3 = null;
            _open = true;
            _infoMode = false;
            _longText = false;
            if (_motor != null) _motor.SetInputEnabled(false);
        }

        public void OpenChoice(string speaker, string line,
            string optionLabel, Action onOption, string optionLabel2, Action onOption2)
        {
            if (_open) return;
            _speaker = speaker;
            _line = line;
            _optionLabel = optionLabel;
            _optionLabel2 = optionLabel2;
            _optionLabel3 = null;
            _onOption = onOption;
            _onOption2 = onOption2;
            _onOption3 = null;
            _open = true;
            _infoMode = false;
            _longText = false;
            if (_motor != null) _motor.SetInputEnabled(false);
        }

        public void OpenChoice3(string speaker, string line,
            string optionLabel, Action onOption,
            string optionLabel2, Action onOption2,
            string optionLabel3, Action onOption3)
        {
            if (_open) return;
            _speaker = speaker;
            _line = line;
            _optionLabel = optionLabel;
            _optionLabel2 = optionLabel2;
            _optionLabel3 = optionLabel3;
            _onOption = onOption;
            _onOption2 = onOption2;
            _onOption3 = onOption3;
            _open = true;
            _infoMode = false;
            _longText = false;
            if (_motor != null) _motor.SetInputEnabled(false);
        }

        public void OpenNotice(string speaker, string line)
        {
            if (_open) return;
            _speaker = speaker;
            _line = line;
            _optionLabel = null;
            _optionLabel2 = null;
            _optionLabel3 = null;
            _onOption = null;
            _onOption2 = null;
            _onOption3 = null;
            _open = true;
            _infoMode = false;
            _longText = false;
            if (_motor != null) _motor.SetInputEnabled(false);
        }

        public void OpenStory(string speaker, string line,
            string optionLabel, Action onOption,
            string optionLabel2, Action onOption2,
            string optionLabel3, Action onOption3)
        {
            OpenChoice3(speaker, line, optionLabel, onOption, optionLabel2, onOption2, optionLabel3, onOption3);
            _longText = true;
        }

        public void OpenInfo(string speaker, string line)
        {
            if (_open) return;
            _speaker = speaker;
            _line = line;
            _optionLabel = null;
            _optionLabel2 = null;
            _optionLabel3 = null;
            _onOption = null;
            _onOption2 = null;
            _onOption3 = null;
            _open = true;
            _infoMode = true;
            _longText = false;
            _scroll = Vector2.zero;
            if (_motor != null) _motor.SetInputEnabled(false);
        }

        private void Close()
        {
            _open = false;
            _infoMode = false;
            _scroll = Vector2.zero;
            _onOption = null;
            _onOption2 = null;
            _onOption3 = null;
            if (_motor != null) _motor.SetInputEnabled(true);
        }

        private void Update()
        {
            if (!_open) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        private void Choose()
        {
            Action callback = _onOption;
            Close();
            callback?.Invoke();
        }

        private void OnGUI()
        {
            if (!_open) return;

            EnsureStyles();
            if (_infoMode)
            {
                DrawInfoPanel();
                return;
            }
            int optionCount = (string.IsNullOrEmpty(_optionLabel) ? 0 : 1) +
                              (string.IsNullOrEmpty(_optionLabel2) ? 0 : 1) +
                              (string.IsNullOrEmpty(_optionLabel3) ? 0 : 1);
            float panelHeight = 248f + Mathf.Max(0, optionCount - 1) * 38f + (_longText ? 54f : 0f);
            float speechHeight = _longText ? 108f : 54f;
            Rect panel = new Rect(Screen.width - 380f, Screen.height - panelHeight - 20f, 356f, panelHeight);
            DrawSolid(panel, new Color(0.055f, 0.070f, 0.080f, 0.99f));
            DrawBorder(panel, new Color(0.62f, 0.52f, 0.32f, 1f), 2f);
            DrawBorder(new Rect(panel.x + 4f, panel.y + 4f, panel.width - 8f, panel.height - 8f),
                new Color(0.20f, 0.25f, 0.27f, 1f), 1f);

            DrawSolid(new Rect(panel.x + 6f, panel.y + 6f, panel.width - 12f, 44f),
                new Color(0.10f, 0.13f, 0.15f, 1f));
            DrawSolid(new Rect(panel.x + 20f, panel.y + 15f, 4f, 26f),
                new Color(0.82f, 0.64f, 0.28f, 1f));
            GUI.Label(new Rect(panel.x + 34f, panel.y + 12f, panel.width - 50f, 30f), _speaker, _speakerStyle);

            Rect speech = new Rect(panel.x + 18f, panel.y + 58f, panel.width - 36f, speechHeight);
            DrawSolid(speech, new Color(0.075f, 0.090f, 0.105f, 1f));
            DrawBorder(speech, new Color(0.28f, 0.32f, 0.33f, 1f), 1f);
            DrawSolid(new Rect(speech.x + 8f, speech.y + 9f, 3f, speech.height - 18f),
                new Color(0.72f, 0.58f, 0.30f, 1f));
            float speechContentWidth = speech.width - 30f;
            float textWidth = speechContentWidth - 18f;
            float speechContentHeight = Mathf.Max(speech.height - 8f,
                _textStyle.CalcHeight(new GUIContent(_line), textWidth) + 8f);
            _scroll = GUI.BeginScrollView(speech, _scroll,
                new Rect(0f, 0f, speechContentWidth, speechContentHeight), false, false);
            GUI.Label(new Rect(18f, 0f, textWidth, speechContentHeight), _line, _textStyle);
            GUI.EndScrollView();

            float optionY = panel.y + 58f + speechHeight + 10f;
            if (!string.IsNullOrEmpty(_optionLabel))
            {
                if (DrawOption(panel, optionY, _optionLabel, _onOption)) return;
                optionY += 42f;
            }
            if (!string.IsNullOrEmpty(_optionLabel2))
            {
                if (DrawOption(panel, optionY, _optionLabel2, _onOption2)) return;
                optionY += 42f;
            }
            if (!string.IsNullOrEmpty(_optionLabel3))
            {
                if (DrawOption(panel, optionY, _optionLabel3, _onOption3)) return;
                optionY += 42f;
            }

            Rect closeRect = new Rect(panel.x + 24f, optionY, panel.width - 48f, 32f);
            bool closeHover = closeRect.Contains(Event.current.mousePosition);
            DrawSolid(closeRect, closeHover
                ? new Color(0.15f, 0.18f, 0.18f, 1f)
                : new Color(0.08f, 0.10f, 0.11f, 1f));
            DrawBorder(closeRect, new Color(0.25f, 0.28f, 0.28f, 1f), 1f);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                closeRect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                Close();
                return;
            }
            _closeStyle.normal.textColor = closeHover
                ? new Color(1f, 0.83f, 0.48f)
                : new Color(0.72f, 0.72f, 0.66f);
            GUI.Label(closeRect, "结束对话", _closeStyle);
        }

        private void DrawInfoPanel()
        {
            Rect panel = new Rect(Screen.width * 0.5f - 320f, Screen.height * 0.5f - 190f, 640f, 380f);
            DrawSolid(panel, new Color(0.055f, 0.070f, 0.080f, 0.99f));
            DrawBorder(panel, new Color(0.62f, 0.52f, 0.32f, 1f), 2f);
            DrawBorder(new Rect(panel.x + 4f, panel.y + 4f, panel.width - 8f, panel.height - 8f),
                new Color(0.20f, 0.25f, 0.27f, 1f), 1f);

            DrawSolid(new Rect(panel.x + 6f, panel.y + 6f, panel.width - 12f, 52f),
                new Color(0.10f, 0.13f, 0.15f, 1f));
            DrawSolid(new Rect(panel.x + 20f, panel.y + 15f, 4f, 30f),
                new Color(0.82f, 0.64f, 0.28f, 1f));
            GUI.Label(new Rect(panel.x + 34f, panel.y + 10f, panel.width - 50f, 36f), _speaker, _speakerStyle);

            Rect body = new Rect(panel.x + 24f, panel.y + 72f, panel.width - 48f, panel.height - 132f);
            DrawSolid(body, new Color(0.075f, 0.090f, 0.105f, 1f));
            DrawBorder(body, new Color(0.28f, 0.32f, 0.33f, 1f), 1f);
            float bodyContentWidth = body.width - 36f;
            float bodyContentHeight = Mathf.Max(body.height - 24f,
                _textStyle.CalcHeight(new GUIContent(_line), bodyContentWidth));
            _scroll = GUI.BeginScrollView(body, _scroll,
                new Rect(0f, 0f, bodyContentWidth, bodyContentHeight), false, false);
            GUI.Label(new Rect(0f, 0f, bodyContentWidth, bodyContentHeight), _line, _textStyle);
            GUI.EndScrollView();

            Rect closeRect = new Rect(panel.x + 24f, panel.y + panel.height - 50f, panel.width - 48f, 34f);
            bool closeHover = closeRect.Contains(Event.current.mousePosition);
            DrawSolid(closeRect, closeHover
                ? new Color(0.18f, 0.22f, 0.24f, 1f)
                : new Color(0.09f, 0.12f, 0.13f, 1f));
            DrawBorder(closeRect, new Color(0.32f, 0.36f, 0.36f, 1f), 1f);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                closeRect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                Close();
                return;
            }
            GUI.Label(closeRect, "关闭   Esc", _closeStyle);
        }

        private bool DrawOption(Rect panel, float y, string label, Action action)
        {
            Rect optionRect = new Rect(panel.x + 24f, y, panel.width - 48f, 38f);
            bool hover = optionRect.Contains(Event.current.mousePosition);
            DrawSolid(optionRect, hover
                ? new Color(0.18f, 0.22f, 0.24f, 1f)
                : new Color(0.09f, 0.12f, 0.13f, 1f));
            DrawBorder(optionRect, hover
                ? new Color(0.72f, 0.60f, 0.36f, 1f)
                : new Color(0.25f, 0.30f, 0.31f, 1f), 1f);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                optionRect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                Close();
                action?.Invoke();
                return true;
            }
            _optionStyle.normal.textColor = hover
                ? new Color(1f, 0.83f, 0.48f)
                : new Color(0.92f, 0.90f, 0.82f);
            GUI.Label(new Rect(optionRect.x + 18f, optionRect.y, optionRect.width - 36f, optionRect.height), label, _optionStyle);
            return false;
        }

        private void EnsureStyles()
        {
            if (_speakerStyle != null) return;

            Font font = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei", "Microsoft YaHei UI", "SimHei", "Noto Sans CJK SC", "Arial" }, 18);

            _speakerStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.96f, 0.91f, 0.78f) }
            };
            _textStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                normal = { textColor = new Color(0.82f, 0.84f, 0.80f) }
            };
            _optionStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 17,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.92f, 0.90f, 0.82f) }
            };
            _closeStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.72f, 0.72f, 0.66f) }
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
