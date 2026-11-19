using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Convenience store at D8. Basic enter/exit only for now; the sales floor is
    /// fully laid out but there is no buying interaction yet.
    /// </summary>
    public class ConvenienceStoreController : MonoBehaviour
    {
        private const float EnterRange = 1.3f;
        private const float ExitRange = 1.1f;
        private static readonly Vector3 InteriorOffset = new Vector3(2000f, 0f, 0f);
        private static readonly Vector3 InteriorSpawn = new Vector3(2000f, -2.9f, 0f);
        private static readonly Vector2 InteriorExitPoint = new Vector2(2000f, -3.9f);
        private static readonly Vector2 InteriorClerkPoint = new Vector2(2003.4f, 0.65f);
        private static readonly Vector2 StockroomPassagePoint = new Vector2(2004.7f, 2.7f);

        private Transform _worldRoot;
        private Transform _player;
        private DialogueChoiceController _dialogue;
        private StoreShopUI _shop;
        private JobOfferComponent _jobOffer;
        private StoreClerkJob _clerkJob;
        private StockroomController _stockroom;
        private PlayerEmployment _employment;
        private Vector2 _exteriorDoor;
        private GameObject _interiorRoot;
        private bool _inside;
        private string _prompt;

        public bool IsInside => _inside;

        public void Configure(Transform worldRoot, Transform player, Vector2 exteriorDoor,
            DialogueChoiceController dialogue = null, StoreShopUI shop = null)
        {
            _worldRoot = worldRoot;
            _player = player;
            _exteriorDoor = exteriorDoor;
            _dialogue = dialogue;
            _shop = shop;

            _jobOffer = JobOfferComponent.Create(transform, "StoreJobOffer",
                player != null ? player.GetComponent<PlayerEmployment>() : null, dialogue,
                "store_clerk", "便利店店员", "便利店 D8");

            _employment = player != null ? player.GetComponent<PlayerEmployment>() : null;
            GameObject clerkJobObject = new GameObject("StoreClerkJob");
            clerkJobObject.transform.SetParent(transform, false);
            _clerkJob = clerkJobObject.AddComponent<StoreClerkJob>();
            _clerkJob.Configure(player, dialogue,
                player != null ? player.GetComponent<PlayerWallet>() : null, _employment);

            GameObject stockroomObject = new GameObject("ConvenienceStockroomController");
            stockroomObject.transform.SetParent(transform, false);
            _stockroom = stockroomObject.AddComponent<StockroomController>();
            _stockroom.Configure(player, dialogue, _employment,
                player != null ? player.GetComponent<PlayerWallet>() : null);
        }

        private void Update()
        {
            if (_player == null || StorageInteraction.AnyPanelOpen ||
                BusTravelController.IsOpen || DialogueChoiceController.IsOpen || StoreShopUI.IsOpen ||
                (_stockroom != null && _stockroom.IsInside)) return;

            bool nearEntrance = !_inside && Vector2.Distance(_player.position, _exteriorDoor) <= EnterRange;
            bool nearExit = _inside && Vector2.Distance(_player.position, InteriorExitPoint) <= ExitRange;
            bool nearClerk = _inside && Vector2.Distance(_player.position, InteriorClerkPoint) <= 1.2f;
            bool nearPassage = _inside && Vector2.Distance(_player.position, StockroomPassagePoint) <= 1.4f;
            bool nearService = _inside && _clerkJob != null && _clerkJob.IsShiftActive &&
                Vector2.Distance(_player.position, StoreClerkJob.ServicePoint) <= 1.25f;

            _prompt = !_inside
                ? (nearEntrance ? "便利店" : string.Empty)
                : _clerkJob != null && _clerkJob.IsShiftActive
                    ? (nearService ? "顾客" : (nearExit ? "街道" : string.Empty))
                    : (nearPassage ? "员工通道" : (nearClerk ? "店员" : (nearExit ? "街道" : string.Empty)));

            if (!InteractionInput.TryConsumeF(!string.IsNullOrEmpty(_prompt))) return;

            if (!_inside && nearEntrance) Enter();
            else if (_inside && nearPassage)
            {
                if (_employment != null && _employment.CurrentJobId == "store_clerk") _stockroom.Enter(_interiorRoot);
                else if (_dialogue != null) _dialogue.OpenNotice("员工通道", "员工专用，请先入职便利店。");
            }
            else if (_inside && _clerkJob != null && _clerkJob.IsShiftActive)
            {
                if (nearService) _clerkJob.ServeCustomer();
                else if (nearExit) Exit();
            }
            else if (_inside && nearClerk && _dialogue != null)
            {
                bool isClerk = _employment != null && _employment.CurrentJobId == "store_clerk";
                if (isClerk)
                {
                    _dialogue.OpenChoice3("店员", "要开始上班、买点东西，还是办理离职？",
                        "上班", () => { if (_clerkJob != null) _clerkJob.StartShift(); },
                        "购物", () => { if (_shop != null) _shop.Open(); },
                        "离职", () => { if (_jobOffer != null) _jobOffer.Offer(); });
                }
                else
                {
                    _dialogue.OpenChoice("店员", "您好，需要点什么？想买东西，还是想找份工作？",
                        "购物", () => { if (_shop != null) _shop.Open(); },
                        "求职", () => { if (_jobOffer != null) _jobOffer.Offer(); });
                }
            }
            else if (_inside && nearExit) Exit();
        }

        private void OnGUI()
        {
            InteractionPromptUI.SetPrompt("store", _prompt);
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
            if (_clerkJob != null && _clerkJob.IsShiftActive) _clerkJob.CancelShift();
            if (_interiorRoot != null) _interiorRoot.SetActive(false);
            if (_worldRoot != null) _worldRoot.gameObject.SetActive(true);

            Rigidbody2D body = _player.GetComponent<Rigidbody2D>();
            if (body != null) body.velocity = Vector2.zero;
            _player.position = new Vector3(_exteriorDoor.x, _exteriorDoor.y - 0.8f, 0f);
        }

        private void BuildInterior()
        {
            _interiorRoot = new GameObject("ConvenienceStoreInterior");
            _interiorRoot.transform.position = InteriorOffset;

            Sprite floor = Resources.Load<Sprite>("Environment/BrickRow/Businesses/ConvenienceStore/store_floor");
            Sprite wall = Resources.Load<Sprite>("Environment/BrickRow/Businesses/ConvenienceStore/store_wall");
            Sprite shelf = Resources.Load<Sprite>("Environment/BrickRow/Businesses/ConvenienceStore/store_shelf");
            Sprite counter = Resources.Load<Sprite>("Environment/BrickRow/Businesses/ConvenienceStore/store_counter");
            Sprite fridge = Resources.Load<Sprite>("Environment/BrickRow/Businesses/ConvenienceStore/store_fridge");
            Sprite clerk = Resources.Load<Sprite>("Environment/BrickRow/Businesses/ConvenienceStore/store_clerk");
            Sprite stockDoor = Resources.Load<Sprite>("Environment/BrickRow/Businesses/ConvenienceStore/store_stockdoor");

            CreateTiled(_interiorRoot.transform, "Floor", floor, Vector3.zero, new Vector2(12f, 8f), -20000, false);
            CreateTiled(_interiorRoot.transform, "WallTop", wall, new Vector3(0f, 4f, 0f), new Vector2(12f, 0.5f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallLeft", wall, new Vector3(-6f, 0f, 0f), new Vector2(0.4f, 8f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallRight", wall, new Vector3(6f, 0f, 0f), new Vector2(0.4f, 8f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallBottomLeft", wall, new Vector3(-3.5f, -4f, 0f), new Vector2(5f, 0.5f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallBottomRight", wall, new Vector3(3.5f, -4f, 0f), new Vector2(5f, 0.5f), -19990, true);

            // Checkout counter and clerk.
            CreateSprite(_interiorRoot.transform, "Counter", counter, new Vector3(3.4f, 1.7f, 0f),
                new Vector2(3.4f, 1.5f), true, -950, 3f, 0.75f);
            CreateSprite(_interiorRoot.transform, "Clerk", clerk, new Vector3(3.4f, 2.8f, 0f),
                new Vector2(0.85f, 1.25f), false, -951);

            // Sales shelves and drinks fridge.
            CreateSprite(_interiorRoot.transform, "ShelfA", shelf, new Vector3(-3.3f, 1.3f, 0f),
                new Vector2(4.2f, 1.25f), true, -950);
            CreateSprite(_interiorRoot.transform, "ShelfB", shelf, new Vector3(-3.3f, -0.6f, 0f),
                new Vector2(4.2f, 1.25f), true, -950);
            CreateSprite(_interiorRoot.transform, "Fridge", fridge, new Vector3(-5.4f, 2.3f, 0f),
                new Vector2(1.25f, 2f), true, -950);

            // Stockroom passage embedded in the back wall.
            CreateBlock(_interiorRoot.transform, "StockroomPassage", new Vector3(4.7f, 3.7f, 0f),
                new Vector2(1.1f, 0.52f), new Color(0.34f, 0.22f, 0.15f), false, -948);

            CreateBlock(_interiorRoot.transform, "Entrance", new Vector3(0f, -4.48f, 0f),
                new Vector2(2f, 0.52f), new Color(0.08f, 0.10f, 0.11f), true, -947);
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
