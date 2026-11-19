using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Temporary H8 bus stop. It opens the same two-stop travel UI as the
    /// transport centre reception desk.
    /// </summary>
    public class BusStopController : MonoBehaviour
    {
        private Transform _player;
        private BusTravelController _travel;
        private Vector2 _stopPoint;
        private string _prompt;

        public void Configure(Transform player, BusTravelController travel, Vector2 stopPoint)
        {
            _player = player;
            _travel = travel;
            _stopPoint = stopPoint;
        }

        private void Update()
        {
            if (_player == null || _travel == null || StorageInteraction.AnyPanelOpen || BusTravelController.IsOpen)
            {
                _prompt = string.Empty;
                return;
            }

            bool near = Vector2.Distance(_player.position, _stopPoint) <= 1.3f;
            _prompt = near ? "公交站" : string.Empty;

            if (InteractionInput.TryConsumeF(near))
            {
                _travel.Open("H8");
            }
        }

        private void OnGUI()
        {
            InteractionPromptUI.SetPrompt("busstop", _prompt);
        }
    }
}
