using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Minimal top-down movement for the first playable test.
    /// WASD moves, Shift sprints. No combat, no jumping, no vehicles.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMotor2D : MonoBehaviour
    {
        [Header("Speed")]
        [SerializeField] private float walkSpeed = 3.2f;
        [SerializeField] private float sprintSpeed = 10f;
        [SerializeField] private float acceleration = 28f;
        [SerializeField] private float deceleration = 36f;

        public Vector2 MoveInput { get; private set; }
        public Vector2 Velocity => _body != null ? _body.velocity : Vector2.zero;
        public bool IsSprinting { get; private set; }
        public float SpeedMultiplier { get; set; } = 1f;

        private Rigidbody2D _body;
        private bool _inputEnabled = true;

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled)
            {
                MoveInput = Vector2.zero;
                IsSprinting = false;
            }
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.gravityScale = 0f;
            _body.freezeRotation = true;
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void Update()
        {
            if (!_inputEnabled)
            {
                MoveInput = Vector2.zero;
                IsSprinting = false;
                return;
            }

            ReadInput();
        }

        private void FixedUpdate()
        {
            float targetSpeed = (IsSprinting ? sprintSpeed : walkSpeed) * Mathf.Clamp(SpeedMultiplier, 0.2f, 2f);
            Vector2 targetVelocity = MoveInput * targetSpeed;
            float rate = MoveInput.sqrMagnitude > 0.001f ? acceleration : deceleration;
            _body.velocity = Vector2.MoveTowards(_body.velocity, targetVelocity, rate * Time.fixedDeltaTime);
        }

        private void ReadInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                MoveInput = Vector2.zero;
                IsSprinting = false;
                return;
            }

            float x = 0f;
            float y = 0f;
            if (keyboard.aKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed) x += 1f;
            if (keyboard.sKey.isPressed) y -= 1f;
            if (keyboard.wKey.isPressed) y += 1f;

            MoveInput = new Vector2(x, y).normalized;
            IsSprinting = keyboard.leftShiftKey.isPressed && MoveInput.sqrMagnitude > 0.01f;
        }
    }
}
