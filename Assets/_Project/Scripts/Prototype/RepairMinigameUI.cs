using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Four-bolt repair timing game. Space stops the pointer for each bolt;
    /// misses add time/mistakes. Mechanics skill widens the green zone.
    /// </summary>
    public class RepairMinigameUI : MonoBehaviour
    {
        private const int BoltCount = 4;
        private const float PointerSpeed = 0.65f;

        public static bool IsOpen => Active != null && Active._open;
        public static RepairMinigameUI Active { get; private set; }

        private Transform _player;
        private PlayerMotor2D _motor;
        private RepairTicket _ticket;
        private Action<int> _onComplete;
        private bool _open;
        private float _pointer;
        private float _greenHalf;
        private int _boltIndex;
        private int _mistakes;
        private float _resultTimer;
        private string _feedback = string.Empty;
        private GUIStyle _titleStyle;
        private GUIStyle _textStyle;
        private GUIStyle _smallStyle;

        public void Configure(Transform player)
        {
            _player = player;
            _motor = player != null ? player.GetComponent<PlayerMotor2D>() : null;
        }

        public void Open(RepairTicket ticket, int mechanicsLevel, Action<int> onComplete)
        {
            if (_open) return;
            Active = this;
            _ticket = ticket;
            _onComplete = onComplete;
            _open = true;
            _pointer = 0f;
            _boltIndex = 0;
            _mistakes = 0;
            _resultTimer = 0f;
            _greenHalf = SkillEffects.MechanicsGreenHalf(mechanicsLevel);
            if (_motor != null) _motor.SetInputEnabled(false);
        }

        private void Update()
        {
            if (!_open) return;

            if (_resultTimer > 0f)
            {
                _resultTimer -= Time.deltaTime;
                if (_resultTimer <= 0f) FinishAndClose();
                return;
            }

            _pointer = Mathf.MoveTowards(_pointer, 1f, PointerSpeed * Time.deltaTime);
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                StrikeBolt();
            }
            else if (_pointer >= 1f)
            {
                _mistakes += 2;
                _feedback = "打滑";
                NextBoltOrFinish();
            }
        }

        private void StrikeBolt()
        {
            float distance = Mathf.Abs(_pointer - 0.5f);
            if (distance <= _greenHalf)
            {
                _feedback = "拧紧";
            }
            else if (distance <= _greenHalf + 0.14f)
            {
                _mistakes++;
                _feedback = "偏了一点";
            }
            else
            {
                _mistakes += 2;
                _feedback = "打滑";
            }
            NextBoltOrFinish();
        }

        private void NextBoltOrFinish()
        {
            _boltIndex++;
            _pointer = 0f;
            if (_boltIndex >= BoltCount)
            {
                _resultTimer = 1.2f;
            }
        }

        private void FinishAndClose()
        {
            int mistakes = _mistakes;
            _open = false;
            Active = null;
            if (_motor != null) _motor.SetInputEnabled(true);
            Action<int> callback = _onComplete;
            _onComplete = null;
            callback?.Invoke(mistakes);
        }

        private void OnGUI()
        {
            if (!_open) return;
            EnsureStyles();

            Rect panel = new Rect(Screen.width * 0.5f - 260f, Screen.height * 0.5f - 90f, 520f, 180f);
            DrawSolid(panel, new Color(0.055f, 0.065f, 0.075f, 0.99f));
            DrawBorder(panel, new Color(0.62f, 0.52f, 0.32f, 1f), 2f);

            GUI.Label(new Rect(panel.x + 18f, panel.y + 10f, 300f, 28f),
                "维修：" + (_ticket != null ? _ticket.PartName : string.Empty), _titleStyle);
            GUI.Label(new Rect(panel.x + 18f, panel.y + 38f, panel.width - 36f, 22f),
                "按空格拧紧螺栓   " + Mathf.Min(_boltIndex + 1, BoltCount) + " / " + BoltCount, _smallStyle);

            Rect bar = new Rect(panel.x + 24f, panel.y + 72f, panel.width - 48f, 42f);
            DrawBar(bar);
            float pointerX = bar.x + bar.width * _pointer;
            DrawSolid(new Rect(pointerX - 2f, bar.y - 6f, 4f, bar.height + 12f), Color.white);

            for (int i = 0; i < BoltCount; i++)
            {
                Rect dot = new Rect(bar.x + 8f + i * 34f, bar.yMax + 16f, 20f, 20f);
                DrawSolid(dot, i < _boltIndex
                    ? new Color(0.28f, 0.66f, 0.34f)
                    : (i == _boltIndex ? new Color(0.86f, 0.70f, 0.24f) : new Color(0.22f, 0.24f, 0.25f)));
            }

            string result = _resultTimer > 0f
                ? "完成，失误 " + _mistakes
                : _feedback;
            GUI.Label(new Rect(panel.x + 18f, panel.y + 138f, panel.width - 36f, 24f), result, _textStyle);
        }

        private void DrawBar(Rect bar)
        {
            float greenStart = 0.5f - _greenHalf;
            float greenEnd = 0.5f + _greenHalf;
            float yellowStart = greenStart - 0.14f;
            float yellowEnd = greenEnd + 0.14f;
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
