using UnityEngine;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Small framed prompt in the lower-left corner. It shows only the place/item
    /// name, slides in from the left, and slides back out when cleared.
    /// </summary>
    public static class InteractionPromptUI
    {
        private const float SlideDuration = 0.18f;
        private const float HiddenOffset = 36f;

        private static GUIStyle _labelStyle;
        private static Font _font;
        private static PromptHost _host;
        private static string _safehouseLabel = string.Empty;
        private static string _storageLabel = string.Empty;
        private static string _transportLabel = string.Empty;
        private static string _busStopLabel = string.Empty;
        private static string _storeLabel = string.Empty;
        private static string _barLabel = string.Empty;
        private static string _neighborLabel = string.Empty;
        private static string _safeLabel = string.Empty;
        private static string _searchLabel = string.Empty;
        private static string _restaurantLabel = string.Empty;
        private static string _autoRepairLabel = string.Empty;
        private static string _nightclubLabel = string.Empty;
        private static string _warehouseLabel = string.Empty;
        private static string _portOfficeLabel = string.Empty;
        private static string _stockroomLabel = string.Empty;
        private static string _depotLabel = string.Empty;
        private static string _busDriveLabel = string.Empty;
        private static string _kitchenLabel = string.Empty;
        private static string _crimeLabel = string.Empty;
        private static string _displayLabel;
        private static float _showStart;
        private static float _hideStart;
        private static float _storageSetTime;
        private static bool _hiding;

        public static void SetPrompt(string source, string label)
        {
            if (source == "storage") { _storageLabel = label ?? string.Empty; _storageSetTime = Time.unscaledTime; }
            else if (source == "transport") _transportLabel = label ?? string.Empty;
            else if (source == "busstop") _busStopLabel = label ?? string.Empty;
            else if (source == "store") _storeLabel = label ?? string.Empty;
            else if (source == "bar") _barLabel = label ?? string.Empty;
            else if (source == "neighbor") _neighborLabel = label ?? string.Empty;
            else if (source == "safe") _safeLabel = label ?? string.Empty;
            else if (source == "search") _searchLabel = label ?? string.Empty;
            else if (source == "restaurant") _restaurantLabel = label ?? string.Empty;
            else if (source == "autorepair") _autoRepairLabel = label ?? string.Empty;
            else if (source == "nightclub") _nightclubLabel = label ?? string.Empty;
            else if (source == "warehouse") _warehouseLabel = label ?? string.Empty;
            else if (source == "portoffice") _portOfficeLabel = label ?? string.Empty;
            else if (source == "stockroom") _stockroomLabel = label ?? string.Empty;
            else if (source == "depot") _depotLabel = label ?? string.Empty;
            else if (source == "busdrive") _busDriveLabel = label ?? string.Empty;
            else if (source == "kitchen") _kitchenLabel = label ?? string.Empty;
            else if (source == "crime") _crimeLabel = label ?? string.Empty;
            else _safehouseLabel = label ?? string.Empty;

            RefreshTarget();

            if (_host == null)
            {
                GameObject hostObject = new GameObject("InteractionPromptHost");
                _host = hostObject.AddComponent<PromptHost>();
            }
        }

        private static string TargetLabel
        {
            get
            {
                if (BusDrivingController.Active != null) return string.Empty;
                if (StorageInteraction.AnyPanelOpen) return string.Empty;
                if (BusTravelController.IsOpen) return string.Empty;
                if (DialogueChoiceController.IsOpen) return string.Empty;
                if (StoreShopUI.IsOpen) return string.Empty;
                if (!string.IsNullOrEmpty(_crimeLabel)) return _crimeLabel;
                if (!string.IsNullOrEmpty(_storageLabel) &&
                    Time.unscaledTime - _storageSetTime <= 0.25f) return _storageLabel;
                if (!string.IsNullOrEmpty(_transportLabel)) return _transportLabel;
                if (!string.IsNullOrEmpty(_storeLabel)) return _storeLabel;
                if (!string.IsNullOrEmpty(_barLabel)) return _barLabel;
                if (!string.IsNullOrEmpty(_neighborLabel)) return _neighborLabel;
                if (!string.IsNullOrEmpty(_safeLabel)) return _safeLabel;
                if (!string.IsNullOrEmpty(_searchLabel)) return _searchLabel;
                if (!string.IsNullOrEmpty(_restaurantLabel)) return _restaurantLabel;
                if (!string.IsNullOrEmpty(_autoRepairLabel)) return _autoRepairLabel;
                if (!string.IsNullOrEmpty(_nightclubLabel)) return _nightclubLabel;
                if (!string.IsNullOrEmpty(_warehouseLabel)) return _warehouseLabel;
                if (!string.IsNullOrEmpty(_portOfficeLabel)) return _portOfficeLabel;
                if (!string.IsNullOrEmpty(_stockroomLabel)) return _stockroomLabel;
                if (!string.IsNullOrEmpty(_depotLabel)) return _depotLabel;
                if (!string.IsNullOrEmpty(_busDriveLabel)) return _busDriveLabel;
                if (!string.IsNullOrEmpty(_kitchenLabel)) return _kitchenLabel;
                if (!string.IsNullOrEmpty(_busStopLabel)) return _busStopLabel;
                return _safehouseLabel;
            }
        }

        private static void RefreshTarget()
        {
            string target = TargetLabel;
            if (!string.IsNullOrEmpty(target))
            {
                bool wasHidden = _displayLabel == null || _hiding;
                _displayLabel = target;
                if (wasHidden)
                {
                    _showStart = Time.unscaledTime;
                    _hiding = false;
                }
            }
            else if (_displayLabel != null && !_hiding)
            {
                _hiding = true;
                _hideStart = Time.unscaledTime;
            }
        }

        private static float CurrentProgress()
        {
            if (_displayLabel == null) return 0f;
            if (_hiding)
            {
                float hidden = 1f - (Time.unscaledTime - _hideStart) / SlideDuration;
                return Mathf.Clamp01(hidden);
            }
            return Mathf.Clamp01((Time.unscaledTime - _showStart) / SlideDuration);
        }

        private static void Render()
        {
            RefreshTarget();
            if (_displayLabel == null) return;

            float progress = CurrentProgress();
            if (_hiding && progress <= 0f)
            {
                _displayLabel = null;
                return;
            }

            EnsureStyle();
            float width = Mathf.Max(116f, _labelStyle.CalcSize(new GUIContent(_displayLabel)).x + 40f);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            float x = Mathf.Lerp(-width - HiddenOffset, 18f, eased);
            Rect box = new Rect(x, Screen.height - 92f, width, 42f);

            DrawSolid(box, new Color(0.96f, 0.95f, 0.91f, 0.98f));
            DrawBorder(box, new Color(0.28f, 0.25f, 0.20f, 1f), 1f);
            DrawBorder(new Rect(box.x + 3f, box.y + 3f, box.width - 6f, box.height - 6f),
                new Color(0.76f, 0.70f, 0.56f, 1f), 1f);

            GUI.Label(new Rect(box.x + 8f, box.y + 6f, box.width - 16f, box.height - 12f), _displayLabel, _labelStyle);
        }

        private static void EnsureStyle()
        {
            if (_labelStyle != null) return;

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Clip
            };

            _font = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei", "Microsoft YaHei UI", "SimHei", "Noto Sans CJK SC", "Arial" }, 18);
            if (_font != null) _labelStyle.font = _font;

            _labelStyle.normal.textColor = new Color(0.12f, 0.11f, 0.09f);
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

        private sealed class PromptHost : MonoBehaviour
        {
            private void OnGUI()
            {
                Render();
            }
        }
    }
}
