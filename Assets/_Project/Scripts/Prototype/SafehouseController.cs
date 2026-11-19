using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Safehouse interaction:
    /// street door -> apartment, apartment left door -> separate bathroom,
    /// and the reverse for both. All spaces use the same camera and character.
    /// </summary>
    public sealed class SafehouseController : MonoBehaviour
    {
        private const float ApartmentEnterRange = 1.1f;
        private const float ApartmentExitRange = 0.9f;
        private const float BathroomEnterRange = 1.2f;
        private const float BathroomExitRange = 0.9f;

        private static readonly Vector2 ApartmentDoor = new Vector2(0f, 5.1f);
        private static readonly Vector2 ApartmentExitDoor = new Vector2(0f, -3.6f);
        private static readonly Vector3 ApartmentSpawn = new Vector3(0f, -2.6f, 0f);

        // Interaction point is slightly below the door center so approaching
        // from the lower side of the doorway still works.
        private static readonly Vector2 BathroomDoor = new Vector2(-6.4f, -0.5f);
        private static readonly Vector2 BathroomExitDoor = new Vector2(0f, -2.2f);
        private static readonly Vector3 BathroomSpawn = new Vector3(0f, -1.2f, 0f);
        private static readonly Vector3 ApartmentReturnFromBathroom = new Vector3(-5.2f, 0f, 0f);
        private static readonly Vector2 BedPoint = new Vector2(4.2f, -1.65f);

        private Transform _streetRoot;
        private Transform _player;
        private DialogueChoiceController _dialogue;
        private Vector2 _exteriorDoor;
        private GameObject _interiorRoot;
        private GameObject _bathroomRoot;
        private Sprite _whiteSprite;
        private bool _inside;
        private bool _inBathroom;
        private string _prompt;

        public bool IsInside => _inside;
        public bool IsInBathroom => _inBathroom;

        /// <summary>
        /// Builds (if needed) and clones the apartment interior. Used for the
        /// south neighbour's apartment so the layout can be reused exactly.
        /// </summary>
        public GameObject CreateApartmentCopy(Vector3 offset)
        {
            if (_interiorRoot == null) BuildApartment();
            GameObject copy = Instantiate(_interiorRoot, offset, Quaternion.identity);
            copy.name = "NeighborApartmentInterior";
            copy.SetActive(false);
            if (!_inside) _interiorRoot.SetActive(false);
            return copy;
        }

        /// <summary>Builds and clones the bathroom for the south neighbour.</summary>
        public GameObject CreateBathroomCopy(Vector3 offset)
        {
            if (_bathroomRoot == null) BuildBathroom();
            GameObject copy = Instantiate(_bathroomRoot, offset, Quaternion.identity);
            copy.name = "NeighborBathroomInterior";
            copy.SetActive(false);
            if (!_inBathroom) _bathroomRoot.SetActive(false);
            return copy;
        }

        public void Configure(Transform streetRoot, Transform player, Vector2 exteriorDoor)
        {
            _streetRoot = streetRoot;
            _player = player;
            _exteriorDoor = exteriorDoor;
        }

        public void SetDialogue(DialogueChoiceController dialogue)
        {
            _dialogue = dialogue;
        }

        private void Update()
        {
            if (_player == null || StorageInteraction.AnyPanelOpen) return;

            bool nearExterior = !_inside && Vector2.Distance(_player.position, _exteriorDoor) <= ApartmentEnterRange;
            bool nearApartmentExit = _inside && !_inBathroom && Vector2.Distance(_player.position, ApartmentExitDoor) <= ApartmentExitRange;
            bool nearBathroomDoor = _inside && !_inBathroom && Vector2.Distance(_player.position, BathroomDoor) <= BathroomEnterRange;
            bool nearBathroomExit = _inBathroom && Vector2.Distance(_player.position, BathroomExitDoor) <= BathroomExitRange;
            bool nearBed = _inside && !_inBathroom && Vector2.Distance(_player.position, BedPoint) <= 1.35f;

            if (!_inside)
            {
                _prompt = nearExterior ? "家" : string.Empty;
            }
            else if (_inBathroom)
            {
                _prompt = nearBathroomExit ? "公寓" : string.Empty;
            }
            else if (nearBed)
            {
                _prompt = "床";
            }
            else if (nearApartmentExit)
            {
                _prompt = "街道";
            }
            else
            {
                _prompt = nearBathroomDoor ? "浴室" : string.Empty;
            }

            if (!InteractionInput.TryConsumeF(!string.IsNullOrEmpty(_prompt))) return;

            if (_inBathroom && nearBathroomExit) ExitBathroom();
            else if (_inside && !_inBathroom && nearBed) OpenBedDialogue();
            else if (_inside && !_inBathroom && nearApartmentExit) ExitApartment();
            else if (_inside && !_inBathroom && nearBathroomDoor) EnterBathroom();
            else if (!_inside && nearExterior) EnterApartment();
        }

        private void OpenBedDialogue()
        {
            if (_dialogue == null)
            {
                SaveGameService.Save(_player);
                return;
            }

            _dialogue.OpenChoice("床", "要休息一下吗？",
                "保存进度", () =>
                {
                    SaveGameService.Save(_player);
                    _dialogue.OpenNotice("床", "进度已保存。");
                },
                "读取进度", () =>
                {
                    if (SaveGameService.Load(_player)) _dialogue.OpenNotice("床", "进度已读取。");
                    else _dialogue.OpenNotice("床", "没有可读取的存档。");
                });
        }

        public void EnterApartment()
        {
            if (_inside || _streetRoot == null || _player == null) return;

            _inside = true;
            _inBathroom = false;
            InteractionPromptUI.SetPrompt("storage", string.Empty);
            InteractionPromptUI.SetPrompt("crime", string.Empty);
            _streetRoot.gameObject.SetActive(false);
            if (_interiorRoot == null) BuildApartment();
            else _interiorRoot.SetActive(true);

            Rigidbody2D body = _player.GetComponent<Rigidbody2D>();
            if (body != null) body.velocity = Vector2.zero;
            _player.position = ApartmentSpawn;
        }

        public void ExitApartment()
        {
            if (!_inside) return;
            if (_inBathroom) ExitBathroom();

            _inside = false;
            _inBathroom = false;
            InteractionPromptUI.SetPrompt("storage", string.Empty);
            InteractionPromptUI.SetPrompt("crime", string.Empty);
            if (_interiorRoot != null) _interiorRoot.SetActive(false);
            if (_bathroomRoot != null) _bathroomRoot.SetActive(false);
            if (_streetRoot != null) _streetRoot.gameObject.SetActive(true);

            Rigidbody2D body = _player.GetComponent<Rigidbody2D>();
            if (body != null) body.velocity = Vector2.zero;
            _player.position = new Vector3(_exteriorDoor.x, _exteriorDoor.y - 0.5f, 0f);
        }

        public void EnterBathroom()
        {
            if (!_inside || _inBathroom) return;

            _inBathroom = true;
            if (_interiorRoot != null) _interiorRoot.SetActive(false);
            if (_bathroomRoot == null) BuildBathroom();
            else _bathroomRoot.SetActive(true);

            Rigidbody2D body = _player.GetComponent<Rigidbody2D>();
            if (body != null) body.velocity = Vector2.zero;
            _player.position = BathroomSpawn;
        }

        public void ExitBathroom()
        {
            if (!_inBathroom) return;

            _inBathroom = false;
            if (_bathroomRoot != null) _bathroomRoot.SetActive(false);
            if (_interiorRoot != null) _interiorRoot.SetActive(true);

            Rigidbody2D body = _player.GetComponent<Rigidbody2D>();
            if (body != null) body.velocity = Vector2.zero;
            _player.position = ApartmentReturnFromBathroom;
        }

        private void BuildApartment()
        {
            _interiorRoot = new GameObject("SafehouseInterior");
            Sprite wallSprite = LoadInteriorSprite("wall_tile");
            Sprite bedSprite = LoadInteriorSprite("bed");
            Sprite tableSprite = LoadInteriorSprite("table_chair");
            Sprite windowSprite = LoadInteriorSprite("window");
            Sprite clockSprite = LoadInteriorSprite("clock");
            Sprite bathroomDoorSprite = LoadInteriorSprite("bathroom_door");
            Sprite safeSprite = Resources.Load<Sprite>("Environment/BrickRow/Props/safe");

            GameObject floor = new GameObject("Floor");
            floor.transform.SetParent(_interiorRoot.transform, false);
            floor.transform.position = Vector3.zero;
            SpriteRenderer floorRenderer = floor.AddComponent<SpriteRenderer>();
            floorRenderer.sprite = CreateFloorSprite();
            floorRenderer.sortingOrder = -900;

            if (wallSprite != null)
            {
                CreateWall(_interiorRoot.transform, "WallTop", new Vector3(0f, 4.4f, 0f), new Vector2(12.8f, 0.8f), wallSprite);
                CreateWall(_interiorRoot.transform, "WallLeft", new Vector3(-6.4f, 0f, 0f), new Vector2(0.8f, 8.8f), wallSprite);
                CreateWall(_interiorRoot.transform, "WallRight", new Vector3(6.4f, 0f, 0f), new Vector2(0.8f, 8.8f), wallSprite);
                CreateWall(_interiorRoot.transform, "WallBottomLeft", new Vector3(-3.6f, -4.4f, 0f), new Vector2(5.6f, 0.8f), wallSprite);
                CreateWall(_interiorRoot.transform, "WallBottomRight", new Vector3(3.6f, -4.4f, 0f), new Vector2(5.6f, 0.8f), wallSprite);
            }
            else
            {
                CreateBlock(_interiorRoot.transform, "WallTop", new Vector3(0f, 4.4f, 0f), new Vector2(12.8f, 0.8f), new Color(0.34f, 0.20f, 0.18f), true, -950);
                CreateBlock(_interiorRoot.transform, "WallLeft", new Vector3(-6.4f, 0f, 0f), new Vector2(0.8f, 8.8f), new Color(0.34f, 0.20f, 0.18f), true, -950);
                CreateBlock(_interiorRoot.transform, "WallRight", new Vector3(6.4f, 0f, 0f), new Vector2(0.8f, 8.8f), new Color(0.34f, 0.20f, 0.18f), true, -950);
                CreateBlock(_interiorRoot.transform, "WallBottomLeft", new Vector3(-3.6f, -4.4f, 0f), new Vector2(5.6f, 0.8f), new Color(0.30f, 0.18f, 0.16f), true, -950);
                CreateBlock(_interiorRoot.transform, "WallBottomRight", new Vector3(3.6f, -4.4f, 0f), new Vector2(5.6f, 0.8f), new Color(0.30f, 0.18f, 0.16f), true, -950);
            }

            // Apartment exit door.
            CreateBlock(_interiorRoot.transform, "DoorFrameLeft", new Vector3(-0.75f, -4.05f, 0f), new Vector2(0.2f, 0.7f), new Color(0.20f, 0.12f, 0.10f), false, -940);
            CreateBlock(_interiorRoot.transform, "DoorFrameRight", new Vector3(0.75f, -4.05f, 0f), new Vector2(0.2f, 0.7f), new Color(0.20f, 0.12f, 0.10f), false, -940);
            CreateBlock(_interiorRoot.transform, "Threshold", new Vector3(0f, -3.92f, 0f), new Vector2(1.2f, 0.14f), new Color(0.48f, 0.32f, 0.22f), false, -930);
            CreateBlock(_interiorRoot.transform, "DoorBlocker", new Vector3(0f, -4.65f, 0f), new Vector2(1.2f, 0.3f), Color.clear, true, -960);

            // Bathroom door on the left wall (closed door visual; F interaction).
            CreateBlock(_interiorRoot.transform, "BathroomDoorFrameTop", new Vector3(-6.4f, 0.7f, 0f), new Vector2(0.9f, 0.2f), new Color(0.20f, 0.12f, 0.10f), false, -940);
            CreateBlock(_interiorRoot.transform, "BathroomDoorFrameBottom", new Vector3(-6.4f, -0.7f, 0f), new Vector2(0.9f, 0.2f), new Color(0.20f, 0.12f, 0.10f), false, -940);
            CreateBlock(_interiorRoot.transform, "BathroomThreshold", new Vector3(-6.15f, 0f, 0f), new Vector2(0.3f, 1.2f), new Color(0.60f, 0.44f, 0.30f), false, -930);
            if (bathroomDoorSprite != null)
            {
                CreateSpriteObject(_interiorRoot.transform, "BathroomDoor", bathroomDoorSprite, new Vector3(-6.05f, 0f, 0f), new Vector2(0.5f, 1.25f), false, -940);
            }

            if (windowSprite != null)
            {
                CreateSpriteObject(_interiorRoot.transform, "Window", windowSprite, new Vector3(-2.5f, 4.4f, 0f), new Vector2(2f, 1f), false, -940);
            }
            if (clockSprite != null)
            {
                CreateSpriteObject(_interiorRoot.transform, "Clock", clockSprite, new Vector3(2.5f, 4.4f, 0f), new Vector2(1f, 1f), false, -940);
            }

            if (tableSprite != null)
            {
                GameObject table = CreateSpriteObject(_interiorRoot.transform, "TableAndChair", tableSprite, new Vector3(-3f, 2.9f, 0f), new Vector2(4f, 2f), true,
                    Mathf.RoundToInt(-(2.9f - 1f) * 100f));
                AddStorage(table, "TABLE", 4);
            }
            if (bedSprite != null)
            {
                CreateSpriteObject(_interiorRoot.transform, "Bed", bedSprite, new Vector3(4.2f, 0f, 0f), new Vector2(2f, 3f), true,
                    Mathf.RoundToInt(-(0f - 1.5f) * 100f));
            }
            if (safeSprite != null)
            {
                GameObject safe = CreateSpriteObject(_interiorRoot.transform, "Safe", safeSprite, new Vector3(5.1f, 2.55f, 0f),
                    new Vector2(1.4f, 1.4f), true, Mathf.RoundToInt(-2.55f * 100f));
                AddStorage(safe, "SAFE", 20);
            }

            CreateBlock(_interiorRoot.transform, "BaseboardTop", new Vector3(0f, 3.98f, 0f), new Vector2(12f, 0.12f), new Color(0.48f, 0.32f, 0.22f), false, -940);
            CreateBlock(_interiorRoot.transform, "BaseboardLeft", new Vector3(-5.98f, 0f, 0f), new Vector2(0.12f, 8f), new Color(0.48f, 0.32f, 0.22f), false, -940);
            CreateBlock(_interiorRoot.transform, "BaseboardRight", new Vector3(5.98f, 0f, 0f), new Vector2(0.12f, 8f), new Color(0.48f, 0.32f, 0.22f), false, -940);
        }

        private void BuildBathroom()
        {
            _bathroomRoot = new GameObject("SafehouseBathroom");
            Sprite wallSprite = LoadInteriorSprite("wall_tile");
            Sprite tileSprite = LoadInteriorSprite("bathroom_tile");
            Sprite toiletSprite = LoadInteriorSprite("toilet");
            Sprite sinkSprite = LoadInteriorSprite("sink");
            Sprite mirrorSprite = LoadInteriorSprite("mirror");
            Sprite showerSprite = LoadInteriorSprite("shower");

            if (tileSprite != null)
            {
                CreateTiledFloor(_bathroomRoot.transform, "BathroomFloor", Vector3.zero, new Vector2(7.2f, 4f), tileSprite, -899);
            }

            if (wallSprite != null)
            {
                CreateWall(_bathroomRoot.transform, "BathroomTop", new Vector3(0f, 2.4f, 0f), new Vector2(7.6f, 0.8f), wallSprite);
                CreateWall(_bathroomRoot.transform, "BathroomBottomLeft", new Vector3(-2.2f, -2.4f, 0f), new Vector2(3.2f, 0.8f), wallSprite);
                CreateWall(_bathroomRoot.transform, "BathroomBottomRight", new Vector3(2.2f, -2.4f, 0f), new Vector2(3.2f, 0.8f), wallSprite);
                CreateWall(_bathroomRoot.transform, "BathroomLeft", new Vector3(-3.8f, 0f, 0f), new Vector2(0.8f, 5.6f), wallSprite);
                CreateWall(_bathroomRoot.transform, "BathroomRight", new Vector3(3.8f, 0f, 0f), new Vector2(0.8f, 5.6f), wallSprite);
            }

            CreateBlock(_bathroomRoot.transform, "DoorFrameLeft", new Vector3(-0.75f, -2.05f, 0f), new Vector2(0.2f, 0.7f), new Color(0.20f, 0.12f, 0.10f), false, -940);
            CreateBlock(_bathroomRoot.transform, "DoorFrameRight", new Vector3(0.75f, -2.05f, 0f), new Vector2(0.2f, 0.7f), new Color(0.20f, 0.12f, 0.10f), false, -940);
            CreateBlock(_bathroomRoot.transform, "Threshold", new Vector3(0f, -1.92f, 0f), new Vector2(1.2f, 0.14f), new Color(0.60f, 0.44f, 0.30f), false, -930);
            CreateBlock(_bathroomRoot.transform, "DoorBlocker", new Vector3(0f, -2.65f, 0f), new Vector2(1.2f, 0.3f), Color.clear, true, -960);

            if (toiletSprite != null)
            {
                CreateSpriteObject(_bathroomRoot.transform, "Toilet", toiletSprite, new Vector3(-1.9f, 0.8f, 0f), new Vector2(1f, 1.5f), true,
                    Mathf.RoundToInt(-(0.8f - 0.75f) * 100f));
            }
            if (sinkSprite != null)
            {
                CreateSpriteObject(_bathroomRoot.transform, "Sink", sinkSprite, new Vector3(2.4f, 1.2f, 0f), new Vector2(1.4f, 1.05f), true,
                    Mathf.RoundToInt(-(1.2f - 0.375f) * 100f));
            }
            if (mirrorSprite != null)
            {
                GameObject mirror = CreateSpriteObject(_bathroomRoot.transform, "Mirror", mirrorSprite, new Vector3(2.4f, 2.02f, 0f), new Vector2(1.1f, 1.1f), false, -940);
                AddStorage(mirror, "MIRROR", 1);
            }
            if (showerSprite != null)
            {
                GameObject shower = CreateSpriteObject(_bathroomRoot.transform, "Shower", showerSprite, new Vector3(2.4f, -1f, 0f), new Vector2(2f, 2f), true,
                    Mathf.RoundToInt(-(-1f - 1f) * 100f));
                AddStorage(shower, "SHOWER", 1);
            }
        }

        private Sprite LoadInteriorSprite(string name)
        {
            Sprite[] available = Resources.LoadAll<Sprite>("Environment/BrickRow/Interior");
            foreach (Sprite sprite in available)
            {
                if (sprite.name.ToLowerInvariant().Contains(name))
                {
                    return sprite;
                }
            }
            return null;
        }

        private void CreateWall(Transform parent, string name, Vector3 position, Vector2 size, Sprite sprite)
        {
            GameObject wall = new GameObject(name);
            wall.transform.SetParent(parent, false);
            wall.transform.position = position;

            SpriteRenderer renderer = wall.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = size;
            renderer.sortingOrder = -950;

            BoxCollider2D box = wall.AddComponent<BoxCollider2D>();
            box.size = size;
        }

        private void CreateTiledFloor(Transform parent, string name, Vector3 position, Vector2 size, Sprite sprite, int sortingOrder)
        {
            GameObject floor = new GameObject(name);
            floor.transform.SetParent(parent, false);
            floor.transform.position = position;

            SpriteRenderer renderer = floor.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = size;
            renderer.sortingOrder = sortingOrder;
        }

        private GameObject CreateSpriteObject(Transform parent, string name, Sprite sprite, Vector3 position, Vector2 worldSize, bool collider, int sortingOrder)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = position;

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            visual.transform.localScale = new Vector3(
                worldSize.x / Mathf.Max(0.001f, sprite.bounds.size.x),
                worldSize.y / Mathf.Max(0.001f, sprite.bounds.size.y),
                1f);

            if (collider)
            {
                BoxCollider2D box = root.AddComponent<BoxCollider2D>();
                box.size = new Vector2(worldSize.x * 0.86f, worldSize.y * 0.56f);
            }

            return root;
        }

        private void AddStorage(GameObject target, string label, int capacity)
        {
            StorageContainer storage = target.AddComponent<StorageContainer>();
            storage.Configure(label, capacity);
        }

        private GameObject CreateBlock(Transform parent, string name, Vector3 position, Vector2 size, Color color, bool collider, int sortingOrder)
        {
            GameObject block = new GameObject(name);
            block.transform.SetParent(parent, false);
            block.transform.position = position;
            block.transform.localScale = new Vector3(size.x, size.y, 1f);

            SpriteRenderer renderer = block.AddComponent<SpriteRenderer>();
            renderer.sprite = GetWhiteSprite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            if (collider)
            {
                BoxCollider2D box = block.AddComponent<BoxCollider2D>();
                box.size = Vector2.one;
            }

            return block;
        }

        private Sprite CreateFloorSprite()
        {
            const int width = 384;
            const int height = 256;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[width * height];
            Color wood = new Color(0.40f, 0.28f, 0.19f);
            Color woodLight = new Color(0.46f, 0.33f, 0.22f);
            Color seam = new Color(0.27f, 0.18f, 0.13f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int plank = y / 16;
                    Color color = (plank % 2 == 0) ? wood : woodLight;
                    if (y % 16 == 0) color = seam;
                    if (x % 64 == 0 && y % 16 != 0) color = seam;
                    pixels[y * width + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 32f);
        }

        private Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null) return _whiteSprite;

            Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();

            _whiteSprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            return _whiteSprite;
        }

        private void OnGUI()
        {
            InteractionPromptUI.SetPrompt("safehouse", _prompt);
        }
    }
}
