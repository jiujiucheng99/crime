using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Nightclub interior at H7. Scene only: no drinks, VIP, skills or dialogue yet.
    /// </summary>
    public class NightclubController : MonoBehaviour
    {
        private const float EnterRange = 1.3f;
        private const float ExitRange = 1.1f;
        private static readonly Vector3 InteriorOffset = new Vector3(8000f, 0f, 0f);
        private static readonly Vector3 InteriorSpawn = new Vector3(8000f, -3.8f, 0f);
        private static readonly Vector2 InteriorExitPoint = new Vector2(8000f, -4.5f);

        private Transform _worldRoot;
        private Transform _player;
        private Vector2 _exteriorDoor;
        private GameObject _interiorRoot;
        private bool _inside;
        private string _prompt;

        public bool IsInside => _inside;

        public void Configure(Transform worldRoot, Transform player, Vector2 exteriorDoor)
        {
            _worldRoot = worldRoot;
            _player = player;
            _exteriorDoor = exteriorDoor;
        }

        private void Update()
        {
            if (_player == null || StorageInteraction.AnyPanelOpen ||
                BusTravelController.IsOpen || DialogueChoiceController.IsOpen ||
                StoreShopUI.IsOpen) return;

            bool nearEntrance = !_inside && Vector2.Distance(_player.position, _exteriorDoor) <= EnterRange;
            bool nearExit = _inside && Vector2.Distance(_player.position, InteriorExitPoint) <= ExitRange;
            _prompt = !_inside
                ? (nearEntrance ? "夜总会" : string.Empty)
                : (nearExit ? "街道" : string.Empty);

            if (!InteractionInput.TryConsumeF(!string.IsNullOrEmpty(_prompt))) return;
            if (!_inside && nearEntrance) Enter();
            else if (_inside && nearExit) Exit();
        }

        private void OnGUI()
        {
            InteractionPromptUI.SetPrompt("nightclub", _prompt);
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
            _interiorRoot = new GameObject("NightclubInterior");
            _interiorRoot.transform.position = InteriorOffset;

            Sprite floor = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Nightclub/club_floor");
            Sprite wall = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Nightclub/club_wall");
            Sprite dance = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Nightclub/club_dancefloor");
            Sprite dj = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Nightclub/club_djbooth");
            Sprite bar = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Nightclub/club_bar");
            Sprite sofa = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Nightclub/club_sofa");
            Sprite speaker = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Nightclub/club_speaker");
            Sprite table = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Nightclub/club_table");
            Sprite bartender = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Bar/bar_bartender");

            CreateTiled(_interiorRoot.transform, "Floor", floor, Vector3.zero, new Vector2(14f, 10f), -20000, false);
            CreateTiled(_interiorRoot.transform, "DanceFloor", dance, new Vector3(-0.8f, 0.3f, 0f), new Vector2(7f, 5f), -19980, false);
            CreateTiled(_interiorRoot.transform, "WallTop", wall, new Vector3(0f, 5f, 0f), new Vector2(14f, 0.5f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallLeft", wall, new Vector3(-7f, 0f, 0f), new Vector2(0.4f, 10f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallRight", wall, new Vector3(7f, 0f, 0f), new Vector2(0.4f, 10f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallBottomLeft", wall, new Vector3(-4f, -5f, 0f), new Vector2(6f, 0.5f), -19990, true);
            CreateTiled(_interiorRoot.transform, "WallBottomRight", wall, new Vector3(4f, -5f, 0f), new Vector2(6f, 0.5f), -19990, true);

            // DJ booth at the back, bar on the right, VIP sofas on the left.
            CreateSprite(_interiorRoot.transform, "DJBooth", dj, new Vector3(0f, 3.7f, 0f),
                new Vector2(4.2f, 1.6f), true, -950, 4f, 1f);
            CreateSprite(_interiorRoot.transform, "ClubBar", bar, new Vector3(4.8f, 2.2f, 0f),
                new Vector2(4f, 1.4f), true, -950, 3.8f, 0.8f);
            CreateSprite(_interiorRoot.transform, "Bartender", bartender, new Vector3(4.8f, 3.35f, 0f),
                new Vector2(0.85f, 1.25f), false, Mathf.RoundToInt(3.35f * 100f));

            CreateSprite(_interiorRoot.transform, "VIP_A", sofa, new Vector3(-5f, 2.8f, 0f),
                new Vector2(3f, 1.4f), true, -950, 2.8f, 0.8f);
            CreateSprite(_interiorRoot.transform, "VIP_B", sofa, new Vector3(-5f, 0.9f, 0f),
                new Vector2(3f, 1.4f), true, -950, 2.8f, 0.8f);
            CreateSprite(_interiorRoot.transform, "VIPTableA", table, new Vector3(-3.2f, 2.8f, 0f),
                new Vector2(1.1f, 1.1f), true, -949, 1f, 0.8f);
            CreateSprite(_interiorRoot.transform, "VIPTableB", table, new Vector3(-3.2f, 0.9f, 0f),
                new Vector2(1.1f, 1.1f), true, -949, 1f, 0.8f);

            CreateSprite(_interiorRoot.transform, "SpeakerA", speaker, new Vector3(-6.2f, 4.1f, 0f),
                new Vector2(0.7f, 1.5f), true, -948, 0.6f, 1f);
            CreateSprite(_interiorRoot.transform, "SpeakerB", speaker, new Vector3(6.2f, 4.1f, 0f),
                new Vector2(0.7f, 1.5f), true, -948, 0.6f, 1f);

            CreateBlock(_interiorRoot.transform, "Entrance", new Vector3(0f, -4.98f, 0f),
                new Vector2(2f, 0.52f), new Color(0.04f, 0.04f, 0.06f), true, -947);
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
