using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Basic convenience-store shop. Items exist and can be bought for cash, but
    /// their gameplay effects are not implemented yet.
    /// </summary>
    public class StoreShopUI : MonoBehaviour
    {
        private struct ShopItem
        {
            public string Id;
            public string Name;
            public int Price;

            public ShopItem(string id, string name, int price)
            {
                Id = id;
                Name = name;
                Price = price;
            }
        }

        private static readonly ShopItem[] Items =
        {
            new ShopItem("first_aid", "急救包", 5),
            new ShopItem("flashlight", "手电筒", 2),
            new ShopItem("wrench", "扳手", 3),
            new ShopItem("gloves", "手套", 1),
            new ShopItem("paperclip", "回形针", 1)
        };

        public static StoreShopUI Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance._open;

        private Transform _player;
        private PlayerMotor2D _motor;
        private PlayerWallet _wallet;
        private PlayerInventory _pockets;
        private NpcDialogueComponent _clerkDialogue;
        private bool _open;
        private int _selectedIndex = -1;
        private int _quantity = 1;
        private string _message;
        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _itemStyle;
        private GUIStyle _priceStyle;
        private GUIStyle _moneyStyle;
        private GUIStyle _messageStyle;
        private GUIStyle _smallStyle;
        private GUIStyle _closeStyle;
        private GUIStyle _actionStyle;

        public void Configure(Transform player, PlayerWallet wallet, NpcDialogueComponent clerkDialogue = null)
        {
            Instance = this;
            _player = player;
            _motor = player != null ? player.GetComponent<PlayerMotor2D>() : null;
            _wallet = wallet;
            _pockets = player != null ? player.GetComponent<PlayerInventory>() : null;
            _clerkDialogue = clerkDialogue;
        }

        public void Open()
        {
            if (_open) return;
            _open = true;
            _selectedIndex = -1;
            _quantity = 1;
            _message = string.Empty;
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
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        private void Buy(ShopItem item)
        {
            BeginPurchase(item);
        }

        private void BeginPurchase(ShopItem item)
        {
            for (int i = 0; i < Items.Length; i++)
            {
                if (Items[i].Id == item.Id)
                {
                    _selectedIndex = i;
                    _quantity = 1;
                    _message = string.Empty;
                    return;
                }
            }
        }

        private void ConfirmPurchase()
        {
            if (_selectedIndex < 0 || _selectedIndex >= Items.Length || _wallet == null || _pockets == null) return;

            ShopItem item = Items[_selectedIndex];
            int total = item.Price * _quantity;
            if (_wallet.Money < total)
            {
                _message = "现金不足";
                return;
            }

            if (_pockets.Count >= _pockets.Capacity)
            {
                _message = "背包已满";
                return;
            }

            if (!_pockets.TryAdd(new ItemStack(item.Id, item.Name, _quantity)))
            {
                _message = "背包已满";
                return;
            }

            _wallet.TrySpend(total);
            _message = "已购买 " + item.Name + " x" + _quantity;
            _selectedIndex = -1;
            _quantity = 1;

            if (_clerkDialogue != null)
            {
                Close();
                _clerkDialogue.Say("谢谢惠顾，欢迎再来。");
            }
        }

        private void OnGUI()
        {
            if (!_open) return;

            EnsureStyles();
            DrawSolid(new Rect(0f, 0f, Screen.width, Screen.height),
                new Color(0.015f, 0.02f, 0.025f, 0.72f));

            Rect panel = new Rect(Screen.width * 0.5f - 300f,
                (Screen.height - Mathf.Min(420f, Screen.height - 24f)) * 0.5f,
                600f, Mathf.Min(420f, Screen.height - 24f));
            DrawSolid(panel, new Color(0.055f, 0.070f, 0.080f, 0.99f));
            DrawBorder(panel, new Color(0.62f, 0.52f, 0.32f, 1f), 2f);
            DrawBorder(new Rect(panel.x + 4f, panel.y + 4f, panel.width - 8f, panel.height - 8f),
                new Color(0.20f, 0.25f, 0.27f, 1f), 1f);

            Rect header = new Rect(panel.x + 6f, panel.y + 6f, panel.width - 12f, 54f);
            DrawSolid(header, new Color(0.10f, 0.13f, 0.15f, 1f));
            DrawSolid(new Rect(header.x + 18f, header.y + 12f, 4f, 30f),
                new Color(0.82f, 0.64f, 0.28f, 1f));
            GUI.Label(new Rect(header.x + 34f, header.y + 6f, header.width - 48f, 26f), "便利店", _titleStyle);
            GUI.Label(new Rect(header.x + 34f, header.y + 30f, header.width - 48f, 18f),
                "CONVENIENCE STORE  ·  D8", _smallStyle);

            GUI.Label(new Rect(panel.x + 28f, panel.y + 66f, 240f, 22f), "商品", _subtitleStyle);
            GUI.Label(new Rect(panel.x + panel.width - 268f, panel.y + 66f, 240f, 22f),
                "现金  " + (_wallet != null ? _wallet.Money : 0) + " 块", _moneyStyle);

            if (_selectedIndex >= 0)
            {
                DrawPurchaseConfirm(panel);
                return;
            }

            for (int i = 0; i < Items.Length; i++)
            {
                ShopItem item = Items[i];
                Rect row = new Rect(panel.x + 24f, panel.y + 88f + i * 31f, panel.width - 48f, 28f);
                bool hover = row.Contains(Event.current.mousePosition);

                DrawSolid(row, hover
                    ? new Color(0.16f, 0.20f, 0.22f, 1f)
                    : new Color(0.09f, 0.12f, 0.13f, 1f));
                DrawBorder(row, hover
                    ? new Color(0.72f, 0.60f, 0.36f, 1f)
                    : new Color(0.24f, 0.29f, 0.30f, 1f), 1f);

                Sprite icon = Resources.Load<Sprite>("Items/" + item.Id);
                if (icon != null)
                {
                    GUI.DrawTexture(new Rect(row.x + 8f, row.y + 3f, 22f, 22f),
                        icon.texture, ScaleMode.ScaleToFit, true);
                }

                _itemStyle.normal.textColor = hover
                    ? new Color(1f, 0.83f, 0.48f)
                    : new Color(0.92f, 0.90f, 0.82f);
                GUI.Label(new Rect(row.x + 38f, row.y, row.width - 130f, row.height), item.Name, _itemStyle);

                bool affordable = _wallet != null && _wallet.Money >= item.Price;
                _priceStyle.normal.textColor = affordable
                    ? new Color(0.88f, 0.78f, 0.48f)
                    : new Color(0.55f, 0.40f, 0.38f);
                GUI.Label(new Rect(row.xMax - 108f, row.y, 88f, row.height), item.Price + " 块", _priceStyle);

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                    row.Contains(Event.current.mousePosition))
                {
                    Event.current.Use();
                    Buy(item);
                    return;
                }
            }

            GUI.Label(new Rect(panel.x + 28f, panel.yMax - 82f, panel.width - 56f, 22f),
                string.IsNullOrEmpty(_message) ? "选择商品购买" : _message, _messageStyle);

            Rect closeRect = new Rect(panel.x + 24f, panel.yMax - 44f, panel.width - 48f, 32f);
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

        private void DrawPurchaseConfirm(Rect panel)
        {
            ShopItem item = Items[_selectedIndex];
            int maxAffordable = _wallet != null && item.Price > 0 ? _wallet.Money / item.Price : 0;
            if (_quantity > Mathf.Max(1, maxAffordable)) _quantity = Mathf.Max(1, maxAffordable);
            bool inventoryFull = _pockets != null && _pockets.Count >= _pockets.Capacity;
            bool canConfirm = maxAffordable > 0 && !inventoryFull;

            Rect itemRow = new Rect(panel.x + 24f, panel.y + 96f, panel.width - 48f, 54f);
            DrawSolid(itemRow, new Color(0.09f, 0.12f, 0.13f, 1f));
            DrawBorder(itemRow, new Color(0.28f, 0.33f, 0.33f, 1f), 1f);
            Sprite icon = Resources.Load<Sprite>("Items/" + item.Id);
            if (icon != null)
            {
                GUI.DrawTexture(new Rect(itemRow.x + 10f, itemRow.y + 10f, 34f, 34f),
                    icon.texture, ScaleMode.ScaleToFit, true);
            }
            GUI.Label(new Rect(itemRow.x + 54f, itemRow.y + 6f, itemRow.width - 70f, 24f), item.Name, _itemStyle);
            GUI.Label(new Rect(itemRow.x + 54f, itemRow.y + 28f, itemRow.width - 70f, 18f),
                "单价  " + item.Price + " 块", _smallStyle);

            GUI.Label(new Rect(panel.x + 28f, panel.y + 158f, 80f, 30f), "数量", _itemStyle);

            Rect minusRect = new Rect(panel.x + 108f, panel.y + 158f, 42f, 32f);
            Rect qtyRect = new Rect(panel.x + 154f, panel.y + 158f, 58f, 32f);
            Rect plusRect = new Rect(panel.x + 216f, panel.y + 158f, 42f, 32f);
            Rect maxRect = new Rect(panel.x + 264f, panel.y + 158f, 62f, 32f);

            if (ClickButton(minusRect, "-", _actionStyle, _quantity > 1)) _quantity--;
            DrawSolid(qtyRect, new Color(0.08f, 0.10f, 0.11f, 1f));
            DrawBorder(qtyRect, new Color(0.28f, 0.33f, 0.33f, 1f), 1f);
            GUI.Label(qtyRect, _quantity.ToString(), _actionStyle);
            if (ClickButton(plusRect, "+", _actionStyle, _quantity < maxAffordable)) _quantity++;
            if (ClickButton(maxRect, "最大", _actionStyle, maxAffordable > 0)) _quantity = Mathf.Max(1, maxAffordable);

            int total = item.Price * _quantity;
            _moneyStyle.normal.textColor = _wallet != null && _wallet.Money >= total
                ? new Color(0.88f, 0.78f, 0.48f)
                : new Color(0.72f, 0.42f, 0.38f);
            GUI.Label(new Rect(panel.x + 28f, panel.y + 202f, panel.width - 56f, 24f),
                "合计  " + total + " 块", _moneyStyle);

            string hint = inventoryFull ? "背包已满" : (maxAffordable <= 0 ? "现金不足" : "确认后扣除现金");
            GUI.Label(new Rect(panel.x + 28f, panel.y + 228f, panel.width - 56f, 20f), hint, _messageStyle);

            Rect confirmRect = new Rect(panel.x + 24f, panel.yMax - 82f, panel.width - 48f, 32f);
            Rect cancelRect = new Rect(panel.x + 24f, panel.yMax - 44f, panel.width - 48f, 30f);
            if (ClickButton(confirmRect, "确认购买", _actionStyle, canConfirm))
            {
                ConfirmPurchase();
                return;
            }
            if (ClickButton(cancelRect, "取消", _actionStyle, true))
            {
                _selectedIndex = -1;
                _quantity = 1;
                _message = string.Empty;
            }
        }

        private bool ClickButton(Rect rect, string label, GUIStyle style, bool enabled)
        {
            bool hover = rect.Contains(Event.current.mousePosition);
            DrawSolid(rect, !enabled
                ? new Color(0.07f, 0.08f, 0.09f, 1f)
                : hover
                    ? new Color(0.20f, 0.23f, 0.23f, 1f)
                    : new Color(0.10f, 0.12f, 0.13f, 1f));
            DrawBorder(rect, enabled && hover
                ? new Color(0.72f, 0.60f, 0.36f, 1f)
                : new Color(0.30f, 0.34f, 0.33f, 1f), 1f);
            style.normal.textColor = enabled
                ? (hover ? new Color(1f, 0.83f, 0.48f) : new Color(0.88f, 0.84f, 0.72f))
                : new Color(0.40f, 0.42f, 0.40f);
            GUI.Label(rect, label, style);

            return enabled && Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                rect.Contains(Event.current.mousePosition);
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
            _itemStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 17,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.92f, 0.90f, 0.82f) }
            };
            _priceStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 15,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.88f, 0.78f, 0.48f) }
            };
            _moneyStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 15,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.88f, 0.78f, 0.48f) }
            };
            _messageStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.78f, 0.80f, 0.74f) }
            };
            _smallStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 11,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.55f, 0.60f, 0.58f) }
            };
            _closeStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.88f, 0.84f, 0.72f) }
            };
            _actionStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.88f, 0.84f, 0.72f) }
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
