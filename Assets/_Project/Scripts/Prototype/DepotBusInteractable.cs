using UnityEngine;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Bus interaction inside the depot. The driving controller will be added later;
    /// for now this confirms the bus and returns the player to the transport centre door.
    /// </summary>
    public class DepotBusInteractable : MonoBehaviour
    {
        private static DepotBusInteractable _promptOwner;

        private Transform _player;
        private DialogueChoiceController _dialogue;
        private PlayerEmployment _employment;
        private TransportDepotController _depot;
        private bool _near;
        private string _prompt;

        public void Configure(Transform player, DialogueChoiceController dialogue,
            PlayerEmployment employment, TransportDepotController depot)
        {
            _player = player;
            _dialogue = dialogue;
            _employment = employment;
            _depot = depot;
        }

        private void Update()
        {
            if (_player == null || _depot == null || !_depot.IsInside ||
                DialogueChoiceController.IsOpen || StorageInteraction.AnyPanelOpen)
            {
                _near = false;
                _prompt = string.Empty;
                return;
            }

            _near = Vector2.Distance(_player.position, transform.position) <= 2.4f;
            _prompt = _near ? "公交车" : string.Empty;

            if (InteractionInput.TryConsumeF(_near)) TryDrive();
        }

        private void TryDrive()
        {
            if (_employment == null || _employment.CurrentJobId != "transport_staff")
            {
                if (_dialogue != null) _dialogue.OpenNotice("公交车", "需要运输中心的工作才能驾驶。");
                return;
            }

            if (_dialogue == null) return;
            _dialogue.Open("公交车", "要驾驶这辆公交车吗？", "发车", () =>
            {
                if (_depot != null) _depot.BeginDriveFromBus();
            });
        }

        private void OnGUI()
        {
            if (!string.IsNullOrEmpty(_prompt))
            {
                _promptOwner = this;
                InteractionPromptUI.SetPrompt("busdrive", _prompt);
            }
            else if (_promptOwner == this)
            {
                _promptOwner = null;
                InteractionPromptUI.SetPrompt("busdrive", string.Empty);
            }
        }
    }
}
