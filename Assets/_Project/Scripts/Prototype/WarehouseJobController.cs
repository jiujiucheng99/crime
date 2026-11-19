using System.Collections.Generic;
using UnityEngine;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Warehouse shift: carry four labelled crates to matching floor zones.
    /// This controller owns the work prompts; WarehouseController only handles
    /// entering/exiting and forwards F to the job.
    /// </summary>
    public class WarehouseJobController : MonoBehaviour
    {
        private const int CratesPerShift = 4;
        private const int BaseWage = 15;
        private const float InteractRange = 1.35f;
        private const float CarryOffsetY = 1.35f;
        private static readonly Vector3 InteriorOffset = new Vector3(9000f, 0f, 0f);
        private static readonly Vector2 DeskPoint = new Vector2(9005.4f, -2.2f);

        private sealed class Crate
        {
            public GameObject GameObject;
            public string Label;
            public string Target;
            public bool Delivered;
        }

        private Transform _player;
        private PlayerEmployment _employment;
        private PlayerWallet _wallet;
        private PlayerSkills _skills;
        private PlayerMotor2D _motor;
        private DialogueChoiceController _dialogue;
        private readonly List<Crate> _crates = new List<Crate>();
        private readonly Dictionary<string, Vector2> _zones = new Dictionary<string, Vector2>
        {
            { "A", new Vector2(9000f - 4.4f, -0.9f) },
            { "B", new Vector2(9000f, -1.6f) },
            { "C", new Vector2(9000f + 3.8f, -2.2f) }
        };
        private readonly List<GameObject> _zoneMarkers = new List<GameObject>();
        private Crate _carried;
        private bool _shiftActive;
        private int _delivered;
        private int _mistakes;
        private float _startTime;
        private GUIStyle _style;
        private GUIStyle _smallStyle;

        public bool IsShiftActive => _shiftActive;

        public void Configure(Transform player, PlayerEmployment employment,
            PlayerWallet wallet, PlayerSkills skills, DialogueChoiceController dialogue)
        {
            _player = player;
            _employment = employment;
            _wallet = wallet;
            _skills = skills;
            _dialogue = dialogue;
            _motor = player != null ? player.GetComponent<PlayerMotor2D>() : null;
        }

        private void Update()
        {
            if (!_shiftActive) return;
            if (_carried != null && _player != null)
            {
                _carried.GameObject.transform.position = _player.position + new Vector3(0f, CarryOffsetY, 0f);
                SpriteRenderer renderer = _carried.GameObject.GetComponent<SpriteRenderer>();
                if (renderer != null) renderer.sortingOrder = Mathf.RoundToInt(-_player.position.y * 100f) + 2;
            }
        }

        public bool TryGetPrompt(out string prompt)
        {
            prompt = null;
            if (!_shiftActive)
            {
                if (Vector2.Distance(_player.position, DeskPoint) <= InteractRange)
                {
                    prompt = "工单";
                    return true;
                }
                return false;
            }

            if (_carried != null)
            {
                foreach (Vector2 zone in _zones.Values)
                {
                    if (Vector2.Distance(_player.position, zone) <= InteractRange)
                    {
                        prompt = "放下木箱";
                        return true;
                    }
                }
                return false;
            }

            Crate near = FindNearCrate();
            if (near != null)
            {
                prompt = "搬起木箱 " + near.Label;
                return true;
            }
            return false;
        }

        public void Interact()
        {
            if (!_shiftActive)
            {
                StartShift();
                return;
            }

            if (_carried != null)
            {
                TryDeliver();
                return;
            }

            Crate near = FindNearCrate();
            if (near != null) PickUp(near);
        }

        public void CancelShift()
        {
            _shiftActive = false;
            _carried = null;
            if (_motor != null) _motor.SpeedMultiplier = 1f;
            ClearCrates();
            ClearZoneMarkers();
        }

        private void StartShift()
        {
            if (_employment == null || _employment.CurrentJobId != "warehouse_worker")
            {
                if (_dialogue != null) _dialogue.OpenNotice("仓库", "需要先在南岸港口办公室入职仓库工人。");
                return;
            }

            _shiftActive = true;
            _delivered = 0;
            _mistakes = 0;
            _startTime = Time.time;
            _carried = null;
            ClearCrates();
            BuildCrates();
            BuildZoneMarkers();
            if (_dialogue != null) _dialogue.OpenNotice("仓库", "班次开始：把 4 个木箱送到对应区域。");
        }

        private void BuildCrates()
        {
            Sprite crateSprite = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Warehouse/warehouse_crate");
            string[] labels = { "A", "B", "C", RandomLabel() };
            Shuffle(labels);
            Vector3[] positions =
            {
                new Vector3(-2.4f, 2.4f, 0f),
                new Vector3(-0.8f, 2.4f, 0f),
                new Vector3(0.8f, 2.4f, 0f),
                new Vector3(2.4f, 2.4f, 0f)
            };
            Shuffle(positions);

            for (int i = 0; i < CratesPerShift; i++)
            {
                GameObject go = new GameObject("WorkCrate" + (i + 1));
                go.transform.position = InteriorOffset + positions[i];
                SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = crateSprite;
                renderer.color = ZoneColor(labels[i]);
                renderer.sortingOrder = Mathf.RoundToInt(-go.transform.position.y * 100f);
                if (crateSprite != null)
                {
                    float scale = 1.2f / Mathf.Max(0.001f, crateSprite.bounds.size.x);
                    go.transform.localScale = new Vector3(scale, scale, 1f);
                }

                _crates.Add(new Crate
                {
                    GameObject = go,
                    Label = labels[i],
                    Target = labels[i]
                });
            }
        }

        private Crate FindNearCrate()
        {
            foreach (Crate crate in _crates)
            {
                if (crate.Delivered) continue;
                if (Vector2.Distance(_player.position, crate.GameObject.transform.position) <= InteractRange)
                    return crate;
            }
            return null;
        }

        private void PickUp(Crate crate)
        {
            _carried = crate;
            if (_motor != null)
            {
                int logistics = _skills != null ? Mathf.Max(1, _skills.LevelOf("logistics")) : 1;
                _motor.SpeedMultiplier = SkillEffects.LogisticsSpeedMultiplier(logistics);
            }
        }

        private void TryDeliver()
        {
            Vector2 target;
            if (!_zones.TryGetValue(_carried.Target, out target)) return;
            if (Vector2.Distance(_player.position, target) > InteractRange)
            {
                if (_dialogue != null) _dialogue.OpenNotice("仓库", "这里不是 " + _carried.Target + " 区。");
                _mistakes++;
                return;
            }

            _carried.Delivered = true;
            Object.Destroy(_carried.GameObject);
            _carried = null;
            if (_motor != null) _motor.SpeedMultiplier = 1f;
            _delivered++;
            if (_delivered >= CratesPerShift) EndShift();
        }

        private void EndShift()
        {
            _shiftActive = false;
            if (_motor != null) _motor.SpeedMultiplier = 1f;
            ClearCrates();
            ClearZoneMarkers();

            int elapsedPenalty = Mathf.FloorToInt((Time.time - _startTime) / 20f);
            int logistics = _skills != null ? Mathf.Max(1, _skills.LevelOf("logistics")) : 1;
            int maxBonus = SkillEffects.LogisticsBonusCap(logistics);
            int bonus = Mathf.Clamp(maxBonus - _mistakes * 2 - elapsedPenalty, 0, maxBonus);
            int wage = BaseWage + bonus;
            if (_wallet != null) _wallet.Add(wage);
            if (_skills != null) _skills.AddExperience("logistics", 12 + bonus);
            if (_dialogue != null)
            {
                _dialogue.OpenNotice("仓库", "班次完成，工资 " + wage + " 块（基础 " + BaseWage + " + 表现 " + bonus + "）。");
            }
        }

        private void BuildZoneMarkers()
        {
            ClearZoneMarkers();
            foreach (KeyValuePair<string, Vector2> pair in _zones)
            {
                GameObject marker = new GameObject("Zone" + pair.Key);
                marker.transform.position = pair.Value;
                marker.transform.localScale = new Vector3(2.2f, 2.2f, 1f);
                SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
                renderer.sprite = WhiteSprite;
                renderer.color = new Color(ZoneColor(pair.Key).r, ZoneColor(pair.Key).g, ZoneColor(pair.Key).b, 0.24f);
                renderer.sortingOrder = -10940;
                _zoneMarkers.Add(marker);
            }
        }

        private void ClearCrates()
        {
            foreach (Crate crate in _crates)
            {
                if (crate.GameObject != null) Object.Destroy(crate.GameObject);
            }
            _crates.Clear();
        }

        private void ClearZoneMarkers()
        {
            foreach (GameObject marker in _zoneMarkers)
            {
                if (marker != null) Object.Destroy(marker);
            }
            _zoneMarkers.Clear();
        }

        private static Color ZoneColor(string label)
        {
            if (label == "A") return new Color(0.92f, 0.72f, 0.20f);
            if (label == "B") return new Color(0.24f, 0.66f, 0.86f);
            return new Color(0.28f, 0.78f, 0.38f);
        }

        private static string RandomLabel()
        {
            int value = Random.Range(0, 3);
            return value == 0 ? "A" : (value == 1 ? "B" : "C");
        }

        private static void Shuffle(string[] values)
        {
            for (int i = values.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                string temp = values[i];
                values[i] = values[j];
                values[j] = temp;
            }
        }

        private static void Shuffle(Vector3[] values)
        {
            for (int i = values.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                Vector3 temp = values[i];
                values[i] = values[j];
                values[j] = temp;
            }
        }

        private void OnGUI()
        {
            if (!_shiftActive) return;
            EnsureStyles();
            string next = _carried != null
                ? "搬运 " + _carried.Label + " → " + _carried.Target + " 区"
                : "找木箱并搬到对应颜色区域  木箱颜色对应目标区  A黄 / B蓝 / C绿";
            Rect rect = new Rect(Screen.width * 0.5f - 230f, 18f, 460f, 30f);
            DrawSolid(rect, new Color(0.04f, 0.05f, 0.06f, 0.9f));
            DrawSolid(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), new Color(0.62f, 0.52f, 0.32f));
            GUI.Label(rect, "仓库清单 " + _delivered + " / " + CratesPerShift + "    " + next, _style);
        }

        private void EnsureStyles()
        {
            if (_style != null) return;
            Font font = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei", "Microsoft YaHei UI", "SimHei", "Noto Sans CJK SC", "Arial" }, 16);
            _style = new GUIStyle(GUI.skin.label)
            {
                font = font, fontSize = 15, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.95f, 0.89f, 0.72f) }
            };
            _smallStyle = new GUIStyle(GUI.skin.label)
            {
                font = font, fontSize = 13,
                normal = { textColor = new Color(0.8f, 0.82f, 0.76f) }
            };
        }

        private static void DrawSolid(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static Sprite _whiteSprite;
        private static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite != null) return _whiteSprite;
                Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[32 * 32];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                texture.SetPixels(pixels);
                texture.Apply();
                _whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 32f, 32f), new Vector2(0.5f, 0.5f), 32f);
                return _whiteSprite;
            }
        }
    }
}
