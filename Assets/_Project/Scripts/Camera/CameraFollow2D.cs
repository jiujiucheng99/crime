using UnityEngine;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Centers the camera on the player and follows every frame.
    /// The target is the player's foot position; the offset raises the view
    /// to the character's visual center (32x48 sprite => 0.75 units at 32 PPU).
    /// </summary>
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0.75f, -10f);
        [SerializeField] private float smoothTime = 0f;
        [SerializeField] private bool clampToBounds = false;
        [SerializeField] private Vector2 minBounds = new Vector2(-16f, -7f);
        [SerializeField] private Vector2 maxBounds = new Vector2(16f, 7f);

        private Vector3 _velocity;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                transform.position = target.position + offset;
                _velocity = Vector3.zero;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desired = target.position + offset;
            if (clampToBounds)
            {
                desired.x = Mathf.Clamp(desired.x, minBounds.x, maxBounds.x);
                desired.y = Mathf.Clamp(desired.y, minBounds.y, maxBounds.y);
            }

            if (smoothTime <= 0f)
            {
                transform.position = desired;
                _velocity = Vector3.zero;
                return;
            }

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
        }
    }
}
