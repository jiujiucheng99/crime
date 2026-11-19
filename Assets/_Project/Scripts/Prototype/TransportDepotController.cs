using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Large bus depot behind the transport centre employee passage.
    /// Scene only: parked buses, no driving or work interaction yet.
    /// </summary>
    public class TransportDepotController : MonoBehaviour
    {
        private const float ExitRange = 1.2f;
        private static readonly Vector3 InteriorOffset = new Vector3(12000f, 0f, 0f);
        private static readonly Vector3 InteriorSpawn = new Vector3(12000f, -5.3f, 0f);
        private static readonly Vector2 InteriorExitPoint = new Vector2(12000f, -6.55f);
        private static readonly Vector2 ReturnToLobby = new Vector2(1004.3f, 3.6f);

        private Transform _player;
        private DialogueChoiceController _dialogue;
        private PlayerEmployment _employment;
        private TransportCenterController _transportCenter;
        private GameObject _lobbyRoot;
        private GameObject _depotRoot;
        private bool _inside;
        private bool _skipExitFrame;
        private string _prompt;

        public bool IsInside => _inside;

        public static TransportDepotController Active { get; private set; }

        public void Configure(Transform player, DialogueChoiceController dialogue,
            PlayerEmployment employment, TransportCenterController transportCenter)
        {
            _player = player;
            _dialogue = dialogue;
            _employment = employment;
            _transportCenter = transportCenter;
        }

        public void Enter(GameObject lobbyRoot)
        {
            if (_inside) return;
            if (_employment == null || _employment.CurrentJobId != "transport_staff")
            {
                if (_dialogue != null) _dialogue.OpenNotice("员工通道", "员工专用，请先入职运输中心。");
                return;
            }

            _lobbyRoot = lobbyRoot;
            if (_lobbyRoot != null) _lobbyRoot.SetActive(false);
            _inside = true;
            _skipExitFrame = true;
            Active = this;
            if (_depotRoot == null) BuildInterior();
            _depotRoot.SetActive(true);

            Rigidbody2D body = _player.GetComponent<Rigidbody2D>();
            if (body != null) body.velocity = Vector2.zero;
            _player.position = InteriorSpawn;
        }

        public void Exit()
        {
            _inside = false;
            if (Active == this) Active = null;
            if (_depotRoot != null) _depotRoot.SetActive(false);
            if (_lobbyRoot != null) _lobbyRoot.SetActive(true);
            if (_player == null) return;

            Rigidbody2D body = _player.GetComponent<Rigidbody2D>();
            if (body != null) body.velocity = Vector2.zero;
            _player.position = ReturnToLobby;
        }

        public void BeginDriveFromBus()
        {
            if (!_inside) return;
            _inside = false;
            if (Active == this) Active = null;
            if (_depotRoot != null) _depotRoot.SetActive(false);
            if (_transportCenter != null) _transportCenter.StartBusDrive();
        }

        private void Update()
        {
            if (!_inside || _player == null) return;
            if (DialogueChoiceController.IsOpen || StorageInteraction.AnyPanelOpen) return;
            if (_skipExitFrame)
            {
                _skipExitFrame = false;
                return;
            }

            bool nearExit = Vector2.Distance(_player.position, InteriorExitPoint) <= ExitRange;
            _prompt = nearExit ? "回到大厅" : string.Empty;
            if (!InteractionInput.TryConsumeF(!string.IsNullOrEmpty(_prompt))) return;
            if (nearExit) Exit();
        }

        private void OnGUI()
        {
            if (!_inside) return;
            InteractionPromptUI.SetPrompt("depot", _prompt);
        }

        private void BuildInterior()
        {
            _depotRoot = new GameObject("TransportBusDepot");
            _depotRoot.transform.position = InteriorOffset;

            Sprite floor = Resources.Load<Sprite>("Environment/BrickRow/Businesses/AutoRepair/garage_floor");
            Sprite wall = Resources.Load<Sprite>("Environment/BrickRow/Businesses/AutoRepair/garage_wall");

            CreateTiled(_depotRoot.transform, "Floor", floor, Vector3.zero, new Vector2(20f, 14f), -20000, false);
            CreateTiled(_depotRoot.transform, "WallTop", wall, new Vector3(0f, 7f, 0f), new Vector2(20f, 0.6f), -19990, true);
            CreateTiled(_depotRoot.transform, "WallLeft", wall, new Vector3(-10f, 0f, 0f), new Vector2(0.5f, 14f), -19990, true);
            CreateTiled(_depotRoot.transform, "WallRight", wall, new Vector3(10f, 0f, 0f), new Vector2(0.5f, 14f), -19990, true);
            CreateTiled(_depotRoot.transform, "WallBottomLeft", wall, new Vector3(-5.5f, -7f, 0f), new Vector2(9f, 0.5f), -19990, true);
            CreateTiled(_depotRoot.transform, "WallBottomRight", wall, new Vector3(5.5f, -7f, 0f), new Vector2(9f, 0.5f), -19990, true);

            // Painted bus bays.
            for (int i = 0; i < 3; i++)
            {
                float x = -6f + i * 6f;
                CreateBlock(_depotRoot.transform, "BayLine" + i, new Vector3(x, -5.9f, 0f),
                    new Vector2(3.2f, 0.06f), new Color(0.78f, 0.66f, 0.28f), false, -19800);
                CreateBlock(_depotRoot.transform, "BayLineTop" + i, new Vector3(x, 5.9f, 0f),
                    new Vector2(3.2f, 0.06f), new Color(0.78f, 0.66f, 0.28f), false, -19800);
            }

            // Three south-facing buses in two rows.
            float[] xs = { -6f, 0f, 6f };
            for (int i = 0; i < xs.Length; i++)
            {
                CreateBus(_depotRoot.transform, "BusSouthTop" + i, "bus_south", new Vector3(xs[i], 3f, 0f), -950);
                CreateBus(_depotRoot.transform, "BusSouthBottom" + i, "bus_south", new Vector3(xs[i], -3f, 0f), -950);
            }

            // One sideways bus in the far right bay for visual variety.
            CreateBus(_depotRoot.transform, "BusEast", "bus_east", new Vector3(8.3f, 5.5f, 0f), -949, 4.1f, 2.35f);

            // Small staff office inset at the back wall.
            CreateBlock(_depotRoot.transform, "OfficePassage", new Vector3(-8.6f, 6.98f, 0f),
                new Vector2(1.2f, 0.52f), new Color(0.16f, 0.38f, 0.40f), false, -948);

            CreateBlock(_depotRoot.transform, "Entrance", new Vector3(0f, -6.98f, 0f),
                new Vector2(2.4f, 0.52f), new Color(0.05f, 0.06f, 0.07f), true, -947);
        }

        private void CreateBus(Transform parent, string name, string resource, Vector3 localPosition,
            int sorting, float colliderWidth = 2.35f, float colliderHeight = 4.1f)
        {
            Sprite sprite = Resources.Load<Sprite>("Environment/BrickRow/Transport/Vehicles/" + resource);
            if (sprite == null) return;

            GameObject bus = new GameObject(name);
            bus.transform.SetParent(parent, false);
            bus.transform.localPosition = localPosition;
            SpriteRenderer renderer = bus.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = Mathf.RoundToInt(-localPosition.y * 100f);

            BoxCollider2D box = bus.AddComponent<BoxCollider2D>();
            box.size = new Vector2(colliderWidth, colliderHeight);
            box.offset = new Vector2(0f, -0.05f);

            DepotBusInteractable interact = bus.AddComponent<DepotBusInteractable>();
            interact.Configure(_player, _dialogue, _employment, this);
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
