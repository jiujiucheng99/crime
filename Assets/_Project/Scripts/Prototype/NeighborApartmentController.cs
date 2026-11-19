using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Configurable neighbour home. The interior and bathroom are cloned from
    /// the player's D6 home, then tinted and locked per building.
    /// </summary>
    public class NeighborApartmentController : MonoBehaviour
    {
        private const float EnterRange = 1.3f;
        private const float ExitRange = 1.0f;
        private const float BathroomEnterRange = 1.3f;
        private const float BathroomExitRange = 1.0f;

        private static NeighborApartmentController _promptOwner;

        private Transform _worldRoot;
        private Transform _player;
        private SafehouseController _safehouse;
        private DialogueChoiceController _dialogue;
        private DoorLockController _doorLock;
        private Vector2 _exteriorDoor;
        private string _displayName = "邻居1的家";

        private Vector3 _interiorOffset = new Vector3(4000f, 0f, 0f);
        private Vector3 _interiorSpawn;
        private Vector2 _interiorExitPoint;
        private Vector2 _bathroomDoorPoint;
        private Vector3 _bathroomOffset;
        private Vector3 _bathroomSpawn;
        private Vector2 _bathroomExitPoint;
        private Vector3 _apartmentReturnFromBathroom;
        private Color _wallTint = new Color(0.72f, 0.82f, 1f, 1f);
        private Color _bedTint = new Color(1f, 0.62f, 0.56f, 1f);

        private GameObject _interiorRoot;
        private GameObject _bathroomRoot;
        private bool _inside;
        private bool _inBathroom;
        private bool _apartmentStorageReady;
        private bool _bathroomStorageReady;
        private string _prompt;

        public bool IsInside => _inside;
        public bool IsInBathroom => _inBathroom;
        public string DisplayName => _displayName;
        public Vector2 ExteriorDoor => _exteriorDoor;

        public static NeighborApartmentController Active { get; private set; }

        public void Configure(Transform worldRoot, Transform player, Vector2 exteriorDoor,
            SafehouseController safehouse, DialogueChoiceController dialogue = null)
        {
            Configure(worldRoot, player, exteriorDoor, safehouse, dialogue,
                "邻居1的家", new Vector3(4000f, 0f, 0f),
                new Color(0.72f, 0.82f, 1f, 1f), new Color(1f, 0.62f, 0.56f, 1f));
        }

        public void Configure(Transform worldRoot, Transform player, Vector2 exteriorDoor,
            SafehouseController safehouse, DialogueChoiceController dialogue,
            string displayName, Vector3 interiorOffset, Color wallTint, Color bedTint)
        {
            _worldRoot = worldRoot;
            _player = player;
            _exteriorDoor = exteriorDoor;
            _safehouse = safehouse;
            _dialogue = dialogue;
            _displayName = displayName;
            _interiorOffset = interiorOffset;
            _wallTint = wallTint;
            _bedTint = bedTint;

            _interiorSpawn = interiorOffset + new Vector3(0f, -2.6f, 0f);
            _interiorExitPoint = new Vector2(interiorOffset.x, interiorOffset.y - 3.6f);
            _bathroomDoorPoint = new Vector2(interiorOffset.x - 6.4f, interiorOffset.y - 0.5f);
            _bathroomOffset = interiorOffset + new Vector3(100f, 0f, 0f);
            _bathroomSpawn = _bathroomOffset + new Vector3(0f, -1.2f, 0f);
            _bathroomExitPoint = new Vector2(_bathroomOffset.x, _bathroomOffset.y - 2.2f);
            _apartmentReturnFromBathroom = interiorOffset + new Vector3(-5.2f, 0f, 0f);

            _doorLock = GetComponent<DoorLockController>();
            if (_doorLock == null) _doorLock = gameObject.AddComponent<DoorLockController>();
            _doorLock.Configure(player, dialogue);
        }

        private void Update()
        {
            if (_player == null || StorageInteraction.AnyPanelOpen ||
                BusTravelController.IsOpen || DialogueChoiceController.IsOpen ||
                StoreShopUI.IsOpen) return;

            bool nearEntrance = !_inside && Vector2.Distance(_player.position, _exteriorDoor) <= EnterRange;
            bool nearExit = _inside && !_inBathroom && Vector2.Distance(_player.position, _interiorExitPoint) <= ExitRange;
            bool nearBathroomDoor = _inside && !_inBathroom &&
                Vector2.Distance(_player.position, _bathroomDoorPoint) <= BathroomEnterRange;
            bool nearBathroomExit = _inside && _inBathroom &&
                Vector2.Distance(_player.position, _bathroomExitPoint) <= BathroomExitRange;

            _prompt = !_inside
                ? (nearEntrance ? _displayName : string.Empty)
                : _inBathroom
                    ? (nearBathroomExit ? "公寓" : string.Empty)
                    : (nearBathroomDoor ? "浴室" : (nearExit ? "街道" : string.Empty));

            if (!InteractionInput.TryConsumeF(!string.IsNullOrEmpty(_prompt))) return;

            if (!_inside && nearEntrance)
            {
                if (_doorLock != null && _doorLock.IsUnlocked) Enter();
                else if (_doorLock != null)
                {
                    _doorLock.TryUnlock();
                }
            }
            else if (_inside && _inBathroom && nearBathroomExit) ExitBathroom();
            else if (_inside && !_inBathroom && nearBathroomDoor) EnterBathroom();
            else if (_inside && !_inBathroom && nearExit) Exit();
        }

        private void OnGUI()
        {
            if (!string.IsNullOrEmpty(_prompt))
            {
                _promptOwner = this;
                InteractionPromptUI.SetPrompt("neighbor", _prompt);
            }
            else if (_promptOwner == this)
            {
                _promptOwner = null;
                InteractionPromptUI.SetPrompt("neighbor", string.Empty);
            }
        }

        private void Enter()
        {
            if (_worldRoot == null || _player == null || _safehouse == null) return;
            _inside = true;
            _inBathroom = false;
            Active = this;
            InteractionPromptUI.SetPrompt("storage", string.Empty);
            InteractionPromptUI.SetPrompt("crime", string.Empty);
            if (CrimeLoopController.Instance != null) CrimeLoopController.Instance.OnNeighborEnter(this);
            if (_doorLock != null) TheftNoise.Add(_doorLock.LastUnlockNoise);
            if (_doorLock != null) _doorLock.Lock();
            _worldRoot.gameObject.SetActive(false);

            if (_interiorRoot == null) _interiorRoot = _safehouse.CreateApartmentCopy(_interiorOffset);
            if (!_apartmentStorageReady)
            {
                ApplyNeighborPalette(_interiorRoot, true);
                PrepareClonedStorage(_interiorRoot);
                PrepareLockedSafe();
                PrepareSearchContainers(_interiorRoot);
                _apartmentStorageReady = true;
            }
            _interiorRoot.SetActive(true);

            Rigidbody2D body = _player.GetComponent<Rigidbody2D>();
            if (body != null) body.velocity = Vector2.zero;
            _player.position = _interiorSpawn;
        }

        private void EnterBathroom()
        {
            if (_interiorRoot == null || _safehouse == null) return;
            _inBathroom = true;
            _interiorRoot.SetActive(false);
            if (_bathroomRoot == null) _bathroomRoot = _safehouse.CreateBathroomCopy(_bathroomOffset);
            if (!_bathroomStorageReady)
            {
                ApplyNeighborPalette(_bathroomRoot, false);
                PrepareClonedStorage(_bathroomRoot);
                PrepareSearchContainers(_bathroomRoot);
                _bathroomStorageReady = true;
            }
            _bathroomRoot.SetActive(true);

            Rigidbody2D body = _player.GetComponent<Rigidbody2D>();
            if (body != null) body.velocity = Vector2.zero;
            _player.position = _bathroomSpawn;
        }

        private void ExitBathroom()
        {
            _inBathroom = false;
            if (_bathroomRoot != null) _bathroomRoot.SetActive(false);
            if (_interiorRoot != null) _interiorRoot.SetActive(true);

            Rigidbody2D body = _player.GetComponent<Rigidbody2D>();
            if (body != null) body.velocity = Vector2.zero;
            _player.position = _apartmentReturnFromBathroom;
        }

        private void Exit()
        {
            _inside = false;
            _inBathroom = false;
            if (Active == this) Active = null;
            InteractionPromptUI.SetPrompt("storage", string.Empty);
            InteractionPromptUI.SetPrompt("crime", string.Empty);
            if (_doorLock != null) _doorLock.Lock();
            if (_interiorRoot != null) _interiorRoot.SetActive(false);
            if (_bathroomRoot != null) _bathroomRoot.SetActive(false);
            if (_worldRoot != null) _worldRoot.gameObject.SetActive(true);

            Rigidbody2D body = _player.GetComponent<Rigidbody2D>();
            if (body != null) body.velocity = Vector2.zero;
            _player.position = new Vector3(_exteriorDoor.x, _exteriorDoor.y + 0.8f, 0f);
            if (CrimeLoopController.Instance != null) CrimeLoopController.Instance.OnNeighborExit(this);
        }

        private void ApplyNeighborPalette(GameObject root, bool tintBed)
        {
            if (root == null) return;

            SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer.gameObject.name.StartsWith("Wall") ||
                    (renderer.gameObject.name.StartsWith("Bathroom") &&
                     !renderer.gameObject.name.StartsWith("BathroomFloor")))
                {
                    renderer.color = _wallTint;
                }
            }

            if (!tintBed) return;
            Transform bed = FindChild(root.transform, "Bed");
            if (bed == null) return;
            foreach (SpriteRenderer renderer in bed.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.color = _bedTint;
            }
        }

        private void PrepareClonedStorage(GameObject root)
        {
            if (root == null) return;
            StorageContainer[] containers = root.GetComponentsInChildren<StorageContainer>(true);
            foreach (StorageContainer container in containers)
            {
                container.Configure(container.DisplayName, container.Capacity);
                if (container.DisplayName == "SAFE") continue;

                if (container.Count == 0)
                {
                    container.TryAdd(StolenGoods.CreateRandomStack());
                    if (container.Capacity >= 4 && Random.value < 0.5f)
                    {
                        container.TryAdd(StolenGoods.CreateRandomStack());
                    }
                }
            }
        }

        private void PrepareLockedSafe()
        {
            if (_interiorRoot == null) return;
            Transform safe = FindChild(_interiorRoot.transform, "Safe");
            if (safe == null) return;

            SafeLockController existing = safe.GetComponent<SafeLockController>();
            if (existing != null) return;

            StorageContainer storage = safe.GetComponent<StorageContainer>();
            SpriteRenderer renderer = safe.GetComponentInChildren<SpriteRenderer>();
            Sprite openSprite = Resources.Load<Sprite>("Environment/BrickRow/Props/safe_open");
            SafeLockController safeLock = safe.gameObject.AddComponent<SafeLockController>();
            safeLock.Configure(_player, renderer, openSprite, storage,
                _player != null ? _player.GetComponent<PlayerWallet>() : null);
        }

        private void PrepareSearchContainers(GameObject root)
        {
            if (root == null) return;
            StorageContainer[] containers = root.GetComponentsInChildren<StorageContainer>(true);
            foreach (StorageContainer container in containers)
            {
                if (container.GetComponent<SafeLockController>() != null) continue;
                if (container.GetComponent<ContainerSearchController>() != null) continue;

                string label = container.DisplayName;
                if (label == "TABLE") label = "桌子";
                else if (label == "MIRROR") label = "镜子";
                else if (label == "SHOWER") label = "淋浴";

                ContainerSearchController search = container.gameObject.AddComponent<ContainerSearchController>();
                search.Configure(_player, container, label);
            }
        }

        private static Transform FindChild(Transform root, string childName)
        {
            if (root.name == childName) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChild(root.GetChild(i), childName);
                if (found != null) return found;
            }
            return null;
        }
    }
}
