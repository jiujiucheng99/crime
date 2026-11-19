using UnityEngine;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Fixed Brick Row bus loop. Stations use a visible stop box; the player must
    /// stop inside it for 10 seconds without any forced braking.
    /// </summary>
    public class BusRouteController : MonoBehaviour
    {
        private const float StopBoxSize = 5.4f;
        private const float StopSeconds = 10f;
        private const float StopSpeed = 0.2f;
        private const int Wage = 20;

        private sealed class Station
        {
            public string Name;
            public Vector2 Position;

            public Station(string name, Vector2 position)
            {
                Name = name;
                Position = position;
            }
        }

        private static readonly Station[] Stations =
        {
            new Station("运输中心", new Vector2(44f, -21.8f)),
            new Station("港口", new Vector2(88f, -42.2f)),
            new Station("南岸仓库", new Vector2(66f, -42.2f)),
            new Station("便利店", new Vector2(0f, -42.2f)),
            new Station("运输中心", new Vector2(44f, -21.8f))
        };

        private BusDrivingController _bus;
        private PlayerWallet _wallet;
        private PlayerSkills _skills;
        private DialogueChoiceController _dialogue;
        private GameObject _stopBox;
        private int _nextIndex;
        private float _holdTimer;
        private bool _insideStop;
        private bool _active;
        private GUIStyle _style;
        private Sprite _whiteSprite;

        public bool IsActive => _active;
        public bool IsHolding => _insideStop;

        public void Configure(BusDrivingController bus, PlayerWallet wallet,
            DialogueChoiceController dialogue = null, PlayerSkills skills = null)
        {
            _bus = bus;
            _wallet = wallet;
            _dialogue = dialogue;
            _skills = skills;
        }

        public void Begin()
        {
            _active = true;
            _insideStop = false;
            _holdTimer = 0f;
            _nextIndex = 1;
            ShowStopBox();
        }

        public void Stop()
        {
            _active = false;
            _insideStop = false;
            _holdTimer = 0f;
            if (_stopBox != null) _stopBox.SetActive(false);
        }

        private void Update()
        {
            if (!_active || _bus == null || !_bus.IsDriving) return;
            Transform busTransform = _bus.BusTransform;
            if (busTransform == null) return;

            Station station = Stations[_nextIndex];
            Vector2 delta = (Vector2)busTransform.position - station.Position;
            bool inside = Mathf.Abs(delta.x) <= StopBoxSize * 0.5f &&
                          Mathf.Abs(delta.y) <= StopBoxSize * 0.5f;

            if (!inside)
            {
                _insideStop = false;
                _holdTimer = 0f;
                return;
            }

            _insideStop = true;
            if (Mathf.Abs(_bus.Speed) > StopSpeed)
            {
                // The player is still moving inside the box; do not count and
                // do not force the bus to stop.
                _holdTimer = 0f;
                return;
            }

            _holdTimer += Time.deltaTime;
            if (_holdTimer >= StopSeconds) CompleteStop();
        }

        private void CompleteStop()
        {
            _insideStop = false;
            _holdTimer = 0f;

            if (_nextIndex >= Stations.Length - 1)
            {
                _active = false;
                if (_wallet != null) _wallet.Add(Wage);
                if (_dialogue != null)
                {
                    _dialogue.OpenNotice("公交线路", "本班次完成，工资 " + Wage + " 块。");
                }
                if (_stopBox != null) _stopBox.SetActive(false);
                return;
            }

            _nextIndex++;
            ShowStopBox();
        }

        private void ShowStopBox()
        {
            if (_stopBox == null) BuildStopBox();
            if (_stopBox == null) return;
            _stopBox.transform.position = Stations[_nextIndex].Position;
            _stopBox.SetActive(true);
        }

        private void BuildStopBox()
        {
            _stopBox = new GameObject("BusStopBox");
            float half = StopBoxSize * 0.5f;
            float thickness = 0.12f;

            CreateEdge("Top", new Vector3(0f, half, 0f), new Vector2(StopBoxSize, thickness), new Color(1f, 0.78f, 0.2f, 0.95f));
            CreateEdge("Bottom", new Vector3(0f, -half, 0f), new Vector2(StopBoxSize, thickness), new Color(1f, 0.78f, 0.2f, 0.95f));
            CreateEdge("Left", new Vector3(-half, 0f, 0f), new Vector2(thickness, StopBoxSize), new Color(1f, 0.78f, 0.2f, 0.95f));
            CreateEdge("Right", new Vector3(half, 0f, 0f), new Vector2(thickness, StopBoxSize), new Color(1f, 0.78f, 0.2f, 0.95f));
            CreateEdge("Fill", Vector3.zero, new Vector2(StopBoxSize - thickness, StopBoxSize - thickness), new Color(1f, 0.78f, 0.2f, 0.08f));
        }

        private void CreateEdge(string name, Vector3 localPosition, Vector2 size, Color color)
        {
            if (_whiteSprite == null) _whiteSprite = CreateWhiteSprite();

            GameObject edge = new GameObject(name);
            edge.transform.SetParent(_stopBox.transform, false);
            edge.transform.localPosition = localPosition;
            edge.transform.localScale = new Vector3(size.x, size.y, 1f);

            SpriteRenderer renderer = edge.AddComponent<SpriteRenderer>();
            renderer.sprite = _whiteSprite;
            renderer.color = color;
            renderer.sortingOrder = -10940;
        }

        private static Sprite CreateWhiteSprite()
        {
            Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            sprite.name = "BusStopBoxWhite";
            return sprite;
        }

        private void OnGUI()
        {
            if (!_active || _bus == null || !_bus.IsDriving) return;
            if (_style == null)
            {
                Font font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Microsoft YaHei", "Microsoft YaHei UI", "SimHei", "Noto Sans CJK SC", "Arial" }, 16);
                _style = new GUIStyle(GUI.skin.label)
                {
                    font = font,
                    fontSize = 15,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.95f, 0.89f, 0.72f) }
                };
            }

            Station station = Stations[_nextIndex];
            string text;
            if (_insideStop)
            {
                if (Mathf.Abs(_bus.Speed) > StopSpeed)
                {
                    text = "请停稳：" + station.Name;
                }
                else
                {
                    text = "停靠中：" + station.Name + "  " + _holdTimer.ToString("F1") + " / 10 秒";
                }
            }
            else
            {
                float distance = Vector2.Distance(_bus.BusTransform.position, station.Position);
                text = "下一站：" + station.Name + "   驶入黄色框内  " + distance.ToString("F0");
            }

            Rect rect = new Rect(Screen.width * 0.5f - 230f, 18f, 460f, 30f);
            Color previous = GUI.color;
            GUI.color = new Color(0.04f, 0.05f, 0.06f, 0.9f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(0.62f, 0.52f, 0.32f, 1f);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Label(rect, text, _style);
        }
    }
}
