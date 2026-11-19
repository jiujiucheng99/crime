using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Minimal bus transport centre interior: enter/exit only. The reception
    /// desk, staff member and staff-only door are visual placeholders for
    /// later gameplay.
    /// </summary>
    public class TransportCenterController : MonoBehaviour
    {
        private const float EnterRange = 1.3f;
        private const float ExitRange = 1.1f;
        private static readonly Vector3 InteriorOffset = new Vector3(1000f, 0f, 0f);
        private static readonly Vector3 InteriorSpawn = new Vector3(1000f, -3.4f, 0f);
        private static readonly Vector2 InteriorExitPoint = new Vector2(1000f, -3.9f);
        private static readonly Vector2 InteriorDeskPoint = new Vector2(1000f, 1.1f);
        private static readonly Vector2 EmployeePassagePoint = new Vector2(1004.3f, 3.8f);

        private Transform _worldRoot;
        private Transform _player;
        private BusTravelController _busTravel;
        private DialogueChoiceController _dialogue;
        private JobOfferComponent _jobOffer;
        private PlayerEmployment _employment;
        private TransportDepotController _depot;
        private BusDrivingController _busDrive;
        private BusRouteController _busRoute;
        private PlayerWallet _wallet;
        private Vector2 _exteriorDoor;
        private GameObject _interiorRoot;
        private bool _inside;
        private string _prompt;

        public bool IsInside => _inside;

        public void Configure(Transform worldRoot, Transform player, Vector2 exteriorDoor,
            BusTravelController busTravel = null, DialogueChoiceController dialogue = null)
        {
            _worldRoot = worldRoot;
            _player = player;
            _exteriorDoor = exteriorDoor;
            _busTravel = busTravel;
            _dialogue = dialogue;
            _employment = player != null ? player.GetComponent<PlayerEmployment>() : null;
            _wallet = player != null ? player.GetComponent<PlayerWallet>() : null;

            _jobOffer = JobOfferComponent.Create(transform, "TransportJobOffer",
                player != null ? player.GetComponent<PlayerEmployment>() : null, dialogue,
                "transport_staff", "运输中心职员", "运输中心 F7");

            GameObject depotObject = new GameObject("TransportDepotController");
            depotObject.transform.SetParent(transform, false);
            _depot = depotObject.AddComponent<TransportDepotController>();
            _depot.Configure(player, dialogue, _employment, this);

            GameObject busDriveObject = new GameObject("BusDrivingController");
            busDriveObject.transform.SetParent(transform, false);
            _busDrive = busDriveObject.AddComponent<BusDrivingController>();
            _busDrive.Configure(player);

            GameObject routeObject = new GameObject("BusRouteController");
            routeObject.transform.SetParent(transform, false);
            _busRoute = routeObject.AddComponent<BusRouteController>();
            _busRoute.Configure(_busDrive, _wallet, dialogue,
                player != null ? player.GetComponent<PlayerSkills>() : null);
            _busDrive.SetRoute(_busRoute);
        }

        private void Update()
        {
            if (_player == null || StorageInteraction.AnyPanelOpen ||
                BusTravelController.IsOpen || DialogueChoiceController.IsOpen ||
                (_depot != null && _depot.IsInside)) return;

            bool nearEntrance = !_inside && Vector2.Distance(_player.position, _exteriorDoor) <= EnterRange;
            bool nearExit = _inside && Vector2.Distance(_player.position, InteriorExitPoint) <= ExitRange;
            bool nearDesk = _inside && Vector2.Distance(_player.position, InteriorDeskPoint) <= 1.25f;
            bool nearPassage = _inside && Vector2.Distance(_player.position, EmployeePassagePoint) <= 1.35f;

            _prompt = !_inside
                ? (nearEntrance ? "运输中心" : string.Empty)
                : (nearPassage ? "员工通道" : (nearDesk ? "前台" : (nearExit ? "街道" : string.Empty)));

            if (!InteractionInput.TryConsumeF(!string.IsNullOrEmpty(_prompt))) return;

            if (!_inside && nearEntrance) Enter();
            else if (_inside && nearPassage)
            {
                if (_employment != null && _employment.CurrentJobId == "transport_staff") _depot.Enter(_interiorRoot);
                else if (_dialogue != null) _dialogue.OpenNotice("员工通道", "员工专用，请先入职运输中心。");
            }
            else if (_inside && nearDesk && _dialogue != null)
            {
                string workLabel = _employment != null && _employment.CurrentJobId == "transport_staff"
                    ? "离职"
                    : "求职";
                _dialogue.OpenChoice("前台", "欢迎。要乘车，还是想在这里找份工作？",
                    "乘车", () =>
                    {
                        if (_busTravel != null) _busTravel.Open("F7", LeaveForTravel);
                    },
                    workLabel, () =>
                    {
                        if (_jobOffer != null) _jobOffer.Offer();
                    });
            }
            else if (_inside && nearExit) Exit();
        }

        public void LeaveForTravel()
        {
            _inside = false;
            if (_interiorRoot != null) _interiorRoot.SetActive(false);
            if (_worldRoot != null) _worldRoot.gameObject.SetActive(true);
        }

        /// <summary>Called after choosing a bus in the depot; starts top-down driving.</summary>
        public void StartBusDrive()
        {
            _inside = false;
            if (_interiorRoot != null) _interiorRoot.SetActive(false);
            if (_worldRoot != null) _worldRoot.gameObject.SetActive(true);

            if (_busDrive != null)
            {
                _busDrive.Begin(new Vector3(_exteriorDoor.x, _exteriorDoor.y - 2.3f, 0f), -90f);
            }
        }

        private void OnGUI()
        {
            InteractionPromptUI.SetPrompt("transport", _depot != null && _depot.IsInside ? string.Empty : _prompt);
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
            _interiorRoot = new GameObject("TransportCenterInterior");
            _interiorRoot.transform.position = InteriorOffset;

            Sprite floor = Resources.Load<Sprite>("Environment/BrickRow/Transport/Interior/station_floor");
            Sprite wall = Resources.Load<Sprite>("Environment/BrickRow/Transport/Interior/station_wall");
            Sprite desk = Resources.Load<Sprite>("Environment/BrickRow/Transport/Interior/reception_desk");
            Sprite staff = Resources.Load<Sprite>("Environment/BrickRow/Transport/Interior/staff");
            Sprite ticket = Resources.Load<Sprite>("Environment/BrickRow/Transport/Interior/ticket_machine");
            Sprite bench = Resources.Load<Sprite>("Environment/BrickRow/Transport/Interior/station_bench");
            Sprite plant = Resources.Load<Sprite>("Environment/BrickRow/Transport/Interior/plant");

            CreateTiled(_interiorRoot.transform, "Floor", floor, Vector3.zero, new Vector2(14f, 9f), -20000, false);
            CreateTiled(_interiorRoot.transform, "WallTop", wall, new Vector3(0f, 4.5f, 0f), new Vector2(14f, 0.5f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallLeft", wall, new Vector3(-7f, 0f, 0f), new Vector2(0.4f, 9f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallRight", wall, new Vector3(7f, 0f, 0f), new Vector2(0.4f, 9f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallBottomLeft", wall, new Vector3(-4f, -4.5f, 0f), new Vector2(6f, 0.5f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallBottomRight", wall, new Vector3(4f, -4.5f, 0f), new Vector2(6f, 0.5f), -19990, true);

            // Reception desk and the staff member behind it. Lower sorting keeps
            // the staff body behind the desk, so only the upper body is visible.
            CreateSprite(_interiorRoot.transform, "ReceptionDesk", desk, new Vector3(0f, 2.1f, 0f), new Vector2(4.2f, 2.1f), true, -950,
                new Vector2(3.8f, 0.95f), new Vector2(0f, -0.3f));
            CreateSprite(_interiorRoot.transform, "Staff", staff, new Vector3(0f, 3.35f, 0f), new Vector2(0.9f, 1.35f), false, -951);

            // Staff-only passage: a small brown inset embedded in the back wall.
            CreateBlock(_interiorRoot.transform, "EmployeePassage", new Vector3(4.3f, 4.48f, 0f),
                new Vector2(1.2f, 0.52f), new Color(0.34f, 0.22f, 0.15f), false, -948);

            // Kiosks and waiting benches.
            CreateSprite(_interiorRoot.transform, "TicketMachineA", ticket, new Vector3(-5.8f, 1.3f, 0f), new Vector2(0.8f, 1.35f), true, -950);
            CreateSprite(_interiorRoot.transform, "TicketMachineB", ticket, new Vector3(-5.8f, -0.5f, 0f), new Vector2(0.8f, 1.35f), true, -950);
            CreateSprite(_interiorRoot.transform, "BenchA", bench, new Vector3(4.8f, -1.5f, 0f), new Vector2(3.2f, 1.2f), true, -950,
                new Vector2(2.6f, 0.5f), new Vector2(0f, -0.2f));
            CreateSprite(_interiorRoot.transform, "BenchB", bench, new Vector3(4.8f, -3.3f, 0f), new Vector2(3.2f, 1.2f), true, -950,
                new Vector2(2.6f, 0.5f), new Vector2(0f, -0.2f));

            CreateSprite(_interiorRoot.transform, "PlantLeft", plant, new Vector3(-6.2f, -3.6f, 0f), new Vector2(0.8f, 1.1f), false, -949);
            CreateSprite(_interiorRoot.transform, "PlantRight", plant, new Vector3(6.2f, 3.8f, 0f), new Vector2(0.8f, 1.1f), false, -949);

            // Entrance/exit opening embedded in the bottom wall, filling the
            // full wall gap between the two bottom wall segments.
            CreateBlock(_interiorRoot.transform, "Entrance", new Vector3(0f, -4.48f, 0f), new Vector2(2f, 0.52f),
                new Color(0.08f, 0.10f, 0.11f), true, -947);
        }

        private static void CreateSprite(Transform parent, string name, Sprite sprite, Vector3 localPosition,
            Vector2 size, bool collider, int sortingOrder, Vector2? colliderSize = null, Vector2? colliderOffset = null)
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
                BoxCollider2D box = go.AddComponent<BoxCollider2D>();
                box.size = colliderSize ?? new Vector2(size.x, size.y * 0.65f);
                box.offset = colliderOffset ?? new Vector2(0f, -size.y * 0.18f);
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
