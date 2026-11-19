using UnityEngine;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Auto repair shift: three repair tickets, correct part, then four-bolt
    /// timing. Workbench and lifted car are separate from the player's own
    /// vehicle repair service.
    /// </summary>
    public class AutoRepairJobController : MonoBehaviour
    {
        private const int TicketsPerShift = 3;
        private const int BaseWage = 12;
        private const float InteractRange = 1.35f;
        private static readonly Vector2 WorkbenchPoint = new Vector2(6995.5f, 2.3f);
        private static readonly Vector2 CarPoint = new Vector2(7000f, -1.7f);

        private Transform _player;
        private PlayerEmployment _employment;
        private PlayerWallet _wallet;
        private PlayerInventory _inventory;
        private PlayerSkills _skills;
        private DialogueChoiceController _dialogue;
        private PlayerMotor2D _motor;
        private RepairMinigameUI _minigame;
        private RepairTicket[] _tickets;
        private int _ticketIndex;
        private bool _shiftActive;
        private GUIStyle _style;

        public bool IsShiftActive => _shiftActive;

        public void Configure(Transform player, PlayerEmployment employment,
            PlayerWallet wallet, PlayerInventory inventory, PlayerSkills skills,
            DialogueChoiceController dialogue)
        {
            _player = player;
            _employment = employment;
            _wallet = wallet;
            _inventory = inventory;
            _skills = skills;
            _dialogue = dialogue;
            _motor = player != null ? player.GetComponent<PlayerMotor2D>() : null;

            _minigame = gameObject.AddComponent<RepairMinigameUI>();
            _minigame.Configure(player);
        }

        public bool TryGetPrompt(out string prompt)
        {
            prompt = null;
            if (!_shiftActive)
            {
                if (Vector2.Distance(_player.position, WorkbenchPoint) <= InteractRange)
                {
                    prompt = "工单";
                    return true;
                }
                return false;
            }

            if (RepairMinigameUI.IsOpen) return false;
            if (Vector2.Distance(_player.position, CarPoint) <= InteractRange)
            {
                prompt = "维修";
                return true;
            }
            return false;
        }

        public void Interact()
        {
            if (!_shiftActive)
            {
                StartShift();
                return;
            }
            TryRepair();
        }

        public void CancelShift()
        {
            _shiftActive = false;
            RepairParts.ClearFrom(_inventory);
        }

        private void StartShift()
        {
            if (_employment == null || _employment.CurrentJobId != "auto_mechanic")
            {
                if (_dialogue != null) _dialogue.OpenNotice("修车厂", "需要先在修车厂入职技工。");
                return;
            }

            _shiftActive = true;
            _ticketIndex = 0;
            _tickets = RepairTickets.PickThree();
            if (_dialogue != null) _dialogue.OpenNotice("修车厂", "班次开始：完成 3 张维修工单。");
        }

        private void TryRepair()
        {
            if (_tickets == null || _ticketIndex >= _tickets.Length) return;
            RepairTicket ticket = _tickets[_ticketIndex];
            if (_inventory == null || _inventory.CountOf(ticket.PartId) <= 0)
            {
                if (_dialogue != null)
                {
                    _dialogue.OpenNotice("缺少零件", "这张工单需要：" + ticket.PartName + "。去零件架拿一个。");
                }
                return;
            }

            _inventory.TryRemove(ticket.PartId);
            int mechanics = _skills != null ? Mathf.Max(1, _skills.LevelOf("mechanics")) : 1;
            _minigame.Open(ticket, mechanics, OnRepairFinished);
        }

        private void OnRepairFinished(int mistakes)
        {
            int mechanics = _skills != null ? Mathf.Max(1, _skills.LevelOf("mechanics")) : 1;
            int maxBonus = SkillEffects.MechanicsBonusCap(mechanics);
            int bonus = Mathf.Clamp(maxBonus - mistakes, 0, maxBonus);
            int wage = BaseWage + bonus;
            if (_wallet != null) _wallet.Add(wage);
            if (_skills != null) _skills.AddExperience("mechanics", Mathf.Max(8, 20 - mistakes * 3));
            _ticketIndex++;

            if (_ticketIndex >= TicketsPerShift)
            {
                _shiftActive = false;
                RepairParts.ClearFrom(_inventory);
                if (_dialogue != null) _dialogue.OpenNotice("修车厂", "三张工单完成，本班次结束。");
                return;
            }

            if (_dialogue != null)
            {
                _dialogue.OpenNotice("维修完成", "本单工资 " + wage + " 块。下一张：" + _tickets[_ticketIndex].Symptom);
            }        }

        private void OnGUI()
        {
            if (!_shiftActive || RepairMinigameUI.IsOpen || _tickets == null) return;
            if (_style == null)
            {
                Font font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Microsoft YaHei", "Microsoft YaHei UI", "SimHei", "Noto Sans CJK SC", "Arial" }, 16);
                _style = new GUIStyle(GUI.skin.label)
                {
                    font = font, fontSize = 15, alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.95f, 0.89f, 0.72f) }
                };
            }

            RepairTicket ticket = _tickets[Mathf.Min(_ticketIndex, _tickets.Length - 1)];
            Rect rect = new Rect(Screen.width * 0.5f - 250f, 18f, 500f, 30f);
            Color previous = GUI.color;
            GUI.color = new Color(0.04f, 0.05f, 0.06f, 0.9f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(0.62f, 0.52f, 0.32f, 1f);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Label(rect, "维修工单 " + (_ticketIndex + 1) + " / " + TicketsPerShift +
                "   " + ticket.Symptom + "   需要：" + ticket.PartName, _style);
        }
    }
}
