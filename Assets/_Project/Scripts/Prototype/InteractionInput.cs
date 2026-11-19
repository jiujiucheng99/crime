using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberCity.Prototype
{
    /// <summary>
    /// One shared F-key consumption per frame. A controller only consumes F when
    /// it actually has an available interaction. The same frame is then blocked
    /// for every other scene/controller, preventing "enter then instant exit".
    /// </summary>
    public static class InteractionInput
    {
        private static int _lastConsumedFrame = -1;
        private static int _transitionFrame = -1;

        public static bool TryConsumeF(bool hasInteraction, bool allowWhileDriving = false)
        {
            if (!hasInteraction) return false;
            if (BusDrivingController.Active != null && !allowWhileDriving) return false;
            if (Time.frameCount == _transitionFrame) return false;
            if (_lastConsumedFrame == Time.frameCount) return false;
            if (Keyboard.current == null || !Keyboard.current.fKey.wasPressedThisFrame) return false;

            _lastConsumedFrame = Time.frameCount;
            _transitionFrame = Time.frameCount;
            return true;
        }

        public static void LockTransition()
        {
            _transitionFrame = Time.frameCount;
        }
    }
}
