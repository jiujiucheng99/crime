using UnityEngine;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Restaurant kitchen behind the employee passage. Scene only; restaurant
    /// work tasks will be added later.
    /// </summary>
    public class RestaurantKitchenController : MonoBehaviour
    {
        private const float ExitRange = 1.2f;
        private const float StoveRange = 1.3f;
        private static readonly Vector3 InteriorOffset = new Vector3(13000f, 0f, 0f);
        private static readonly Vector3 InteriorSpawn = new Vector3(13000f, -2.7f, 0f);
        private static readonly Vector2 InteriorExitPoint = new Vector2(13000f, -3.2f);
        private static readonly Vector2 StovePoint = new Vector2(12995.8f, 2.2f);
        private static readonly Vector2 ReturnToRestaurant = new Vector2(6000f, 3.5f);

        private Transform _player;
        private DialogueChoiceController _dialogue;
        private PlayerEmployment _employment;
        private PlayerInventory _inventory;
        private PlayerWallet _wallet;
        private PlayerSkills _skills;
        private CookingMinigameUI _cookingUI;
        private CookingRecipe _order;
        private GameObject _restaurantRoot;
        private GameObject _kitchenRoot;
        private bool _inside;
        private string _prompt;

        public bool IsInside => _inside;
        public static RestaurantKitchenController Active { get; private set; }

        public void Configure(Transform player, DialogueChoiceController dialogue, PlayerEmployment employment)
        {
            _player = player;
            _dialogue = dialogue;
            _employment = employment;
            _inventory = player != null ? player.GetComponent<PlayerInventory>() : null;
            _wallet = player != null ? player.GetComponent<PlayerWallet>() : null;
            _skills = player != null ? player.GetComponent<PlayerSkills>() : null;

            _cookingUI = gameObject.AddComponent<CookingMinigameUI>();
            _cookingUI.Configure(player);
        }

        public void Enter(GameObject restaurantRoot)
        {
            if (_inside) return;
            if (_employment == null || _employment.CurrentJobId != "restaurant_waiter")
            {
                if (_dialogue != null) _dialogue.OpenNotice("员工通道", "员工专用，请先入职餐厅。");
                return;
            }

            _restaurantRoot = restaurantRoot;
            if (_restaurantRoot != null) _restaurantRoot.SetActive(false);
            _inside = true;
            Active = this;
            _order = CookingRecipes.RandomRecipe();
            if (_kitchenRoot == null) BuildInterior();
            _kitchenRoot.SetActive(true);

            Rigidbody2D body = _player.GetComponent<Rigidbody2D>();
            if (body != null) body.velocity = Vector2.zero;
            _player.position = InteriorSpawn;
        }

        public void Exit()
        {
            _inside = false;
            if (Active == this) Active = null;
            CookingIngredients.ClearFrom(_inventory);
            if (_kitchenRoot != null) _kitchenRoot.SetActive(false);
            if (_restaurantRoot != null) _restaurantRoot.SetActive(true);
            if (_player == null) return;

            Rigidbody2D body = _player.GetComponent<Rigidbody2D>();
            if (body != null) body.velocity = Vector2.zero;
            _player.position = ReturnToRestaurant;
        }

        private void Update()
        {
            if (!_inside || _player == null) return;
            if (CookingMinigameUI.IsOpen || DialogueChoiceController.IsOpen || StorageInteraction.AnyPanelOpen) return;

            bool nearStove = Vector2.Distance(_player.position, StovePoint) <= StoveRange;
            bool nearExit = Vector2.Distance(_player.position, InteriorExitPoint) <= ExitRange;
            _prompt = nearStove ? "灶台" : (nearExit ? "回到餐厅" : string.Empty);
            if (!InteractionInput.TryConsumeF(!string.IsNullOrEmpty(_prompt))) return;
            if (nearStove) StartCooking();
            else if (nearExit) Exit();
        }

        private void StartCooking()
        {
            if (_order == null) return;
            if (!_order.HasIngredients(_inventory))
            {
                if (_dialogue != null)
                {
                    _dialogue.OpenNotice("缺少食材", _order.Name + " 需要：" + _order.IngredientList() + "。");
                }
                return;
            }

            _order.Consume(_inventory);
            int cookingLevel = _skills != null ? Mathf.Max(1, _skills.LevelOf("cooking")) : 1;
            _cookingUI.Open(_order, cookingLevel, OnCooked);
        }

        private void OnCooked(int reward)
        {
            if (_wallet != null) _wallet.Add(reward);
            if (_skills != null)
            {
                int xp = reward >= 30 ? 30 : (reward >= 20 ? 20 : 10);
                _skills.AddExperience("cooking", xp);
            }
            if (_dialogue != null)
            {
                _dialogue.OpenNotice("出餐完成", _order.Name + " 完成，获得 " + reward + " 块。");
            }
            _order = CookingRecipes.RandomRecipe(_order);
        }

        private void OnGUI()
        {
            if (!_inside) return;
            InteractionPromptUI.SetPrompt("kitchen", _prompt);
            if (_order != null && !CookingMinigameUI.IsOpen)
            {
                GUIStyle style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.95f, 0.89f, 0.72f) }
                };
                GUI.Label(new Rect(Screen.width * 0.5f - 260f, 54f, 520f, 24f),
                    "订单：" + _order.Name + "    食材：" + _order.IngredientList(), style);
            }
        }

        private void BuildInterior()
        {
            _kitchenRoot = new GameObject("RestaurantKitchen");
            _kitchenRoot.transform.position = InteriorOffset;

            Sprite floor = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Restaurant/Kitchen/kitchen_floor");
            Sprite wall = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Restaurant/Kitchen/kitchen_wall");
            Sprite counter = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Restaurant/Kitchen/kitchen_counter");
            Sprite stove = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Restaurant/Kitchen/kitchen_stove");
            Sprite sink = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Restaurant/Kitchen/kitchen_sink");
            Sprite fridge = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Restaurant/Kitchen/kitchen_fridge");
            Sprite shelf = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Restaurant/Kitchen/kitchen_shelf");
            Sprite prep = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Restaurant/Kitchen/kitchen_prep");
            Sprite chef = Resources.Load<Sprite>("Environment/BrickRow/Businesses/Restaurant/Kitchen/kitchen_chef");

            CreateTiled(_kitchenRoot.transform, "Floor", floor, Vector3.zero, new Vector2(12f, 8f), -20000, false);
            CreateTiled(_kitchenRoot.transform, "WallTop", wall, new Vector3(0f, 4f, 0f), new Vector2(12f, 0.5f), -19990, true);
            CreateTiled(_kitchenRoot.transform, "WallLeft", wall, new Vector3(-6f, 0f, 0f), new Vector2(0.4f, 8f), -19990, true);
            CreateTiled(_kitchenRoot.transform, "WallRight", wall, new Vector3(6f, 0f, 0f), new Vector2(0.4f, 8f), -19990, true);
            CreateTiled(_kitchenRoot.transform, "WallBottomLeft", wall, new Vector3(-3.5f, -4f, 0f), new Vector2(5f, 0.5f), -19990, true);
            CreateTiled(_kitchenRoot.transform, "WallBottomRight", wall, new Vector3(3.5f, -4f, 0f), new Vector2(5f, 0.5f), -19990, true);

            // Cooking line on the back wall.
            CreateSprite(_kitchenRoot.transform, "KitchenCounter", counter, new Vector3(0f, 3.2f, 0f),
                new Vector2(6f, 1.3f), true, -950, 5.6f, 0.8f);
            CreateSprite(_kitchenRoot.transform, "Stove", stove, new Vector3(-4.2f, 3.2f, 0f),
                new Vector2(1.8f, 1.4f), true, -950, 1.7f, 0.8f);
            WorkAreaMarker.Create(_kitchenRoot.transform, "WorkMarker_Stove",
                new Vector3(-4.2f, 2.5f, 0f), new Color(0.95f, 0.78f, 0.22f));
            CreateSprite(_kitchenRoot.transform, "Sink", sink, new Vector3(4.2f, 3.2f, 0f),
                new Vector2(1.8f, 1.2f), true, -950, 1.7f, 0.7f);
            CreateSprite(_kitchenRoot.transform, "Chef", chef, new Vector3(-1.6f, 2.1f, 0f),
                new Vector2(0.85f, 1.3f), false, Mathf.RoundToInt(2.1f * 100f));

            CreateSprite(_kitchenRoot.transform, "PrepTable", prep, new Vector3(0f, 0.8f, 0f),
                new Vector2(4.2f, 1.2f), true, -950, 3.8f, 0.75f);
            GameObject fridgeObject = CreateSprite(_kitchenRoot.transform, "Fridge", fridge, new Vector3(-5.2f, 1.2f, 0f),
                new Vector2(1.2f, 2f), true, -950, 1.1f, 1.4f);
            if (fridgeObject != null)
            {
                StorageContainer storage = fridgeObject.AddComponent<StorageContainer>();
                storage.Configure("FRIDGE", 6);
                AutoRestockContainer restock = fridgeObject.AddComponent<AutoRestockContainer>();
                restock.Configure(CookingIngredients.All, 5, "FRIDGE", 12);
                restock.EnsureStock();
                WorkAreaMarker.Create(_kitchenRoot.transform, "WorkMarker_Fridge",
                    new Vector3(-5.2f, 0.0f, 0f), new Color(0.35f, 0.72f, 0.92f));
            }
            CreateSprite(_kitchenRoot.transform, "Shelf", shelf, new Vector3(4.8f, 1.2f, 0f),
                new Vector2(2f, 1.2f), true, -950, 1.8f, 0.8f);

            CreateBlock(_kitchenRoot.transform, "Entrance", new Vector3(0f, -3.98f, 0f),
                new Vector2(1.8f, 0.52f), new Color(0.06f, 0.07f, 0.08f), true, -947);
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
