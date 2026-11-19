using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Convenience-store stockroom behind the employee passage. The clerk job's
    /// backroom task is to organise three boxes.
    /// </summary>
    public class StockroomController : MonoBehaviour
    {
        private const float StationRange = 1.3f;
        private const float ExitRange = 1.1f;
        private const int RequiredBoxes = 3;
        private const int Wage = 10;

        private static readonly Vector3 InteriorOffset = new Vector3(11000f, 0f, 0f);
        private static readonly Vector3 InteriorSpawn = new Vector3(11000f, -2.0f, 0f);
        private static readonly Vector2 InteriorExitPoint = new Vector2(11000f, -3.2f);
        private static readonly Vector2 ReturnToStore = new Vector2(2004.7f, 2.8f);

        private Transform _player;
        private DialogueChoiceController _dialogue;
        private PlayerEmployment _employment;
        private PlayerWallet _wallet;
        private GameObject _storeRoot;
        private GameObject _stockroomRoot;
        private GameObject[] _stations;
        private Vector3[] _stationPositions;
        private bool[] _sorted;
        private bool _inside;
        private bool _skipExitFrame;
        private bool _completed;
        private int _count;
        private string _prompt;
        private GUIStyle _statusStyle;

        public bool IsInside => _inside;

        public void Configure(Transform player, DialogueChoiceController dialogue,
            PlayerEmployment employment, PlayerWallet wallet)
        {
            _player = player;
            _dialogue = dialogue;
            _employment = employment;
            _wallet = wallet;
        }

        public void Enter(GameObject storeRoot)
        {
            if (_inside) return;
            if (_employment == null || _employment.CurrentJobId != "store_clerk")
            {
                if (_dialogue != null) _dialogue.OpenNotice("员工通道", "员工专用，请先入职便利店。");
                return;
            }

            _storeRoot = storeRoot;
            if (_storeRoot != null) _storeRoot.SetActive(false);
            _inside = true;
            _skipExitFrame = true;
            _completed = false;
            _count = 0;
            if (_stockroomRoot == null) BuildInterior();
            else ResetBoxes();
            _stockroomRoot.SetActive(true);

            Rigidbody2D body = _player.GetComponent<Rigidbody2D>();
            if (body != null) body.velocity = Vector2.zero;
            _player.position = InteriorSpawn;
        }

        public void Exit()
        {
            _inside = false;
            if (_stockroomRoot != null) _stockroomRoot.SetActive(false);
            if (_storeRoot != null) _storeRoot.SetActive(true);

            Rigidbody2D body = _player.GetComponent<Rigidbody2D>();
            if (body != null) body.velocity = Vector2.zero;
            _player.position = ReturnToStore;
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
            int station = FindNearestStation();
            _prompt = station >= 0 ? "整理货箱" : (nearExit ? "回到店内" : string.Empty);

            if (!InteractionInput.TryConsumeF(!string.IsNullOrEmpty(_prompt))) return;

            if (station >= 0) SortBox(station);
            else if (nearExit) Exit();
        }

        private int FindNearestStation()
        {
            if (_stations == null) return -1;
            int best = -1;
            float bestDistance = StationRange;
            for (int i = 0; i < _stations.Length; i++)
            {
                if (_stations[i] == null || _sorted[i]) continue;
                float distance = Vector2.Distance(_player.position, _stations[i].transform.position);
                if (distance < bestDistance)
                {
                    best = i;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private void SortBox(int index)
        {
            if (index < 0 || index >= _stations.Length || _sorted[index]) return;
            _sorted[index] = true;
            _count++;

            SpriteRenderer renderer = _stations[index].GetComponentInChildren<SpriteRenderer>();
            if (renderer != null) renderer.color = new Color(0.62f, 1f, 0.66f, 1f);
            _stations[index].transform.localPosition += new Vector3(0f, 0.1f, 0f);

            if (_count >= RequiredBoxes)
            {
                _completed = true;
                if (_wallet != null) _wallet.Add(Wage);
                if (_dialogue != null) _dialogue.OpenNotice("仓库", "整理完成，工资 " + Wage + " 块。");
            }
        }

        private void ResetBoxes()
        {
            _count = 0;
            _completed = false;
            if (_stations == null) return;
            for (int i = 0; i < _stations.Length; i++)
            {
                _sorted[i] = false;
                if (_stations[i] == null) continue;
                _stations[i].transform.localPosition = _stationPositions[i];
                SpriteRenderer renderer = _stations[i].GetComponentInChildren<SpriteRenderer>();
                if (renderer != null) renderer.color = Color.white;
            }
        }

        private void OnGUI()
        {
            if (!_inside) return;
            InteractionPromptUI.SetPrompt("stockroom", _prompt);
            if (_statusStyle == null)
            {
                Font font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Microsoft YaHei", "Microsoft YaHei UI", "SimHei", "Noto Sans CJK SC", "Arial" }, 16);
                _statusStyle = new GUIStyle(GUI.skin.label)
                {
                    font = font,
                    fontSize = 15,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.95f, 0.89f, 0.72f) }
                };
            }

            Rect rect = new Rect(Screen.width * 0.5f - 120f, 18f, 240f, 30f);
            Color previous = GUI.color;
            GUI.color = new Color(0.04f, 0.05f, 0.06f, 0.9f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(0.62f, 0.52f, 0.32f, 1f);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Label(rect, _completed ? "整理完成" : "整理货箱 " + _count + " / " + RequiredBoxes, _statusStyle);
        }

        private void BuildInterior()
        {
            _stockroomRoot = new GameObject("ConvenienceStockroom");
            _stockroomRoot.transform.position = InteriorOffset;

            Sprite floor = Resources.Load<Sprite>("Environment/BrickRow/Businesses/ConvenienceStore/Stockroom/stockroom_floor");
            Sprite wall = Resources.Load<Sprite>("Environment/BrickRow/Businesses/ConvenienceStore/Stockroom/stockroom_wall");
            Sprite shelf = Resources.Load<Sprite>("Environment/BrickRow/Businesses/ConvenienceStore/Stockroom/stockroom_shelf");
            Sprite box = Resources.Load<Sprite>("Environment/BrickRow/Businesses/ConvenienceStore/Stockroom/stockroom_box");
            Sprite table = Resources.Load<Sprite>("Environment/BrickRow/Businesses/ConvenienceStore/Stockroom/stockroom_table");

            CreateTiled(_stockroomRoot.transform, "Floor", floor, Vector3.zero, new Vector2(9f, 7f), -20000, false);
            CreateTiled(_stockroomRoot.transform, "WallTop", wall, new Vector3(0f, 3.5f, 0f), new Vector2(9f, 0.5f), -19990, true);
            CreateTiled(_stockroomRoot.transform, "WallLeft", wall, new Vector3(-4.5f, 0f, 0f), new Vector2(0.4f, 7f), -19990, true);
            CreateTiled(_stockroomRoot.transform, "WallRight", wall, new Vector3(4.5f, 0f, 0f), new Vector2(0.4f, 7f), -19990, true);
            CreateTiled(_stockroomRoot.transform, "WallBottomLeft", wall, new Vector3(-2.5f, -3.5f, 0f), new Vector2(4f, 0.5f), -19990, true);
            CreateTiled(_stockroomRoot.transform, "WallBottomRight", wall, new Vector3(2.5f, -3.5f, 0f), new Vector2(4f, 0.5f), -19990, true);

            CreateSprite(_stockroomRoot.transform, "ShelfLeft", shelf, new Vector3(-3.6f, 2.6f, 0f),
                new Vector2(2f, 1.2f), true, -950, 1.8f, 0.7f);
            CreateSprite(_stockroomRoot.transform, "ShelfRight", shelf, new Vector3(3.6f, 2.6f, 0f),
                new Vector2(2f, 1.2f), true, -950, 1.8f, 0.7f);
            CreateSprite(_stockroomRoot.transform, "SortingTable", table, new Vector3(0f, 0.4f, 0f),
                new Vector2(4.6f, 1.4f), true, -949, 4.2f, 0.8f);

            _stations = new GameObject[RequiredBoxes];
            _stationPositions = new Vector3[RequiredBoxes];
            _sorted = new bool[RequiredBoxes];
            Vector3[] positions =
            {
                new Vector3(-2.1f, 0.5f, 0f),
                new Vector3(0f, 0.5f, 0f),
                new Vector3(2.1f, 0.5f, 0f)
            };
            for (int i = 0; i < RequiredBoxes; i++)
            {
                _stationPositions[i] = positions[i];
                GameObject station = CreateSprite(_stockroomRoot.transform, "SortBox" + (i + 1), box,
                    positions[i], new Vector2(0.9f, 0.9f), false, -948);
                _stations[i] = station;
                _sorted[i] = false;
            }

            CreateBlock(_stockroomRoot.transform, "Entrance", new Vector3(0f, -3.48f, 0f),
                new Vector2(1.5f, 0.52f), new Color(0.06f, 0.08f, 0.07f), true, -947);
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
