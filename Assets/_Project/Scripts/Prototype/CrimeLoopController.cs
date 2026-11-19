using UnityEngine;

namespace AmberCity.Prototype
{
    /// <summary>
    /// First burglary loop: noise, neighbour discovery, delayed police and a
    /// faster gang collector. No wanted stars and no combat are used.
    /// </summary>
    public class CrimeLoopController : MonoBehaviour
    {
        private const float SprintNoisePerSecond = 28f;
        private const float WalkNoisePerSecond = 2.5f;

        public static CrimeLoopController Instance { get; private set; }

        private Transform _player;
        private Transform _worldRoot;
        private PlayerMotor2D _motor;
        private PlayerInventory _inventory;
        private PlayerWallet _wallet;
        private DialogueChoiceController _dialogue;
        private NeighborApartmentController _neighbor;
        private Vector2 _lastNeighborDoor;
        private int _stolenAtEntry;
        private float _reportTimer;
        private float _policeTimer;
        private float _gangTimer;
        private string _toast;
        private float _toastTimer;
        private bool _highNoiseWarned;
        private bool _midNoiseWarned;
        private bool _caughtRisk;
        private GUIStyle _style;
        private GUIStyle _toastStyle;

        public void Configure(Transform player, Transform worldRoot, PlayerInventory inventory, PlayerWallet wallet,
            DialogueChoiceController dialogue)
        {
            _player = player;
            _worldRoot = worldRoot;
            _inventory = inventory;
            _wallet = wallet;
            _dialogue = dialogue;
            _motor = player != null ? player.GetComponent<PlayerMotor2D>() : null;
        }

        private void Awake()
        {
            Instance = this;
        }

        public void OnNeighborEnter(NeighborApartmentController neighbor)
        {
            _neighbor = neighbor;
            _stolenAtEntry = StolenGoods.CountStolen(_inventory);
            TheftNoise.Reset();
            TheftNoise.InsideTheft = true;
            _highNoiseWarned = false;
            _midNoiseWarned = false;
            _caughtRisk = false;
        }

        public void OnNeighborExit(NeighborApartmentController neighbor)
        {
            TheftNoise.InsideTheft = false;
            int stolenNow = StolenGoods.CountStolen(_inventory);
            if (stolenNow > _stolenAtEntry)
            {
                CrimeStoryState.LastNeighborName = neighbor != null ? neighbor.DisplayName : "邻居";
                _lastNeighborDoor = neighbor != null ? neighbor.ExteriorDoor : Vector2.zero;

                if (_caughtRisk || TheftNoise.Value >= 85f)
                {
                    StolenGoods.RemoveAll(_inventory);
                    if (_wallet != null) _wallet.TrySpend(10);
                    CrimeStoryState.NeighborReported = true;
                    Toast("邻居在门口堵住了你：赃物被夺回，罚了 10 块。");
                    TheftNoise.Reset();
                    _neighbor = null;
                    return;
                }

                float delay = Mathf.Clamp(90f - TheftNoise.Value * 0.7f, 20f, 90f);
                _reportTimer = delay;
                Toast("你离开了" + CrimeStoryState.LastNeighborName + "。里面少了东西。");
            }
            TheftNoise.Reset();
            _neighbor = null;
        }

        private void Update()
        {
            if (TheftNoise.InsideTheft)
            {
                if (_motor != null)
                {
                    if (_motor.IsSprinting) TheftNoise.Add(SprintNoisePerSecond * Time.deltaTime);
                    else if (_motor.MoveInput.sqrMagnitude > 0.01f)
                        TheftNoise.Add(WalkNoisePerSecond * Time.deltaTime);
                }
                TheftNoise.Decay(Time.deltaTime);

                if (!_midNoiseWarned && TheftNoise.Value >= 60f)
                {
                    _midNoiseWarned = true;
                    Toast("邻居翻了个身。放轻脚步，或者准备离开。");
                }
                if (!_highNoiseWarned && TheftNoise.Value >= 80f)
                {
                    _highNoiseWarned = true;
                    Toast("邻居被惊醒了。再吵下去，你会在门口被堵住。");
                }
                if (TheftNoise.Value >= 85f) _caughtRisk = true;
            }
            else
            {
                TheftNoise.Decay(Time.deltaTime);
            }

            if (_toastTimer > 0f) _toastTimer -= Time.deltaTime;

            if (_reportTimer > 0f)
            {
                _reportTimer -= Time.deltaTime;
                if (_reportTimer <= 0f) ReportTheft();
            }

            if (_policeTimer > 0f)
            {
                _policeTimer -= Time.deltaTime;
                if (_policeTimer <= 0f) SpawnPolice();
            }

            if (_gangTimer > 0f)
            {
                _gangTimer -= Time.deltaTime;
                if (_gangTimer <= 0f) SpawnGang();
            }
        }

