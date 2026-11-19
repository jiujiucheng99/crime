using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Auto repair interior at H6. Scene only: no repair, parts or vehicle interaction.
    /// </summary>
    public class AutoRepairController : MonoBehaviour
    {
        private const float EnterRange = 1.3f;
        private const float ExitRange = 1.1f;
        private static readonly Vector3 InteriorOffset = new Vector3(7000f, 0f, 0f);
        private static readonly Vector3 InteriorSpawn = new Vector3(7000f, -3.1f, 0f);
        private static readonly Vector2 InteriorExitPoint = new Vector2(7000f, -4.1f);
        private static readonly Vector2 MechanicPoint = new Vector2(6995.8f, -1.2f);

        private Transform _worldRoot;
        private Transform _player;
        private DialogueChoiceController _dialogue;
        private JobOfferComponent _jobOffer;
        private AutoRepairJobController _job;
        private PlayerEmployment _employment;
        private PlayerWallet _wallet;
        private PlayerInventory _inventory;
        private PlayerSkills _skills;
        private bool _vehicleRepaired;
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
            _dialogue = dialogue;
            _employment = player != null ? player.GetComponent<PlayerEmployment>() : null;
            _wallet = player != null ? player.GetComponent<PlayerWallet>() : null;
            _inventory = player != null ? player.GetComponent<PlayerInventory>() : null;
            _skills = player != null ? player.GetComponent<PlayerSkills>() : null;

            _jobOffer = JobOfferComponent.Create(transform, "AutoRepairJobOffer",
                _employment, dialogue,
                "auto_mechanic", "修车厂技工", "修车厂 H6", "mechanics");

            GameObject jobObject = new GameObject("AutoRepairJobController");
            jobObject.transform.SetParent(transform, false);
            _job = jobObject.AddComponent<AutoRepairJobController>();
            _job.Configure(player, _employment, _wallet, _inventory, _skills, dialogue);
        }

        private void Update()
        {
            if (_player == null || StorageInteraction.AnyPanelOpen ||
                BusTravelController.IsOpen || DialogueChoiceController.IsOpen ||
                StoreShopUI.IsOpen || RepairMinigameUI.IsOpen) return;

            bool nearEntrance = !_inside && Vector2.Distance(_player.position, _exteriorDoor) <= EnterRange;
            bool nearExit = _inside && Vector2.Distance(_player.position, InteriorExitPoint) <= ExitRange;
            bool nearMechanic = _inside && Vector2.Distance(_player.position, MechanicPoint) <= 1.25f;

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
                ? (nearEntrance ? "修车厂" : string.Empty)
                : (nearMechanic ? "维修工" : (nearExit ? "街道" : string.Empty));

            if (!InteractionInput.TryConsumeF(!string.IsNullOrEmpty(_prompt))) return;
            if (!_inside && nearEntrance) Enter();
            else if (_inside && nearMechanic) OpenMechanicDialogue();
            else if (_inside && nearExit) Exit();
        }

        private void OpenMechanicDialogue()
        {
            if (_dialogue == null) return;
            _dialogue.OpenChoice("维修工", "车有毛病，还是想找份活干？",
                "求职", () =>
                {
                    if (_jobOffer != null) _jobOffer.Offer();
                },
                "修理", RepairVehicle);
        }

        private void RepairVehicle()
        {
            if (_employment == null || _employment.CurrentJobId != "transport_staff")
            {
                if (_dialogue != null) _dialogue.OpenNotice("维修工", "您似乎并没有车。");
                return;
            }

            if (_wallet == null || !_wallet.TrySpend(10))
            {
                if (_dialogue != null) _dialogue.OpenNotice("维修工", "修理需要 10 块。");
                return;
            }

            _vehicleRepaired = true;
            if (_dialogue != null) _dialogue.OpenNotice("维修工", "修理完成，花了 10 块。");
        }

        private void OnGUI()
        {
            InteractionPromptUI.SetPrompt("autorepair", _prompt);
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
            _interiorRoot = new GameObject("AutoRepairInterior");
            _interiorRoot.transform.position = InteriorOffset;

            Sprite floor = Resources.Load<Sprite>("Environment/BrickRow/Businesses/AutoRepair/garage_floor");
            Sprite wall = Resources.Load<Sprite>("Environment/BrickRow/Businesses/AutoRepair/garage_wall");
            Sprite lift = Resources.Load<Sprite>("Environment/BrickRow/Businesses/AutoRepair/garage_lift");
            Sprite car = Resources.Load<Sprite>("Environment/BrickRow/Businesses/AutoRepair/garage_car");
            Sprite bench = Resources.Load<Sprite>("Environment/BrickRow/Businesses/AutoRepair/garage_bench");
            Sprite tires = Resources.Load<Sprite>("Environment/BrickRow/Businesses/AutoRepair/garage_tires");
            Sprite shelf = Resources.Load<Sprite>("Environment/BrickRow/Businesses/AutoRepair/garage_shelf");
            Sprite mechanic = Resources.Load<Sprite>("Environment/BrickRow/Businesses/AutoRepair/garage_mechanic");

            CreateTiled(_interiorRoot.transform, "Floor", floor, Vector3.zero, new Vector2(12f, 9f), -20000, false);
            CreateTiled(_interiorRoot.transform, "WallTop", wall, new Vector3(0f, 4.5f, 0f), new Vector2(12f, 0.5f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallLeft", wall, new Vector3(-6f, 0f, 0f), new Vector2(0.4f, 9f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallRight", wall, new Vector3(6f, 0f, 0f), new Vector2(0.4f, 9f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallBottomLeft", wall, new Vector3(-3.5f, -4.5f, 0f), new Vector2(5f, 0.5f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallBottomRight", wall, new Vector3(3.5f, -4.5f, 0f), new Vector2(5f, 0.5f), -19990, true);

            // Central lift with a car on it.
            CreateSprite(_interiorRoot.transform, "CarLift", lift, new Vector3(0f, 0.9f, 0f),
                new Vector2(6f, 4f), true, -960, 5.8f, 3.6f);
            CreateSprite(_interiorRoot.transform, "LiftedCar", car, new Vector3(0f, 0.9f, 0f),
                new Vector2(4.6f, 2.3f), false, -955);

            // Workbench left, parts shelf right, tire stack lower right.
            CreateSprite(_interiorRoot.transform, "Workbench", bench, new Vector3(-4.5f, 3.4f, 0f),
                new Vector2(3.4f, 1.3f), true, -950, 3.2f, 0.8f);
            WorkAreaMarker.Create(_interiorRoot.transform, "WorkMarker_Bench",
                new Vector3(-4.5f, 4.0f, 0f), new Color(0.95f, 0.78f, 0.22f));
            GameObject partsShelf = CreateSprite(_interiorRoot.transform, "PartsShelf", shelf, new Vector3(4.5f, 3.4f, 0f),
                new Vector2(3.2f, 1.5f), true, -950, 3f, 0.9f);
            WorkAreaMarker.Create(_interiorRoot.transform, "WorkMarker_Parts",
                new Vector3(4.5f, 4.0f, 0f), new Color(0.35f, 0.72f, 0.92f));
            WorkAreaMarker.Create(_interiorRoot.transform, "WorkMarker_Car",
                new Vector3(0f, 2.6f, 0f), new Color(0.86f, 0.44f, 0.34f));
            if (partsShelf != null)
            {
                StorageContainer storage = partsShelf.AddComponent<StorageContainer>();
                storage.Configure("PARTS", 8);
                AutoRestockContainer restock = partsShelf.AddComponent<AutoRestockContainer>();
                restock.Configure(RepairParts.All, 5, "PARTS", 8);
                restock.EnsureStock();
            }
            CreateSprite(_interiorRoot.transform, "TireStack", tires, new Vector3(4.7f, -2.7f, 0f),
                new Vector2(1.3f, 1.3f), true, -950, 1.1f, 0.9f);

            CreateSprite(_interiorRoot.transform, "Mechanic", mechanic, new Vector3(-4.2f, -1.2f, 0f),
                new Vector2(0.85f, 1.3f), false, Mathf.RoundToInt(1.2f * 100f));

            // Staff/office passage embedded in the back wall.
            CreateBlock(_interiorRoot.transform, "OfficePassage", new Vector3(-5.1f, 4.48f, 0f),
                new Vector2(1.2f, 0.52f), new Color(0.16f, 0.38f, 0.40f), false, -948);

            CreateBlock(_interiorRoot.transform, "Entrance", new Vector3(0f, -4.48f, 0f),
                new Vector2(2f, 0.52f), new Color(0.08f, 0.10f, 0.11f), true, -947);
        }

        private static GameObject CreateSprite(Transform parent, string name, Sprite sprite, Vector3 localPosition,
            Vector2 size, bool collider, int sortingOrder, float colliderWidth = 0f, float colliderHeight = 0f)
        {
            if (sprite == null) return null;
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

            return go;
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
