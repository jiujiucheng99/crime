using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Minecraft-style hotbar, E inventory, and container page.
    /// Click a slot to pick up/place a stack; number keys select hotbar slots.
    /// </summary>
    public sealed class StorageInteraction : MonoBehaviour
    {
        private const float InteractRange = 1.7f;
        private const float AutoCloseRange = 2.5f;
        private const int SlotSize = 32;
        private const int SlotGap = 3;
        private const int GridColumns = 9;
        private const float HotbarBottomMargin = 28f;

        private static readonly Color PanelColor = new Color(0.10f, 0.10f, 0.13f, 0.98f);
        private static readonly Color PanelBorder = new Color(0.55f, 0.55f, 0.62f, 1f);
        private static readonly Color SlotColor = new Color(0.20f, 0.20f, 0.24f, 1f);
        private static readonly Color SlotBorder = new Color(0.45f, 0.45f, 0.52f, 1f);
        private static readonly Color SelectColor = new Color(1f, 0.78f, 0.15f, 1f);

        private PlayerInventory _pockets;
        private PlayerSkills _skills;
        private PlayerMotor2D _motor;
        private StorageContainer _open;
        private StorageContainer _nearest;
        private ItemStack _held;
        private bool _inventoryOpen;
        private string _prompt;
        private GUIStyle _skillHeaderStyle;
        private GUIStyle _skillNameStyle;
        private GUIStyle _skillLevelStyle;
        private GUIStyle _skillDescStyle;
        private GUIStyle _skillDetailStyle;
        private int _selectedSkillIndex = -1;
        private Vector2 _skillScroll;

        public static bool AnyPanelOpen { get; private set; }
        public bool IsOpen => _inventoryOpen || _open != null;

        public void Configure(PlayerInventory pockets)
        {
            _pockets = pockets;
            _motor = GetComponent<PlayerMotor2D>();
            _skills = GetComponent<PlayerSkills>();
            AnyPanelOpen = false;
        }

        private void Update()
        {
            if (_pockets == null || Keyboard.current == null) return;

            if (IsOpen)
            {
                if (_open != null && Vector2.Distance(transform.position, _open.transform.position) > AutoCloseRange)
                {
                    ClosePanels();
                    return;
                }

                if (Keyboard.current.escapeKey.wasPressedThisFrame ||
                    Keyboard.current.eKey.wasPressedThisFrame ||
                    (_open != null && Keyboard.current.fKey.wasPressedThisFrame))
                {
                    ClosePanels();
                }
                return;
            }

            for (int i = 0; i < PlayerInventory.HotbarSize; i++)
            {
                Key key = (Key)((int)Key.Digit1 + i);
                if (Keyboard.current[key].wasPressedThisFrame)
                {
                    _pockets.SelectHotbar(i);
                }
            }

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                OpenInventory();
                return;
            }

            _nearest = FindNearest();
            _prompt = _nearest != null ? PromptName(_nearest.DisplayName) : string.Empty;
            InteractionPromptUI.SetPrompt("storage", IsOpen ? string.Empty : _prompt);

            if (InteractionInput.TryConsumeF(_nearest != null))
            {
                OpenContainer(_nearest);
            }
        }

        public void OpenNearest()
        {
            StorageContainer found = FindNearest();
            if (found == null) return;
            OpenContainer(found);
        }

        private void OpenContainer(StorageContainer container)
        {
            AutoRestockContainer restock = container.GetComponent<AutoRestockContainer>();
            if (restock != null) restock.EnsureStock();
            _open = container;
            _inventoryOpen = false;
            SetPanelState(true);
        }

        public void OpenInventory()
        {
            _open = null;
            _inventoryOpen = true;
            SetPanelState(true);
        }

        public void ClosePanels()
        {
            if (_held != null)
            {
                if (!_pockets.TryAdd(_held)) return; // Keep the panel open if there is no room.
                _held = null;
            }

            _inventoryOpen = false;
            _open = null;
            _prompt = string.Empty;
            InteractionPromptUI.SetPrompt("storage", string.Empty);
            SetPanelState(false);
        }

        private void SetPanelState(bool open)
        {
            AnyPanelOpen = open;
            InteractionPromptUI.SetPrompt("storage", string.Empty);
            if (_motor != null) _motor.SetInputEnabled(!open);
        }

        private static string PromptName(string displayName)
        {
            switch (displayName)
            {
                case "TABLE": return "桌子";
                case "MIRROR": return "镜子";
                case "SHOWER": return "淋浴";
                case "SAFE": return "保险箱";
                case "FRIDGE": return "冰箱";
                case "PARTS": return "零件架";
                default: return displayName;
            }
        }

        private StorageContainer FindNearest()
        {
            StorageContainer best = null;
            float bestDistance = InteractRange;

            foreach (StorageContainer container in FindObjectsOfType<StorageContainer>())
            {
                SafeLockController safeLock = container.GetComponent<SafeLockController>();
                if (safeLock != null && safeLock.IsLocked) continue;
                ContainerSearchController search = container.GetComponent<ContainerSearchController>();
                if (search != null && !search.IsSearched) continue;

                float distance = Vector2.Distance(transform.position, container.transform.position);
                if (distance < bestDistance)
                {
                    best = container;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private void OnGUI()
        {
            if (BusTravelController.IsOpen || DialogueChoiceController.IsOpen || StoreShopUI.IsOpen)
            {
                InteractionPromptUI.SetPrompt("storage", string.Empty);
                return;
            }

            InteractionPromptUI.SetPrompt("storage", IsOpen ? string.Empty : _prompt);

            DrawHotbar();
            DrawHandLabel();

            if (IsOpen)
            {
                if (_inventoryOpen) DrawInventoryPanel();
                else DrawContainerPanel();
                DrawHeldItem();
                return;
            }
        }

        private void OnDisable()
        {
            InteractionPromptUI.SetPrompt("storage", string.Empty);
        }

        private void DrawHandLabel()
        {
            ItemStack selected = _pockets.SelectedItem;
            if (selected == null) return;

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 11;
            style.normal.textColor = Color.white;
            float y = IsOpen ? 8f : Screen.height - SlotSize - HotbarBottomMargin - 22f;
            GUI.Label(new Rect(Screen.width * 0.5f - 140f, y, 280f, 18f),
                "HAND: " + selected.DisplayName + " x" + selected.Count, style);
        }

        private void DrawHotbar()
        {
            float width = GridColumns * SlotSize + (GridColumns - 1) * SlotGap;
            float startX = (Screen.width - width) * 0.5f;
            float y = Screen.height - SlotSize - HotbarBottomMargin;

            DrawPanelRect(new Rect(startX - 6f, y - 6f, width + 12f, SlotSize + 12f));

            for (int i = 0; i < PlayerInventory.HotbarSize; i++)
            {
                Rect rect = new Rect(startX + i * (SlotSize + SlotGap), y, SlotSize, SlotSize);
                DrawSlot(rect, _pockets.GetSlot(i), i == _pockets.SelectedHotbar, false, i);
            }
        }

        private void DrawInventoryPanel()
        {
            EnsureSkillStyles();
            float panelWidth = GridColumns * SlotSize + (GridColumns - 1) * SlotGap + 24f;
            const float skillViewportHeight = 100f;
            const float skillDetailHeight = 46f;
            float panelHeight = 3 * SlotSize + 2 * SlotGap + 48f + 4f +
                                skillViewportHeight + skillDetailHeight + 34f;
            Rect panel = new Rect((Screen.width - panelWidth) * 0.5f,
                Screen.height - SlotSize - HotbarBottomMargin - panelHeight - 8f,
                panelWidth, panelHeight);
            DrawPanelRect(panel);
            GUI.Label(new Rect(panel.x + 12f, panel.y + 8f, 220f, 22f), "INVENTORY");

            GUI.Label(new Rect(panel.x + 12f, panel.y + 30f, 120f, 18f), "技能", _skillHeaderStyle);

            Rect viewport = new Rect(panel.x + 10f, panel.y + 48f, panel.width - 20f, skillViewportHeight);
            FillRect(viewport, new Color(0.07f, 0.08f, 0.10f, 1f));
            DrawBorder(viewport, new Color(0.25f, 0.27f, 0.31f, 1f), 1f);

            float contentHeight = 8f;
            if (_skills != null) contentHeight = Mathf.Max(skillViewportHeight - 4f, _skills.Skills.Count * 34f + 8f);
            Rect content = new Rect(0f, 0f, viewport.width - 18f, contentHeight);
            _skillScroll = GUI.BeginScrollView(viewport, _skillScroll, content, false, true);

            if (_skills != null)
            {
                if (_selectedSkillIndex < 0 && _skills.Skills.Count > 0) _selectedSkillIndex = 0;
                for (int i = 0; i < _skills.Skills.Count; i++)
                {
                    PlayerSkills.Skill skill = _skills.Skills[i];
                    Rect row = new Rect(2f, 2f + i * 34f, content.width - 4f, 32f);
                    if (row.Contains(Event.current.mousePosition)) _selectedSkillIndex = i;
                    bool selected = i == _selectedSkillIndex;
                    FillRect(row, selected
                        ? new Color(0.20f, 0.19f, 0.13f, 1f)
                        : (i % 2 == 0
                            ? new Color(0.11f, 0.12f, 0.15f, 1f)
                            : new Color(0.09f, 0.10f, 0.13f, 1f)));
                    if (selected) DrawBorder(row, new Color(0.72f, 0.58f, 0.26f, 1f), 1f);
                    GUI.Label(new Rect(row.x + 7f, row.y + 2f, row.width - 68f, 20f), skill.DisplayName, _skillNameStyle);
                    GUI.Label(new Rect(row.xMax - 58f, row.y + 2f, 52f, 20f), "LV" + skill.Level, _skillLevelStyle);
                    Rect xpTrack = new Rect(row.x + 7f, row.yMax - 4f, row.width - 75f, 2f);
                    FillRect(xpTrack, new Color(0.20f, 0.22f, 0.26f, 1f));
                    int need = skill.ExperienceForNextLevel;
                    float ratio = need > 0 ? Mathf.Clamp01((float)skill.Experience / need) : 1f;
                    FillRect(new Rect(xpTrack.x, xpTrack.y, xpTrack.width * ratio, xpTrack.height),
                        new Color(0.72f, 0.56f, 0.22f, 1f));
                }
            }

            GUI.EndScrollView();

            Rect detail = new Rect(panel.x + 10f, panel.y + 48f + skillViewportHeight + 5f,
                panel.width - 20f, skillDetailHeight - 7f);
            FillRect(detail, new Color(0.07f, 0.08f, 0.10f, 1f));
            DrawBorder(detail, new Color(0.25f, 0.27f, 0.31f, 1f), 1f);
            if (_skills != null && _selectedSkillIndex >= 0 &&
                _selectedSkillIndex < _skills.Skills.Count)
            {
                PlayerSkills.Skill selectedSkill = _skills.Skills[_selectedSkillIndex];
                SkillDefinition definition = SkillCatalog.Get(selectedSkill.Id);
                string effect = definition != null ? definition.EffectDescription : string.Empty;
                GUI.Label(new Rect(detail.x + 9f, detail.y + 4f, detail.width - 18f, detail.height - 8f),
                    selectedSkill.DisplayName + "：" + effect, _skillDetailStyle);
            }

            float gridTop = panel.y + 48f + skillViewportHeight + skillDetailHeight + 10f;
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < GridColumns; col++)
                {
                    int index = PlayerInventory.HotbarSize + row * GridColumns + col;
                    Rect rect = new Rect(panel.x + 12f + col * (SlotSize + SlotGap),
                        gridTop + row * (SlotSize + SlotGap), SlotSize, SlotSize);
                    DrawSlot(rect, _pockets.GetSlot(index), false, false, index);
                }
            }
        }

        private void EnsureSkillStyles()
        {
            if (_skillHeaderStyle != null) return;
            Font font = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei", "Microsoft YaHei UI", "SimHei", "Noto Sans CJK SC", "Arial" }, 15);
            _skillHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 13,
                normal = { textColor = new Color(0.62f, 0.68f, 0.66f) }
            };
            _skillNameStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 13,
                normal = { textColor = new Color(0.92f, 0.90f, 0.82f) }
            };
            _skillLevelStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 13,
                normal = { textColor = new Color(0.82f, 0.68f, 0.38f) }
            };
            _skillDescStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 10,
                normal = { textColor = new Color(0.64f, 0.68f, 0.66f) }
            };
            _skillDetailStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 11,
                wordWrap = true,
                normal = { textColor = new Color(0.78f, 0.80f, 0.74f) }
            };
        }

        private void DrawContainerPanel()
        {
            if (_open == null) return;

            int columns = Mathf.Min(GridColumns, Mathf.Max(1, _open.Capacity));
            int rows = Mathf.CeilToInt(_open.Capacity / (float)columns);
            float panelWidth = GridColumns * SlotSize + (GridColumns - 1) * SlotGap + 24f;
            float panelHeight = (rows + 3) * SlotSize + (rows + 2) * SlotGap + 64f;
            Rect panel = new Rect((Screen.width - panelWidth) * 0.5f,
                Screen.height - SlotSize - HotbarBottomMargin - panelHeight - 8f,
                panelWidth, panelHeight);
            DrawPanelRect(panel);
            GUI.Label(new Rect(panel.x + 12f, panel.y + 10f, 320f, 22f), _open.DisplayName + "  " + _open.Count + "/" + _open.Capacity);

            for (int i = 0; i < _open.Capacity; i++)
            {
                int col = i % columns;
                int row = i / columns;
                Rect rect = new Rect(panel.x + 12f + col * (SlotSize + SlotGap),
                    panel.y + 36f + row * (SlotSize + SlotGap), SlotSize, SlotSize);
                DrawSlot(rect, _open.GetSlot(i), false, true, i);
            }

            float inventoryTop = panel.y + 36f + rows * (SlotSize + SlotGap) + 12f;
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < GridColumns; col++)
                {
                    int index = PlayerInventory.HotbarSize + row * GridColumns + col;
                    Rect rect = new Rect(panel.x + 12f + col * (SlotSize + SlotGap),
                        inventoryTop + row * (SlotSize + SlotGap), SlotSize, SlotSize);
                    DrawSlot(rect, _pockets.GetSlot(index), false, false, index);
                }
            }
        }

        private void DrawSlot(Rect rect, ItemStack stack, bool selected, bool containerSlot, int index)
        {
            FillRect(rect, SlotColor);
            DrawBorder(rect, SlotBorder, 1f);

            if (stack != null)
            {
                Sprite icon = ItemIcon(stack.Id);
                if (icon != null)
                {
                    Color oldIconColor = GUI.color;
                    GUI.color = Color.white;
                    GUI.DrawTexture(new Rect(rect.x + 5f, rect.y + 7f, 22f, 22f),
                        icon.texture, ScaleMode.ScaleToFit, true);
                    GUI.color = oldIconColor;
                }
                else
                {
                    Color old = GUI.color;
                    GUI.color = ItemColor(stack.Id);
                    GUI.DrawTexture(new Rect(rect.x + 6f, rect.y + 7f, 20f, 20f), Texture2D.whiteTexture);
                    GUI.color = old;
                }

                GUIStyle label = new GUIStyle(GUI.skin.label);
                label.fontSize = 9;
                label.alignment = TextAnchor.UpperCenter;
                label.normal.textColor = Color.white;
                GUI.Label(new Rect(rect.x + 1f, rect.y + 3f, rect.width - 2f, 12f),
                    ShortName(stack.DisplayName), label);

                GUIStyle count = new GUIStyle(GUI.skin.label);
                count.fontSize = 10;
                count.alignment = TextAnchor.LowerRight;
                count.normal.textColor = Color.white;
                GUI.Label(new Rect(rect.x + 2f, rect.y + 14f, rect.width - 4f, 14f), "x" + stack.Count, count);
            }

            if (selected)
            {
                DrawBorder(rect, SelectColor, 2f);
            }

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                OnSlotClicked(containerSlot, index);
            }

            if (stack != null && rect.Contains(Event.current.mousePosition))
            {
                DrawItemTooltip(rect, stack.DisplayName);
            }
        }

        private static void DrawItemTooltip(Rect slot, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 11;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = new Color(0.96f, 0.93f, 0.82f);
            float width = Mathf.Max(80f, style.CalcSize(new GUIContent(text)).x + 16f);
            float y = slot.y - 24f;
            if (y < 0f) y = slot.yMax + 4f;
            Rect box = new Rect(slot.x + slot.width * 0.5f - width * 0.5f, y, width, 20f);
            FillRect(box, new Color(0.04f, 0.05f, 0.06f, 0.96f));
            DrawBorder(box, new Color(0.62f, 0.52f, 0.32f, 1f), 1f);
            GUI.Label(box, text, style);
        }

        private void OnSlotClicked(bool containerSlot, int index)
        {
            if (!IsOpen) return;

            ItemStack current = containerSlot && _open != null ? _open.GetSlot(index) : _pockets.GetSlot(index);

            if (_held == null)
            {
                if (current == null) return;
                _held = current;
                if (containerSlot && _open != null) _open.SetSlot(index, null);
                else _pockets.SetSlot(index, null);
                return;
            }

            if (containerSlot && _open != null) _open.SetSlot(index, _held);
            else _pockets.SetSlot(index, _held);
            _held = current;
        }

        private void DrawHeldItem()
        {
            if (_held == null) return;
            Vector2 mouse = Event.current.mousePosition;
            // Keep the cursor at the centre of the dragged stack, not its top-left corner.
            Rect rect = new Rect(mouse.x - 14f, mouse.y - 14f, 28f, 28f);
            Sprite icon = ItemIcon(_held.Id);
            if (icon != null)
            {
                Color oldIconColor = GUI.color;
                GUI.color = Color.white;
                GUI.DrawTexture(rect, icon.texture, ScaleMode.ScaleToFit, true);
                GUI.color = oldIconColor;
            }
            else
            {
                Color old = GUI.color;
                GUI.color = ItemColor(_held.Id);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = old;
            }
        }

        private static readonly Dictionary<string, Sprite> ItemIcons = new Dictionary<string, Sprite>();

        private static Sprite ItemIcon(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            Sprite sprite;
            if (ItemIcons.TryGetValue(id, out sprite)) return sprite;

            sprite = Resources.Load<Sprite>("Items/" + id);
            ItemIcons[id] = sprite;
            return sprite;
        }

        private static void FillRect(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        private static void DrawBorder(Rect rect, Color color, float thickness)
        {
            FillRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            FillRect(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), color);
            FillRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            FillRect(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), color);
        }

        private static void DrawPanelRect(Rect rect)
        {
            FillRect(rect, PanelColor);
            DrawBorder(rect, PanelBorder, 2f);
        }

        private static string ShortName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "?";
            return name.Length <= 8 ? name : name.Substring(0, 8);
        }

        private static Color ItemColor(string id)
        {
            if (id == "test_item") return new Color(0.25f, 0.75f, 0.85f, 1f);
            if (id == "contraband") return new Color(0.75f, 0.22f, 0.22f, 1f);
            return new Color(0.65f, 0.65f, 0.65f, 1f);
        }
    }
}
