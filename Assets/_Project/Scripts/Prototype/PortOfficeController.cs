using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Port office at H8. The foreman's only function is hiring for the G8 warehouse.
    /// </summary>
    public class PortOfficeController : MonoBehaviour
    {
        private const float EnterRange = 1.3f;
        private const float ExitRange = 1.1f;
        private const float ForemanRange = 1.3f;

        private static readonly Vector3 InteriorOffset = new Vector3(10000f, 0f, 0f);
        private static readonly Vector3 InteriorSpawn = new Vector3(10000f, -2.6f, 0f);
        private static readonly Vector2 InteriorExitPoint = new Vector2(10000f, -3.2f);
        private static readonly Vector2 ForemanPoint = new Vector2(10000f, 0.65f);

        private Transform _worldRoot;
        private Transform _player;
        private DialogueChoiceController _dialogue;
        private JobOfferComponent _jobOffer;
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

            _jobOffer = JobOfferComponent.Create(transform, "PortWarehouseJobOffer",
                player != null ? player.GetComponent<PlayerEmployment>() : null, dialogue,
                "warehouse_worker", "仓库工人", "南岸仓库 G8", "logistics");
        }

        private void Update()
        {
            if (_player == null || StorageInteraction.AnyPanelOpen ||
                BusTravelController.IsOpen || DialogueChoiceController.IsOpen ||
                StoreShopUI.IsOpen) return;

            bool nearEntrance = !_inside && Vector2.Distance(_player.position, _exteriorDoor) <= EnterRange;
            bool nearExit = _inside && Vector2.Distance(_player.position, InteriorExitPoint) <= ExitRange;
            bool nearForeman = _inside && Vector2.Distance(_player.position, ForemanPoint) <= ForemanRange;
            _prompt = !_inside
                ? (nearEntrance ? "港口办公室" : string.Empty)
                : (nearForeman ? "码头主管" : (nearExit ? "街道" : string.Empty));

            if (!InteractionInput.TryConsumeF(!string.IsNullOrEmpty(_prompt))) return;
            if (!_inside && nearEntrance) Enter();
            else if (_inside && nearForeman && _jobOffer != null) _jobOffer.Offer();
            else if (_inside && nearExit) Exit();
        }

        private void OnGUI()
        {
            InteractionPromptUI.SetPrompt("portoffice", _prompt);
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
            _interiorRoot = new GameObject("PortOfficeInterior");
            _interiorRoot.transform.position = InteriorOffset;

            Sprite floor = Resources.Load<Sprite>("Environment/BrickRow/Businesses/PortOffice/port_office_floor");
            Sprite wall = Resources.Load<Sprite>("Environment/BrickRow/Businesses/PortOffice/port_office_wall");
            Sprite window = Resources.Load<Sprite>("Environment/BrickRow/Businesses/PortOffice/port_office_window");
            Sprite desk = Resources.Load<Sprite>("Environment/BrickRow/Businesses/PortOffice/port_office_desk");
            Sprite cabinet = Resources.Load<Sprite>("Environment/BrickRow/Businesses/PortOffice/port_office_cabinet");
            Sprite map = Resources.Load<Sprite>("Environment/BrickRow/Businesses/PortOffice/port_office_map");
            Sprite crate = Resources.Load<Sprite>("Environment/BrickRow/Businesses/PortOffice/port_office_crate");
            Sprite foreman = Resources.Load<Sprite>("Environment/BrickRow/Businesses/PortOffice/port_foreman");

            CreateTiled(_interiorRoot.transform, "Floor", floor, Vector3.zero, new Vector2(10f, 7f), -20000, false);
            CreateTiled(_interiorRoot.transform, "WallTop", wall, new Vector3(0f, 3.5f, 0f), new Vector2(10f, 0.5f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallLeft", wall, new Vector3(-5f, 0f, 0f), new Vector2(0.4f, 7f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallRight", wall, new Vector3(5f, 0f, 0f), new Vector2(0.4f, 7f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallBottomLeft", wall, new Vector3(-2.75f, -3.5f, 0f), new Vector2(4.5f, 0.5f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallBottomRight", wall, new Vector3(2.75f, -3.5f, 0f), new Vector2(4.5f, 0.5f), -19990, true);

            // Sea-facing window on the back wall.
            CreateSprite(_interiorRoot.transform, "SeaWindow", window, new Vector3(0f, 3.25f, 0f),
                new Vector2(3.2f, 1.25f), false, -960);

            // Foreman desk and foreman.
            CreateSprite(_interiorRoot.transform, "ForemanDesk", desk, new Vector3(0f, 1.8f, 0f),
                new Vector2(4f, 1.3f), true, -950, 3.8f, 0.8f);
            CreateSprite(_interiorRoot.transform, "Foreman", foreman, new Vector3(0f, 3.0f, 0f),
                new Vector2(0.9f, 1.35f), false, -951);

            // Office furniture.
            CreateSprite(_interiorRoot.transform, "CabinetLeft", cabinet, new Vector3(-4.3f, 2.2f, 0f),
                new Vector2(0.9f, 1.4f), true, -950, 0.8f, 1f);
            CreateSprite(_interiorRoot.transform, "CabinetRight", cabinet, new Vector3(4.3f, 2.2f, 0f),
                new Vector2(0.9f, 1.4f), true, -950, 0.8f, 1f);
            CreateSprite(_interiorRoot.transform, "MapBoard", map, new Vector3(-2.6f, 2.9f, 0f),
                new Vector2(2.4f, 1.2f), false, -949);
            CreateSprite(_interiorRoot.transform, "CrateA", crate, new Vector3(-4.0f, -1.8f, 0f),
                new Vector2(1.2f, 1.2f), true, -950, 1f, 0.9f);
            CreateSprite(_interiorRoot.transform, "CrateB", crate, new Vector3(-2.8f, -1.8f, 0f),
                new Vector2(1.2f, 1.2f), true, -950, 1f, 0.9f);

            CreateBlock(_interiorRoot.transform, "Entrance", new Vector3(0f, -3.48f, 0f),
                new Vector2(1.6f, 0.52f), new Color(0.06f, 0.08f, 0.09f), true, -947);
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