        private void ReportTheft()
        {
            CrimeStoryState.NeighborReported = true;
            Toast("邻居发现失窃，报警了。酒吧那边的消息更快。");
            _policeTimer = 28f;
            _gangTimer = 14f;
        }

        private void SpawnPolice()
        {
            Vector3 position = (Vector3)_lastNeighborDoor +
                new Vector3(2.0f, -Mathf.Sign(_lastNeighborDoor.y) * 2.0f, 0f);
            GameObject npc = CreateNpc("PoliceResponder", position,
                "Characters/Police/police_officer_front", 1.5f);
            CrimeResponder responder = npc.AddComponent<CrimeResponder>();
            responder.Configure(CrimeResponder.ResponderKind.Police,
                _player, _dialogue, _wallet, _inventory);
            Toast("警笛声在附近响起。");
        }

        private void SpawnGang()
        {
            Vector3 position = new Vector3(2.6f, 5.2f, 0f);
            GameObject npc = CreateNpc("GangCollector", position,
                "Characters/BarPatrons/patron_02", 1.35f);
            SpriteRenderer renderer = npc.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = new Color(0.72f, 0.42f, 0.42f);
            CrimeResponder responder = npc.AddComponent<CrimeResponder>();
            responder.Configure(CrimeResponder.ResponderKind.Gang,
                _player, _dialogue, _wallet, _inventory);
            Toast("有人在你家楼下等你。");
        }

        private GameObject CreateNpc(string name, Vector3 position, string resourcePath, float height)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            GameObject go = new GameObject(name);
            go.transform.position = position;
            if (_worldRoot != null) go.transform.SetParent(_worldRoot, true);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = Mathf.RoundToInt(-position.y * 100f);
            if (sprite != null)
            {
                float scale = height / Mathf.Max(0.001f, sprite.bounds.size.y);
                go.transform.localScale = new Vector3(scale, scale, 1f);
            }
            return go;
        }

        private void Toast(string message)
        {
            _toast = message;
            _toastTimer = 5f;
        }

        private void OnGUI()
        {
            if (TheftNoise.InsideTheft || TheftNoise.Value > 0f)
            {
                EnsureStyles();
                Rect box = new Rect(Screen.width * 0.5f - 170f, 54f, 340f, 28f);
                DrawSolid(box, new Color(0.04f, 0.05f, 0.06f, 0.9f));
                float value = TheftNoise.Value;
                Color color = value >= 85f
                    ? new Color(0.72f, 0.18f, 0.16f)
                    : (value >= 60f ? new Color(0.78f, 0.56f, 0.18f) : new Color(0.34f, 0.62f, 0.34f));
                DrawSolid(new Rect(box.x + 3f, box.y + 3f, (box.width - 6f) * value / 100f, box.height - 6f), color);
                GUI.Label(box, "噪音 " + Mathf.RoundToInt(value) + "  " + TheftNoise.Label(), _style);
            }

            if (_toastTimer > 0f && !string.IsNullOrEmpty(_toast))
            {
                EnsureStyles();
                Rect box = new Rect(Screen.width * 0.5f - 230f, 88f, 460f, 28f);
                DrawSolid(box, new Color(0.04f, 0.05f, 0.06f, 0.92f));
                GUI.Label(box, _toast, _toastStyle);
            }
        }

        private void EnsureStyles()
        {
            if (_style != null) return;
            Font font = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei", "Microsoft YaHei UI", "SimHei", "Noto Sans CJK SC", "Arial" }, 15);
            _style = new GUIStyle(GUI.skin.label)
            {
                font = font, fontSize = 13, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.96f, 0.92f, 0.80f) }
            };
            _toastStyle = new GUIStyle(GUI.skin.label)
            {
                font = font, fontSize = 13, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.92f, 0.86f, 0.72f) }
            };
        }

        private static void DrawSolid(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
