using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Restaurant interior at E8. Scene only: no ordering, no hunger, no jobs.
    /// </summary>
    public class RestaurantController : MonoBehaviour
    {
        private const float EnterRange = 1.3f;
        private const float ExitRange = 1.1f;
        private static readonly Vector3 InteriorOffset = new Vector3(6000f, 0f, 0f);
        private static readonly Vector3 InteriorSpawn = new Vector3(6000f, -3.1f, 0f);
        private static readonly Vector2 InteriorExitPoint = new Vector2(6000f, -4.1f);
        private static readonly Vector2 WaiterPoint = new Vector2(6001.9f, 3.1f);
        private static readonly Vector2 KitchenPassagePoint = new Vector2(6000f, 3.7f);

        private Transform _worldRoot;
        private Transform _player;
        private DialogueChoiceController _dialogue;
        private JobOfferComponent _jobOffer;
        private PlayerEmployment _employment;
        private RestaurantKitchenController _kitchen;
        private Vector2 _exteriorDoor;
        private GameObject _interiorRoot;
        private bool _inside;
        private string _prompt;

        public bool IsInside => _inside;

        public void Configure(Transform worldRoot, Transform player, Vector2 exteriorDoor,
            DialogueChoiceController dialogue = null)
        {
            _worldRoot = worldRoot;
            _player = player;
            _exteriorDoor = exteriorDoor;
            _dialogue = dialogue;

            _jobOffer = JobOfferComponent.Create(transform, "RestaurantJobOffer",
                player != null ? player.GetComponent<PlayerEmployment>() : null, dialogue,
                "restaurant_waiter", "餐厅服务员", "餐厅 E8", "cooking");

            _employment = player != null ? player.GetComponent<PlayerEmployment>() : null;
            GameObject kitchenObject = new GameObject("RestaurantKitchenController");
            kitchenObject.transform.SetParent(transform, false);
            _kitchen = kitchenObject.AddComponent<RestaurantKitchenController>();
            _kitchen.Configure(player, dialogue, _employment);
        }

        private void Update()
        {
            if (_player == null || StorageInteraction.AnyPanelOpen ||
                BusTravelController.IsOpen || DialogueChoiceController.IsOpen ||
                StoreShopUI.IsOpen || (_kitchen != null && _kitchen.IsInside)) return;

            bool nearEntrance = !_inside && Vector2.Distance(_player.position, _exteriorDoor) <= EnterRange;
            bool nearExit = _inside && Vector2.Distance(_player.position, InteriorExitPoint) <= ExitRange;
            bool nearWaiter = _inside && Vector2.Distance(_player.position, WaiterPoint) <= 1.25f;
            bool nearKitchen = _inside && Vector2.Distance(_player.position, KitchenPassagePoint) <= 1.4f;
            _prompt = !_inside
                ? (nearEntrance ? "餐厅" : string.Empty)
                : (nearKitchen ? "厨房" : (nearWaiter ? "服务员" : (nearExit ? "街道" : string.Empty)));

            if (!InteractionInput.TryConsumeF(!string.IsNullOrEmpty(_prompt))) return;
            if (!_inside && nearEntrance) Enter();
            else if (_inside && nearKitchen)
            {
                if (_employment != null && _employment.CurrentJobId == "restaurant_waiter") _kitchen.Enter(_interiorRoot);
                else if (_dialogue != null) _dialogue.OpenNotice("员工通道", "员工专用，请先入职餐厅。");
            }
            else if (_inside && nearWaiter && _jobOffer != null) _jobOffer.Offer();
            else if (_inside && nearExit) Exit();
        }

        private void OnGUI()
        {
            InteractionPromptUI.SetPrompt("restaurant", _prompt);
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
            _interiorRoot = new GameObject("RestaurantInterior");
            _interiorRoot.transform.position = InteriorOffset;

            Sprite floor = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Restaurant/restaurant_floor");
            Sprite wall = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Restaurant/restaurant_wall");
            Sprite table = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Restaurant/restaurant_table");
            Sprite booth = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Restaurant/restaurant_booth");
            Sprite counter = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Restaurant/restaurant_counter");
            Sprite waiter = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Restaurant/restaurant_waiter");

            CreateTiled(_interiorRoot.transform, "Floor", floor, Vector3.zero, new Vector2(12f, 9f), -20000, false);
            CreateTiled(_interiorRoot.transform, "WallTop", wall, new Vector3(0f, 4.5f, 0f), new Vector2(12f, 0.5f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallLeft", wall, new Vector3(-6f, 0f, 0f), new Vector2(0.4f, 9f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallRight", wall, new Vector3(6f, 0f, 0f), new Vector2(0.4f, 9f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallBottomLeft", wall, new Vector3(-3.5f, -4.5f, 0f), new Vector2(5f, 0.5f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallBottomRight", wall, new Vector3(3.5f, -4.5f, 0f), new Vector2(5f, 0.5f), -19990, true);

            // Dining tables: collide with the tabletop only, not the chairs.
            CreateSprite(_interiorRoot.transform, "TableA", table, new Vector3(-3.5f, 2.0f, 0f), new Vector2(3f, 2.25f), true, -950, 2.1f, 0.9f);
            CreateSprite(_interiorRoot.transform, "TableB", table, new Vector3(0.1f, 2.0f, 0f), new Vector2(3f, 2.25f), true, -950, 2.1f, 0.9f);
            CreateSprite(_interiorRoot.transform, "TableC", table, new Vector3(-3.5f, -1.1f, 0f), new Vector2(3f, 2.25f), true, -950, 2.1f, 0.9f);
            CreateSprite(_interiorRoot.transform, "TableD", table, new Vector3(0.1f, -1.1f, 0f), new Vector2(3f, 2.25f), true, -950, 2.1f, 0.9f);

            // Booths along the right wall.
            CreateSprite(_interiorRoot.transform, "BoothA", booth, new Vector3(4.1f, 1.8f, 0f), new Vector2(3f, 2.1f), true, -950);
            CreateSprite(_interiorRoot.transform, "BoothB", booth, new Vector3(4.1f, -1.4f, 0f), new Vector2(3f, 2.1f), true, -950);

            // Host counter near the entrance and kitchen door at the back.
            CreateSprite(_interiorRoot.transform, "HostCounter", counter, new Vector3(3.8f, -2.7f, 0f), new Vector2(4f, 1.2f), true, -949);
            // Staff passage: a flat brown inset embedded in the back wall.
            CreateBlock(_interiorRoot.transform, "KitchenPassage", new Vector3(0f, 4.48f, 0f),
                new Vector2(1.2f, 0.52f), new Color(0.14f, 0.34f, 0.29f), false, -948);
            CreateSprite(_interiorRoot.transform, "Waiter", waiter, new Vector3(1.9f, 3.1f, 0f), new Vector2(0.85f, 1.3f), false,
                Mathf.RoundToInt(3.1f * 100f));

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
