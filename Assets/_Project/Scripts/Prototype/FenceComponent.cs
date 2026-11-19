using UnityEngine;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Simple fence point: sells all stolen goods in the player's inventory.
    /// </summary>
    public class FenceComponent : MonoBehaviour
    {
        private PlayerInventory _pockets;
        private PlayerWallet _wallet;
        private DialogueChoiceController _dialogue;

        public void Configure(PlayerInventory pockets, PlayerWallet wallet, DialogueChoiceController dialogue)
        {
            _pockets = pockets;
            _wallet = wallet;
            _dialogue = dialogue;
        }

        public void SellAll()
        {
            if (FenceUI.Instance != null)
            {
                FenceUI.Instance.Open();
                return;
            }

            int total = StolenGoods.SellAll(_pockets, _wallet);
            if (_dialogue == null) return;

            if (total <= 0)
            {
                _dialogue.OpenNotice("酒保", "你身上没有能出手的货。");
                return;
            }

            _dialogue.OpenNotice("酒保", "这些货值 " + total + " 块，钱货两清。");
        }

        public static FenceComponent Create(Transform parent, string objectName,
            PlayerInventory pockets, PlayerWallet wallet, DialogueChoiceController dialogue)
        {
            GameObject go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            FenceComponent fence = go.AddComponent<FenceComponent>();
            fence.Configure(pockets, wallet, dialogue);
            return fence;
        }
    }
}
