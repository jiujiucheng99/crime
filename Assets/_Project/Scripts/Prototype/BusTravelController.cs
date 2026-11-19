using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Minimal two-stop bus travel used to validate the transport-centre
    /// interaction before adding more districts.
    /// </summary>
    public class BusTravelController : MonoBehaviour
    {
        private struct Destination
        {
            public string Id;
            public string Name;
            public Vector2 WorldPosition;

            public Destination(string id, string name, Vector2 worldPosition)
            {
                Id = id;
                Name = name;
                WorldPosition = worldPosition;
            }
        }

        private static readonly Destination[] Destinations =
        {
            new Destination("F7", "砖巷公交中心", new Vector2(44f, -17.8f)),
            new Destination("H8", "南岸港口", new Vector2(82f, -26.2f))
        };

        public static BusTravelController Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance._open;

        private Transform _worldRoot;
        private Transform _player;
        private PlayerMotor2D _motor;
        private Action _leaveInterior;
        private string _currentStop;
        private bool _open;
        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _rowStyle;
        private GUIStyle _currentRowStyle;
        private GUIStyle _closeStyle;
        private GUIStyle _smallStyle;

        public void Configure(Transform worldRoot, Transform player)
        {
            Instance = this;
            _worldRoot = worldRoot;
            _player = player;
            _motor = player != null ? player.GetComponent<PlayerMotor2D>() : null;
        }

        public void Open(string currentStop, Action leaveInterior = null)
        {
            if (_open) return;

            _currentStop = currentStop;
            _leaveInterior = leaveInterior;
            _open = true;
            if (_motor != null) _motor.SetInputEnabled(false);
        }

        private void Close()
        {
            _open = false;
            _leaveInterior = null;
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

        private void TravelTo(Destination destination)
        {
            if (destination.Id == _currentStop)
            {
                Close();
                return;
            }

            if (_leaveInterior != null) _leaveInterior();
            if (_worldRoot != null) _worldRoot.gameObject.SetActive(true);
            if (_player != null)
            {
                Rigidbody2D body = _player.GetComponent<Rigidbody2D>();
                if (body != null) body.velocity = Vector2.zero;
                _player.position = new Vector3(destination.WorldPosition.x, destination.WorldPosition.y, 0f);
            }

            Close();
        }

        /// <summary>Programmatic entry point for testing and future scripted travel.</summary>
        public void TravelToStop(string id)
        {
            foreach (Destination destination in Destinations)
            {
                if (destination.Id == id)
                {
                    TravelTo(destination);
                    return;
                }
            }
        }

        private void OnGUI()
        {
            if (!_open) return;

            EnsureStyles();
            GUI.depth = 0;

            // Dim the game and HUD behind the destination panel.
            DrawSolid(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.015f, 0.02f, 0.025f, 0.72f));

            Rect panel = new Rect(Screen.width * 0.5f - 230f, Screen.height * 0.5f - 168f, 460f, 336f);
            DrawSolid(panel, new Color(0.055f, 0.070f, 0.080f, 0.99f));
            DrawBorder(panel, new Color(0.62f, 0.52f, 0.32f, 1f), 2f);
            DrawBorder(new Rect(panel.x + 4f, panel.y + 4f, panel.width - 8f, panel.height - 8f),
                new Color(0.20f, 0.25f, 0.27f, 1f), 1f);

            Rect header = new Rect(panel.x + 6f, panel.y + 6f, panel.width - 12f, 66f);
            DrawSolid(header, new Color(0.10f, 0.13f, 0.15f, 1f));
            DrawSolid(new Rect(header.x + 18f, header.y + 16f, 4f, 34f), new Color(0.82f, 0.64f, 0.28f, 1f));

            GUI.Label(new Rect(header.x + 34f, header.y + 10f, header.width - 48f, 28f), "公交运输中心", _titleStyle);
            GUI.Label(new Rect(header.x + 34f, header.y + 38f, header.width - 48f, 18f), "TRANSPORT CENTER  ·  F7", _smallStyle);

            GUI.Label(new Rect(panel.x + 26f, panel.y + 86f, panel.width - 52f, 22f), "选择目的地", _subtitleStyle);
            GUI.Label(new Rect(panel.x + panel.width - 146f, panel.y + 88f, 120f, 18f), "Esc 关闭", _smallStyle);

            for (int i = 0; i < Destinations.Length; i++)
            {
                Destination destination = Destinations[i];
                bool current = destination.Id == _currentStop;
                Rect row = new Rect(panel.x + 24f, panel.y + 116f + i * 54f, panel.width - 48f, 44f);

                if (current)
                {
                    DrawSolid(row, new Color(0.12f, 0.15f, 0.16f, 1f));
                    DrawBorder(row, new Color(0.25f, 0.29f, 0.29f, 1f), 1f);
                    GUI.Label(new Rect(row.x + 18f, row.y, row.width - 120f, row.height), destination.Name, _currentRowStyle);
                    GUI.Label(new Rect(row.xMax - 92f, row.y, 72f, row.height), "当前", _smallStyle);
                }
                else
                {
                    bool hover = row.Contains(Event.current.mousePosition);
                    DrawSolid(row, hover
                        ? new Color(0.16f, 0.20f, 0.22f, 1f)
                        : new Color(0.09f, 0.12f, 0.13f, 1f));
                    DrawBorder(row, hover
                        ? new Color(0.72f, 0.60f, 0.36f, 1f)
                        : new Color(0.25f, 0.30f, 0.31f, 1f), 1f);
                    if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                        row.Contains(Event.current.mousePosition))
                    {
                        Event.current.Use();
                        TravelTo(destination);
                        return;
                    }

                    _rowStyle.normal.textColor = hover
                        ? new Color(1f, 0.83f, 0.48f)
                        : new Color(0.92f, 0.90f, 0.82f);
                    GUI.Label(new Rect(row.x + 18f, row.y, row.width - 36f, row.height), destination.Name, _rowStyle);
                }
            }

            Rect closeRect = new Rect(panel.x + 24f, panel.yMax - 58f, panel.width - 48f, 38f);
            bool closeHover = closeRect.Contains(Event.current.mousePosition);
            DrawSolid(closeRect, closeHover
                ? new Color(0.20f, 0.23f, 0.23f, 1f)
                : new Color(0.10f, 0.12f, 0.13f, 1f));
            DrawBorder(closeRect, new Color(0.32f, 0.35f, 0.34f, 1f), 1f);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                closeRect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                Close();
                return;
            }
            _closeStyle.normal.textColor = closeHover
                ? new Color(1f, 0.83f, 0.48f)
                : new Color(0.88f, 0.84f, 0.72f);
            GUI.Label(closeRect, "关闭", _closeStyle);
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

        private void EnsureStyles()
        {
            if (_titleStyle != null) return;

            Font font = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei", "Microsoft YaHei UI", "SimHei", "Noto Sans CJK SC", "Arial" }, 18);

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.96f, 0.91f, 0.78f) }
            };
            _subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 15,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.62f, 0.68f, 0.66f) }
            };
            _rowStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(0, 0, 0, 0),
                normal = { textColor = new Color(0.92f, 0.90f, 0.82f) }
            };
            _currentRowStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.54f, 0.58f, 0.56f) }
            };
            _closeStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.88f, 0.84f, 0.72f) }
            };
            _smallStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 11,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.55f, 0.60f, 0.58f) }
            };
        }
    }
}
