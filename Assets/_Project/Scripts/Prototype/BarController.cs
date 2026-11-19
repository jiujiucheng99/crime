using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Bar interior at F8. Enter/exit only; no drink ordering yet.
    /// </summary>
    public class BarController : MonoBehaviour
    {
        private const float EnterRange = 1.3f;
        private const float ExitRange = 1.1f;
        private static readonly Vector3 InteriorOffset = new Vector3(3000f, 0f, 0f);
        private static readonly Vector3 InteriorSpawn = new Vector3(3000f, -3.2f, 0f);
        private static readonly Vector2 InteriorExitPoint = new Vector2(3000f, -4.1f);
        private static readonly Vector2 BartenderPoint = new Vector2(2997.4f, 0.65f);
        private static readonly Vector2 FixerPoint = new Vector2(2995.5f, -2.5f);

        private Transform _worldRoot;
        private Transform _player;
        private DialogueChoiceController _dialogue;
        private PlayerWallet _wallet;
        private PlayerInventory _pockets;
        private PlayerSkills _skills;
        private FenceComponent _fence;
        private Vector2 _exteriorDoor;
        private GameObject _interiorRoot;
        private bool _inside;
        private bool _medicalQuestDone;
        private string _prompt;

        public bool IsInside => _inside;

        public void Configure(Transform worldRoot, Transform player, Vector2 exteriorDoor,
            DialogueChoiceController dialogue = null, PlayerWallet wallet = null)
        {
            _worldRoot = worldRoot;
            _player = player;
            _exteriorDoor = exteriorDoor;
            _dialogue = dialogue;
            _wallet = wallet;
            _pockets = player != null ? player.GetComponent<PlayerInventory>() : null;
            _skills = player != null ? player.GetComponent<PlayerSkills>() : null;
            _fence = FenceComponent.Create(transform, "BarFence", _pockets, _wallet, dialogue);
        }

        private void Update()
        {
            if (_player == null || StorageInteraction.AnyPanelOpen ||
                BusTravelController.IsOpen || DialogueChoiceController.IsOpen ||
                StoreShopUI.IsOpen || FenceUI.IsOpen) return;

            bool nearEntrance = !_inside && Vector2.Distance(_player.position, _exteriorDoor) <= EnterRange;
            bool nearExit = _inside && Vector2.Distance(_player.position, InteriorExitPoint) <= ExitRange;
            bool nearBartender = _inside && Vector2.Distance(_player.position, BartenderPoint) <= 1.3f;
            bool nearFixer = _inside && Vector2.Distance(_player.position, FixerPoint) <= 1.3f;
            _prompt = !_inside
                ? (nearEntrance ? "酒吧" : string.Empty)
                : (nearFixer ? "瘦猴" : (nearBartender ? "酒保" : (nearExit ? "街道" : string.Empty)));

            if (!InteractionInput.TryConsumeF(!string.IsNullOrEmpty(_prompt))) return;
            if (!_inside && nearEntrance) Enter();
            else if (_inside && nearFixer) OpenFixerDialogue();
            else if (_inside && nearBartender && _dialogue != null) OpenBartenderDialogue();
            else if (_inside && nearExit) Exit();
        }

        private void OpenFixerDialogue()
        {
            if (_dialogue == null) return;
            CrimeStoryState.LockpickTeacherMet = true;

            if (_skills != null && _skills.Has("lockpicking"))
            {
                _dialogue.OpenStory("瘦猴",
                    "门已经会开了？那就别再问我怎么进去。\n" +
                    "这片街区的人睡得浅。你跑、你翻、你撬，都会留下声音。\n" +
                    "警察会晚一步，酒吧这边的人可不会。\n" +
                    "你欠我的人情，不是账单，是别人替你记着的事。",
                    "离开", null,
                    null, null,
                    null, null);
                return;
            }

            _dialogue.OpenStory("瘦猴",
                "你总在这附近转。巷子里那几户人家，晚上都没人。\n" +
                "想学开门？我不收钱，先欠我一个人情。\n" +
                "以后会有人来找你，到时候别装傻。\n" +
                "至于为什么信我：这条街上的消息，比警察快。",
                "学开锁（欠一个人情）", LearnLockpicking,
                "先算了", null,
                null, null);
        }

        private void LearnLockpicking()
        {
            if (_skills == null) return;
            _skills.Learn("lockpicking");
            CrimeStoryState.OwesFavor = true;
            CrimeStoryState.LockpickTeacherMet = true;
            if (_dialogue != null)
            {
                _dialogue.OpenStory("瘦猴",
                    "看好了。回形针别弯太狠，听见锁芯响就停。\n" +
                    "记住，你欠我的不是钱，是一个人情。\n" +
                    "还有，动静小一点。邻居比警察先听见。",
                    "明白了", null,
                    null, null,
                    null, null);
            }
        }

        private void OpenBartenderDialogue()
        {
            if (_medicalQuestDone)
            {
                _dialogue.Open("酒保", "还有货要出手吗？", "销赃", () =>
                {
                    if (_fence != null) _fence.SellAll();
                });
                return;
            }

            _dialogue.OpenChoice("酒保", "有医药包，还是有货要出手？",
                "上交医药包", SubmitMedicalKit,
                "销赃", () =>
                {
                    if (_fence != null) _fence.SellAll();
                });
        }

        private void SubmitMedicalKit()
        {
            if (_pockets == null || !_pockets.TryRemove("first_aid"))
            {
                _dialogue.OpenNotice("酒保", "你身上没有急救包。");
                return;
            }

            if (_wallet != null) _wallet.Add(7);
            _medicalQuestDone = true;
            _dialogue.OpenNotice("酒保", "交易完成，这是 7 块。");
        }

        private void OnGUI()
        {
            InteractionPromptUI.SetPrompt("bar", FenceUI.IsOpen ? string.Empty : _prompt);
        }

        private void Enter()
        {
            if (_worldRoot == null || _player == null) return;
            _inside = true;
            _worldRoot.gameObject.SetActive(false);
            if (_interiorRoot == null) BuildInterior();
            else _interiorRoot.SetActive(true);

            Rigidbody2D body = _player.GetComponent<Rigidbody2D>();
            if (body != null) body.velocity = Vector2.zero;
            _player.position = InteriorSpawn;
        }

        private void Exit()
        {
            _inside = false;
            if (_interiorRoot != null) _interiorRoot.SetActive(false);
            if (_worldRoot != null) _worldRoot.gameObject.SetActive(true);

            Rigidbody2D body = _player.GetComponent<Rigidbody2D>();
            if (body != null) body.velocity = Vector2.zero;
            _player.position = new Vector3(_exteriorDoor.x, _exteriorDoor.y - 0.8f, 0f);
        }

        private void BuildInterior()
        {
            _interiorRoot = new GameObject("BarInterior");
            _interiorRoot.transform.position = InteriorOffset;

            Sprite floor = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Bar/bar_floor");
            Sprite wall = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Bar/bar_wall");
            Sprite counter = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Bar/bar_counter");
            Sprite backbar = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Bar/bar_backbar");
            Sprite stool = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Bar/bar_stool");
            Sprite booth = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Bar/bar_booth");
            Sprite bartender = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Bar/bar_bartender");
            Sprite neon = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Bar/bar_neon");

            CreateTiled(_interiorRoot.transform, "Floor", floor, Vector3.zero, new Vector2(12f, 9f), -20000, false);
            CreateTiled(_interiorRoot.transform, "WallTop", wall, new Vector3(0f, 4.5f, 0f), new Vector2(12f, 0.5f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallLeft", wall, new Vector3(-6f, 0f, 0f), new Vector2(0.4f, 9f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallRight", wall, new Vector3(6f, 0f, 0f), new Vector2(0.4f, 9f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallBottomLeft", wall, new Vector3(-3.5f, -4.5f, 0f), new Vector2(5f, 0.5f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallBottomRight", wall, new Vector3(3.5f, -4.5f, 0f), new Vector2(5f, 0.5f), -19990, true);

            // Bar counter, back bar and bartender.
            CreateSprite(_interiorRoot.transform, "BackBar", backbar, new Vector3(-2.6f, 3.4f, 0f),
                new Vector2(4.4f, 1.5f), true, -953);
            CreateSprite(_interiorRoot.transform, "BarCounter", counter, new Vector3(-2.6f, 1.7f, 0f),
                new Vector2(4.4f, 1.4f), true, -950, 4.0f, 0.75f);
            CreateSprite(_interiorRoot.transform, "Bartender", bartender, new Vector3(-2.6f, 2.85f, 0f),
                new Vector2(0.85f, 1.25f), false, -952);

            // Stools in front of the counter.
            CreateSprite(_interiorRoot.transform, "StoolA", stool, new Vector3(-4.1f, 0.55f, 0f), new Vector2(0.55f, 0.8f), false, -949);
            CreateSprite(_interiorRoot.transform, "StoolB", stool, new Vector3(-2.6f, 0.55f, 0f), new Vector2(0.55f, 0.8f), false, -949);
            CreateSprite(_interiorRoot.transform, "StoolC", stool, new Vector3(-1.1f, 0.55f, 0f), new Vector2(0.55f, 0.8f), false, -949);

            // Booths on the right.
            CreateSprite(_interiorRoot.transform, "BoothA", booth, new Vector3(3.6f, 1.3f, 0f), new Vector2(3.1f, 2.1f), true, -950);
            CreateSprite(_interiorRoot.transform, "BoothB", booth, new Vector3(3.6f, -1.7f, 0f), new Vector2(3.1f, 2.1f), true, -950);

            CreateSprite(_interiorRoot.transform, "NeonSign", neon, new Vector3(-2.6f, 4.1f, 0f), new Vector2(2.6f, 0.9f), false, -948);

            CreatePatron(_interiorRoot.transform, "PatronA", "patron_01", new Vector3(2.15f, 1.3f, 0f), 1.45f);
            CreatePatron(_interiorRoot.transform, "PatronB", "patron_02", new Vector3(2.15f, -1.7f, 0f), 1.45f);
            CreatePatron(_interiorRoot.transform, "PatronC", "patron_03", new Vector3(-4.1f, 0.55f, 0f), 1.45f);
            CreatePatron(_interiorRoot.transform, "Fixer", "patron_02", new Vector3(-4.5f, -1.6f, 0f), 1.45f);
            WorkAreaMarker.Create(_interiorRoot.transform, "FixerMarker",
                new Vector3(-4.5f, -0.25f, 0f), new Color(0.95f, 0.78f, 0.22f));

            CreateBlock(_interiorRoot.transform, "Entrance", new Vector3(0f, -4.48f, 0f),
                new Vector2(2f, 0.52f), new Color(0.08f, 0.10f, 0.11f), true, -947);
        }

        private static void CreatePatron(Transform parent, string name, string resourceName,
            Vector3 localPosition, float targetHeight)
        {
            Sprite sprite = Resources.Load<Sprite>("Characters/BarPatrons/" + resourceName);
            if (sprite == null) return;

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = Mathf.RoundToInt(-localPosition.y * 100f);
            float scale = targetHeight / Mathf.Max(0.001f, sprite.bounds.size.y);
            go.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private static void CreateSprite(Transform parent, string name, Sprite sprite, Vector3 localPosition,
            Vector2 size, bool collider, int sortingOrder, float colliderWidth = 0f, float colliderHeight = 0f)
        {
            if (sprite == null) return;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            go.transform.localScale = new Vector3(
                size.x / Mathf.Max(0.001f, sprite.bounds.size.x),
                size.y / Mathf.Max(0.001f, sprite.bounds.size.y), 1f);

            if (collider)
            {
                float scaleX = Mathf.Max(0.001f, go.transform.localScale.x);
                float scaleY = Mathf.Max(0.001f, go.transform.localScale.y);
                float worldWidth = colliderWidth > 0f ? colliderWidth : size.x;
                float worldHeight = colliderHeight > 0f ? colliderHeight : size.y * 0.65f;
                BoxCollider2D box = go.AddComponent<BoxCollider2D>();
                box.size = new Vector2(worldWidth / scaleX, worldHeight / scaleY);
                box.offset = new Vector2(0f, -0.06f / scaleY);
            }
        }

        private static void CreateTiled(Transform parent, string name, Sprite sprite, Vector3 localPosition,
            Vector2 size, int sortingOrder, bool collider)
        {
            if (sprite == null) return;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = size;
            renderer.sortingOrder = sortingOrder;

            if (collider)
            {
                BoxCollider2D box = go.AddComponent<BoxCollider2D>();
                box.size = size;
            }
        }

        private static void CreateBlock(Transform parent, string name, Vector3 localPosition, Vector2 size,
            Color color, bool collider, int sortingOrder)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = WhiteSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            if (collider)
            {
                BoxCollider2D box = go.AddComponent<BoxCollider2D>();
                box.size = Vector2.one;
            }
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
