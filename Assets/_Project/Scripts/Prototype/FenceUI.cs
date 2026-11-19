using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Fence trade panel: lists stolen goods and sells one unit at a time.
    /// </summary>
    public class FenceUI : MonoBehaviour
    {
        private sealed class Entry
        {
            public string Id;
            public string Name;
            public int Count;
            public int Value;
        }

        public static FenceUI Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance._open;

        private PlayerInventory _pockets;
        private PlayerWallet _wallet;
        private PlayerMotor2D _motor;
        private bool _open;
        private string _feedback;
        private GUIStyle _titleStyle;
        private GUIStyle _textStyle;
        private GUIStyle _smallStyle;
        private GUIStyle _buttonStyle;

        public void Configure(Transform player)
        {
            Instance = this;
            _pockets = player != null ? player.GetComponent<PlayerInventory>() : null;
            _wallet = player != null ? player.GetComponent<PlayerWallet>() : null;
            _motor = player != null ? player.GetComponent<PlayerMotor2D>() : null;
        }

        public void Open()
        {
            if (_open) return;
            _open = true;
            _feedback = null;
            if (_motor != null) _motor.SetInputEnabled(false);
        }

        private void Close()
        {
            _open = false;
            if (_motor != null) _motor.SetInputEnabled(true);
        }

        private void Update()
        {
            if (!_open) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }

        private void OnGUI()
        {
            if (!_open) return;
            EnsureStyles();

            List<Entry> entries = BuildEntries();
            float panelHeight = Mathf.Min(Screen.height - 24f, 150f + entries.Count * 44f);
            Rect panel = new Rect(Screen.width * 0.5f - 260f, (Screen.height - panelHeight) * 0.5f, 520f, panelHeight);
            DrawSolid(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.01f, 0.015f, 0.02f, 0.75f));
            DrawSolid(panel, new Color(0.055f, 0.065f, 0.075f, 0.99f));
            DrawBorder(panel, new Color(0.62f, 0.52f, 0.32f, 1f), 2f);

            GUI.Label(new Rect(panel.x + 20f, panel.y + 12f, 220f, 30f), "销赃", _titleStyle);
            GUI.Label(new Rect(panel.x + 160f, panel.y + 18f, panel.width - 180f, 24f),
                "现金 " + (_wallet != null ? _wallet.Money : 0) + " 块", _smallStyle);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 46f, panel.width - 40f, 22f),
                "选择要卖的赃物，每次卖出 1 件", _smallStyle);

            if (entries.Count == 0)
            {
                GUI.Label(new Rect(panel.x + 20f, panel.y + 80f, panel.width - 40f, 30f),
                    "背包里没有可卖的赃物。", _textStyle);
            }
            else
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    Entry entry = entries[i];
                    Rect row = new Rect(panel.x + 18f, panel.y + 76f + i * 44f, panel.width - 36f, 38f);
                    DrawSolid(row, new Color(0.08f, 0.10f, 0.12f, 1f));
                    DrawBorder(row, new Color(0.22f, 0.26f, 0.28f, 1f), 1f);

                    Sprite icon = Resources.Load<Sprite>("Items/" + entry.Id);
                    if (icon != null) GUI.DrawTexture(new Rect(row.x + 6f, row.y + 4f, 30f, 30f),
                        icon.texture, ScaleMode.ScaleToFit, true);

                    GUI.Label(new Rect(row.x + 44f, row.y, 180f, row.height),
                        entry.Name + "  x" + entry.Count, _textStyle);
                    GUI.Label(new Rect(row.x + 222f, row.y, 100f, row.height),
                        entry.Value + " 块/件", _smallStyle);

                    Rect button = new Rect(row.xMax - 86f, row.y + 5f, 78f, 28f);
                    if (ClickButton(button, "卖出 1", true))
                    {
                        SellOne(entry);
                    }
                }
            }

            if (!string.IsNullOrEmpty(_feedback))
            {
                GUI.Label(new Rect(panel.x + 20f, panel.yMax - 78f, panel.width - 40f, 20f),
                    _feedback, _smallStyle);
            }

            Rect close = new Rect(panel.x + 20f, panel.yMax - 46f, panel.width - 40f, 32f);
            if (ClickButton(close, "结束交易", false)) Close();
        }

        private List<Entry> BuildEntries()
        {
            List<Entry> entries = new List<Entry>();
            if (_pockets == null) return entries;

            for (int i = 0; i < PlayerInventory.TotalSize; i++)
            {
                ItemStack stack = _pockets.GetSlot(i);
                if (stack == null || !StolenGoods.IsStolen(stack.Id)) continue;

                Entry existing = entries.Find(e => e.Id == stack.Id);
                if (existing == null)
                {
                    entries.Add(new Entry
                    {
                        Id = stack.Id,
                        Name = stack.DisplayName,
                        Count = stack.Count,
                        Value = StolenGoods.ValueOf(stack.Id)
                    });
                }
                else
                {
                    existing.Count += stack.Count;
                }
            }
            return entries;
        }

        private void SellOne(Entry entry)
        {
            int value = StolenGoods.ValueOf(entry.Id);
            if (_pockets == null || !_pockets.TryRemove(entry.Id, 1) || _wallet == null)
            {
                _feedback = "卖出失败。";
                return;
            }

            _wallet.Add(value);
            _feedback = "卖出 " + entry.Name + "，获得 " + value + " 块。";
        }

        private bool ClickButton(Rect rect, string label, bool small)
        {
            bool hover = rect.Contains(Event.current.mousePosition);
            DrawSolid(rect, hover
                ? new Color(0.18f, 0.22f, 0.24f, 1f)
                : new Color(0.09f, 0.12f, 0.13f, 1f));
            DrawBorder(rect, hover
                ? new Color(0.72f, 0.60f, 0.36f, 1f)
                : new Color(0.25f, 0.30f, 0.31f, 1f), 1f);
            _buttonStyle.normal.textColor = hover
                ? new Color(1f, 0.83f, 0.48f)
                : new Color(0.92f, 0.90f, 0.82f);
            GUI.Label(rect, label, _buttonStyle);

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                rect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                return true;
            }
            return false;
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null) return;
            Font font = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei", "Microsoft YaHei UI", "SimHei", "Noto Sans CJK SC", "Arial" }, 18);
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                font = font, fontSize = 21, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.96f, 0.91f, 0.78f) }
            };
            _textStyle = new GUIStyle(GUI.skin.label)
            {
                font = font, fontSize = 15, alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.92f, 0.90f, 0.82f) }
            };
            _smallStyle = new GUIStyle(GUI.skin.label)
            {
                font = font, fontSize = 13, alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.68f, 0.72f, 0.70f) }
            };
            _buttonStyle = new GUIStyle(GUI.skin.label)
            {
                font = font, fontSize = 14, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.92f, 0.90f, 0.82f) }
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
