using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Top-down bus driving mode. W throttle, S reverse/brake, A/D steering,
    /// Space brake, F exit.
    /// </summary>
    public class BusDrivingController : MonoBehaviour
    {
        private const float MaxForwardSpeed = 7.5f;
        private const float MaxReverseSpeed = 3f;
        private const float Acceleration = 5.5f;
        private const float ReverseAcceleration = 3.5f;
        private const float Brake = 9f;
        private const float Drag = 2.5f;
        private const float TurnRate = 75f;
        private const float StartHeading = -90f;
        private const float DirectionSwitchMargin = 3f;

        private Transform _player;
        private CameraFollow2D _cameraFollow;
        private GameObject _bus;
        private Transform _visual;
        private SpriteRenderer _renderer;
        private BoxCollider2D _collider;
        private Rigidbody2D _body;
        private Vector2 _direction;
        private float _speed;
        private int _visualIndex = -1;

        private static readonly string[] DirectionSprites =
        {
            "bus_east", "bus_ne", "bus_north", "bus_nw",
            "bus_west", "bus_sw", "bus_south", "bus_se"
        };
        private float _heading;
        private bool _driving;
        private bool _routeHold;
        private int _startFrame;
        private GUIStyle _hudStyle;
        private BusRouteController _route;

        public bool IsDriving => _driving;
        public float Speed => _speed;
        public Transform BusTransform => _bus != null ? _bus.transform : null;
        public static BusDrivingController Active { get; private set; }

        public void Configure(Transform player)
        {
            _player = player;
            Camera camera = Camera.main;
            _cameraFollow = camera != null ? camera.GetComponent<CameraFollow2D>() : null;
        }

        public void SetRoute(BusRouteController route)
        {
            _route = route;
        }

        public void SetRouteHold(bool hold)
        {
            _routeHold = hold;
            if (hold) _speed = 0f;
        }

        public void Begin(Vector3 position, float heading = StartHeading)
        {
            if (_driving) return;
            _driving = true;
            Active = this;
            _speed = 0f;
            _routeHold = false;
            _heading = heading;
            _startFrame = Time.frameCount;

            BuildBus(position);
            if (_player != null)
            {
                _player.position = position;
                _player.gameObject.SetActive(false);
            }
            if (_cameraFollow != null && _bus != null) _cameraFollow.SetTarget(_bus.transform);
            if (_route != null) _route.Begin();
        }

        private void Update()
        {
            if (!_driving || _bus == null) return;
            if (Time.frameCount == _startFrame) return;

            if (InteractionInput.TryConsumeF(true, true))
            {
                ExitBus();
                return;
            }

            if (_routeHold)
            {
                _speed = 0f;
                UpdateVisual(HeadingDirection(_heading));
                return;
            }

            float throttle = (Keyboard.current != null && Keyboard.current.wKey.isPressed) ? 1f : 0f;
            float reverse = (Keyboard.current != null && Keyboard.current.sKey.isPressed) ? 1f : 0f;
            float brake = (Keyboard.current != null && Keyboard.current.spaceKey.isPressed) ? 1f : 0f;
            float steer = 0f;
            if (Keyboard.current != null && Keyboard.current.aKey.isPressed) steer += 1f;
            if (Keyboard.current != null && Keyboard.current.dKey.isPressed) steer -= 1f;

            float dt = Time.deltaTime;
            if (throttle > 0f) _speed += Acceleration * dt;
            else if (reverse > 0f)
            {
                if (_speed > 0.05f) _speed -= Brake * dt;
                else _speed -= ReverseAcceleration * dt;
            }
            else _speed = Mathf.MoveTowards(_speed, 0f, Drag * dt);

            if (brake > 0f) _speed = Mathf.MoveTowards(_speed, 0f, Brake * dt);
            _speed = Mathf.Clamp(_speed, -MaxReverseSpeed, MaxForwardSpeed);

            if (Mathf.Abs(_speed) > 0.05f)
            {
                _heading += steer * TurnRate * dt * Mathf.Sign(_speed);
            }

            Vector2 direction = HeadingDirection(_heading);
            _direction = direction;
            UpdateVisual(direction);
            _renderer.sortingOrder = Mathf.RoundToInt(-_bus.transform.position.y * 100f);
        }

        private void FixedUpdate()
        {
            if (!_driving || _body == null) return;
            _body.velocity = _direction * _speed;
            _body.rotation = _heading + 90f;
            _body.angularVelocity = 0f;
        }

        private static Vector2 HeadingDirection(float heading)
        {
            float rad = heading * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        private void UpdateVisual(Vector2 direction)
        {
            if (_renderer == null || _collider == null || _bus == null || _visual == null) return;

            float angle = Mathf.Repeat(_heading, 360f);
            int index = ResolveVisualIndex(angle);
            if (index != _visualIndex)
            {
                Sprite sprite = Resources.Load<Sprite>(
                    "Environment/BrickRow/Transport/Vehicles/" + DirectionSprites[index]);
                if (sprite != null)
                {
                    _renderer.sprite = sprite;
                    _visualIndex = index;
                }
            }

            // The root body points its local down toward the heading. Each view
            // has its own base direction, so counter-rotate the visual child.
            float baseAngle = index * 45f;
            _visual.localRotation = Quaternion.Euler(0f, 0f, -baseAngle - 90f);
            _collider.size = new Vector2(2.35f, 4.1f);
        }

        private int ResolveVisualIndex(float angle)
        {
            if (_visualIndex < 0) return Mathf.RoundToInt(angle / 45f) % DirectionSprites.Length;

            // Keep the current view inside a slightly widened sector. The next
            // view is selected only after the bus has clearly crossed 22.5°.
            float currentBase = _visualIndex * 45f;
            float delta = Mathf.Abs(Mathf.DeltaAngle(currentBase, angle));
            if (delta <= 22.5f + DirectionSwitchMargin) return _visualIndex;

            return Mathf.RoundToInt(angle / 45f) % DirectionSprites.Length;
        }

        private void BuildBus(Vector3 position)
        {
            if (_bus != null) Destroy(_bus);
            _bus = new GameObject("DrivenBus");
            _bus.transform.position = position;

            GameObject visualObject = new GameObject("Visual");
            visualObject.transform.SetParent(_bus.transform, false);
            _visual = visualObject.transform;
            _renderer = visualObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = Resources.Load<Sprite>("Environment/BrickRow/Transport/Vehicles/bus_south");
            _renderer.sortingOrder = Mathf.RoundToInt(-position.y * 100f);
            _visualIndex = -1;

            _body = _bus.AddComponent<Rigidbody2D>();
            _body.gravityScale = 0f;
            _body.mass = 8f;
            _body.freezeRotation = true;
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
            _bus.transform.rotation = Quaternion.Euler(0f, 0f, _heading + 90f);

            _collider = _bus.AddComponent<BoxCollider2D>();
            _collider.size = new Vector2(2.35f, 4.1f);
        }

        private void ExitBus()
        {
            if (!_driving) return;
            _driving = false;
            if (Active == this) Active = null;
            if (_route != null) _route.Stop();
            Vector3 exitPosition = _bus.transform.position + new Vector3(0f, -2.8f, 0f);

            if (_player != null)
            {
                _player.position = exitPosition;
                _player.gameObject.SetActive(true);
                PlayerMotor2D motor = _player.GetComponent<PlayerMotor2D>();
                if (motor != null) motor.SetInputEnabled(true);
                Rigidbody2D body = _player.GetComponent<Rigidbody2D>();
                if (body != null) body.velocity = Vector2.zero;
            }

            if (_cameraFollow != null && _player != null) _cameraFollow.SetTarget(_player);

            Destroy(_bus);
            _bus = null;
            _speed = 0f;
        }

        private void OnGUI()
        {
            if (!_driving) return;
            if (_hudStyle == null)
            {
                Font font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Microsoft YaHei", "Microsoft YaHei UI", "SimHei", "Noto Sans CJK SC", "Arial" }, 15);
                _hudStyle = new GUIStyle(GUI.skin.label)
                {
                    font = font,
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.95f, 0.89f, 0.72f) }
                };
            }

            Rect rect = new Rect(Screen.width * 0.5f - 210f, Screen.height - 78f, 420f, 28f);
            Color previous = GUI.color;
            GUI.color = new Color(0.04f, 0.05f, 0.06f, 0.9f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Label(rect, "BUS  " + _speed.ToString("F1") + "   W油门  S倒车  A/D转向  Space刹车  F下车", _hudStyle);
        }
    }
}
