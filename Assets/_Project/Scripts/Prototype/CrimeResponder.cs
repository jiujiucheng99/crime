using UnityEngine;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Simple police officer or gang collector who arrives after a burglary.
    /// They use story dialogue rather than combat or a wanted level.
    /// </summary>
    public class CrimeResponder : MonoBehaviour
    {
        public enum ResponderKind
        {
            Police,
            Gang
        }

        private const float InteractRange = 1.4f;

        private ResponderKind _kind;
        private Transform _player;
        private DialogueChoiceController _dialogue;
        private PlayerWallet _wallet;
        private PlayerInventory _inventory;
        private string _prompt;

        public void Configure(ResponderKind kind, Transform player, DialogueChoiceController dialogue,
            PlayerWallet wallet, PlayerInventory inventory)
        {
            _kind = kind;
            _player = player;
            _dialogue = dialogue;
            _wallet = wallet;
            _inventory = inventory;
        }

        private void Update()
        {
            if (_player == null) return;
            if (DialogueChoiceController.IsOpen || StorageInteraction.AnyPanelOpen ||
                BusTravelController.IsOpen || StoreShopUI.IsOpen)
            {
                _prompt = string.Empty;
                return;
            }

            bool near = Vector2.Distance(_player.position, transform.position) <= InteractRange;
            _prompt = near ? (_kind == ResponderKind.Police ? "警察" : "灰庭手下") : string.Empty;
            if (near && InteractionInput.TryConsumeF(true))
            {
                OpenDialogue();
            }
        }

        private void OnGUI()
        {
            InteractionPromptUI.SetPrompt("crime", _prompt);
        }

        private void OnDestroy()
        {
            InteractionPromptUI.SetPrompt("crime", string.Empty);
        }

        private void OnDisable()
        {
            InteractionPromptUI.SetPrompt("crime", string.Empty);
        }

        private void OpenDialogue()
        {
            if (_dialogue == null) return;
            if (_kind == ResponderKind.Police)
            {
                CrimeStoryState.PoliceArrived = true;
                _dialogue.OpenStory("警察",
                    "有人报警说这户人家被翻过。\n" +
                    "你在这附近做什么？有没有看到什么人？\n" +
                    "别紧张，我只是例行问话。",
                    "我只是路过", Dismiss,
                    "我什么都不知道", Dismiss,
                    "我听到过动静", MentionNoise);
                return;
            }

            CrimeStoryState.GangContacted = true;
            _dialogue.OpenStory("灰庭手下",
                "你在不该动手的街区动了手。\n" +
                "邻居报警之前，酒吧这边就已经知道了。\n" +
                "现在有两个选择：给钱，给货，或者欠灰庭一个人情。",
                "交 20 块", PayOff,
                "把赃物交出来", HandOverGoods,
                "我欠灰庭一个人情", OweFavor);
        }

        private void Dismiss()
        {
            if (_dialogue != null) _dialogue.OpenNotice("警察", "警察记下了你的样子，暂时离开了。");
            Destroy(gameObject);
        }

        private void MentionNoise()
        {
            if (_dialogue != null)
            {
                _dialogue.OpenStory("警察",
                    "你听到动静，却没有报警？\n" +
                    "这条街最近不太平。要是再想起什么，来找我。\n" +
                    "下次别在晚上到处乱走。",
                    "知道了", Dismiss,
                    "离开", Dismiss,
                    null, null);
            }
        }

        private void PayOff()
        {
            if (_wallet == null || !_wallet.TrySpend(20))
            {
                if (_dialogue != null) _dialogue.OpenNotice("灰庭手下", "你连 20 块都拿不出来？那就别怪我不客气。");
                return;
            }

            CrimeStoryState.GangResolved = true;
            if (_dialogue != null) _dialogue.OpenNotice("灰庭手下", "算你识相。今晚的事，灰庭暂时不追究。");
            Destroy(gameObject);
        }

        private void HandOverGoods()
        {
            if (_inventory == null || StolenGoods.CountStolen(_inventory) <= 0)
            {
                if (_dialogue != null) _dialogue.OpenNotice("灰庭手下", "你身上没有货。别拿我寻开心。");
                return;
            }

            StolenGoods.RemoveAll(_inventory);
            CrimeStoryState.GangResolved = true;
            if (_dialogue != null) _dialogue.OpenNotice("灰庭手下", "货我带走。以后动手之前，先问问这是谁的地盘。");
            Destroy(gameObject);
        }

        private void OweFavor()
        {
            CrimeStoryState.OwesFavor = true;
            CrimeStoryState.GangResolved = true;
            if (_dialogue != null) _dialogue.OpenNotice("灰庭手下", "记住，你欠灰庭一个人情。会有人来收的。");
            Destroy(gameObject);
        }
    }
}
