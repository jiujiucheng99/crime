using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Warehouse interior at G8. Scene only: no loot, storage or vehicle interaction.
    /// </summary>
    public class WarehouseController : MonoBehaviour
    {
        private const float EnterRange = 1.3f;
        private const float ExitRange = 1.1f;
        private static readonly Vector3 InteriorOffset = new Vector3(9000f, 0f, 0f);
        private static readonly Vector3 InteriorSpawn = new Vector3(9000f, -3.8f, 0f);
        private static readonly Vector2 InteriorExitPoint = new Vector2(9000f, -4.5f);

        private Transform _worldRoot;
        private Transform _player;
        private WarehouseJobController _job;
        private PlayerEmployment _employment;
        private PlayerWallet _wallet;
        private PlayerSkills _skills;
        private Vector2 _exteriorDoor;
        private GameObject _interiorRoot;
        private bool _inside;
        private string _prompt;

        public bool IsInside => _inside;
        public bool IsShiftActive => _job != null && _job.IsShiftActive;

        public void Configure(Transform worldRoot, Transform player, Vector2 exteriorDoor,
            DialogueChoiceController dialogue = null)
        {
            _worldRoot = worldRoot;
            _player = player;
            _exteriorDoor = exteriorDoor;
            _employment = player != null ? player.GetComponent<PlayerEmployment>() : null;
            _wallet = player != null ? player.GetComponent<PlayerWallet>() : null;
            _skills = player != null ? player.GetComponent<PlayerSkills>() : null;

            GameObject jobObject = new GameObject("WarehouseJobController");
            jobObject.transform.SetParent(transform, false);
            _job = jobObject.AddComponent<WarehouseJobController>();
            _job.Configure(player, _employment, _wallet, _skills, dialogue);
        }

        private void Update()
        {
            if (_player == null || StorageInteraction.AnyPanelOpen ||
                BusTravelController.IsOpen || DialogueChoiceController.IsOpen ||
                StoreShopUI.IsOpen) return;

            bool nearEntrance = !_inside && Vector2.Distance(_player.position, _exteriorDoor) <= EnterRange;
            bool nearExit = _inside && Vector2.Distance(_player.position, InteriorExitPoint) <= ExitRange;

            string jobPrompt;
            if (_inside && _job != null && _job.TryGetPrompt(out jobPrompt))
            {
                _prompt = jobPrompt;
                if (InteractionInput.TryConsumeF(true))
                {
                    _job.Interact();
                    return;
                }
            }

            _prompt = !_inside
                ? (nearEntrance ? "仓库" : string.Empty)
                : (nearExit ? "街道" : string.Empty);

            if (!InteractionInput.TryConsumeF(!string.IsNullOrEmpty(_prompt))) return;
            if (!_inside && nearEntrance) Enter();
            else if (_inside && nearExit) Exit();
        }

        private void OnGUI()
        {
            InteractionPromptUI.SetPrompt("warehouse", _prompt);
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
            if (_job != null) _job.CancelShift();
            if (_interiorRoot != null) _interiorRoot.SetActive(false);
            if (_worldRoot != null) _worldRoot.gameObject.SetActive(true);

            Rigidbody2D body = _player.GetComponent<Rigidbody2D>();
            if (body != null) body.velocity = Vector2.zero;
            _player.position = new Vector3(_exteriorDoor.x, _exteriorDoor.y - 0.8f, 0f);
        }

        private void BuildInterior()
        {
            _interiorRoot = new GameObject("WarehouseInterior");
            _interiorRoot.transform.position = InteriorOffset;

            Sprite floor = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Warehouse/warehouse_floor");
            Sprite wall = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Warehouse/warehouse_wall");
            Sprite crate = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Warehouse/warehouse_crate");
            Sprite pallet = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Warehouse/warehouse_pallet");
            Sprite shelf = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Warehouse/warehouse_shelf");
            Sprite forklift = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Warehouse/warehouse_forklift");
            Sprite loading = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Warehouse/warehouse_loadingdoor");
            Sprite desk = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Warehouse/warehouse_desk");

            CreateTiled(_interiorRoot.transform, "Floor", floor, Vector3.zero, new Vector2(14f, 10f), -20000, false);
            CreateTiled(_interiorRoot.transform, "WallTop", wall, new Vector3(0f, 5f, 0f), new Vector2(14f, 0.5f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallLeft", wall, new Vector3(-7f, 0f, 0f), new Vector2(0.4f, 10f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallRight", wall, new Vector3(7f, 0f, 0f), new Vector2(0.4f, 10f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallBottomLeft", wall, new Vector3(-4f, -5f, 0f), new Vector2(6f, 0.5f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallBottomRight", wall, new Vector3(4f, -5f, 0f), new Vector2(6f, 0.5f), -19990, true);

            // Loading door embedded in the back wall.
            CreateSprite(_interiorRoot.transform, "LoadingDoor", loading, new Vector3(0f, 4.05f, 0f),
                new Vector2(4f, 2.6f), false, -948);

            // Shelving and stacks.
            CreateSprite(_interiorRoot.transform, "ShelfLeft", shelf, new Vector3(-5.4f, 3.6f, 0f),
                new Vector2(3.2f, 1.5f), true, -950, 3f, 0.9f);
            CreateSprite(_interiorRoot.transform, "ShelfRight", shelf, new Vector3(5.4f, 3.6f, 0f),
                new Vector2(3.2f, 1.5f), true, -950, 3f, 0.9f);

            CreateSprite(_interiorRoot.transform, "CrateStackA", crate, new Vector3(-5.4f, 1.5f, 0f),
                new Vector2(1.4f, 1.4f), true, -950, 1.2f, 1f);
            CreateSprite(_interiorRoot.transform, "CrateStackB", crate, new Vector3(-4f, 1.5f, 0f),
                new Vector2(1.4f, 1.4f), true, -950, 1.2f, 1f);
            CreateSprite(_interiorRoot.transform, "CrateStackC", crate, new Vector3(-4.7f, 2.8f, 0f),
                new Vector2(1.4f, 1.4f), true, -950, 1.2f, 1f);
            CreateSprite(_interiorRoot.transform, "PalletA", pallet, new Vector3(4.2f, 1.1f, 0f),
                new Vector2(1.5f, 1.5f), true, -950, 1.3f, 1f);
            CreateSprite(_interiorRoot.transform, "PalletB", pallet, new Vector3(5.7f, 1.1f, 0f),
                new Vector2(1.5f, 1.5f), true, -950, 1.3f, 1f);

            CreateSprite(_interiorRoot.transform, "Forklift", forklift, new Vector3(-4.8f, -2.9f, 0f),
                new Vector2(2.8f, 2.1f), true, -950, 2.6f, 1.5f);

            // Small office desk near the back-right corner.
            CreateSprite(_interiorRoot.transform, "OfficeDesk", desk, new Vector3(5.4f, -1.1f, 0f),
                new Vector2(2.8f, 1.3f), true, -949, 2.6f, 0.8f);
            WorkAreaMarker.Create(_interiorRoot.transform, "WorkMarker_Desk",
                new Vector3(5.4f, 0.35f, 0f), new Color(0.95f, 0.78f, 0.22f));
            CreateBlock(_interiorRoot.transform, "OfficePassage", new Vector3(5.1f, 4.48f, 0f),
                new Vector2(1.2f, 0.52f), new Color(0.16f, 0.38f, 0.40f), false, -948);

            CreateBlock(_interiorRoot.transform, "Entrance", new Vector3(0f, -4.98f, 0f),
                new Vector2(2f, 0.52f), new Color(0.06f, 0.06f, 0.06f), true, -947);
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
