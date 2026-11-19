using UnityEngine;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Reusable locked-door check. A master key (reserved ID "master_key")
    /// overrides the normal skill/item requirement without being consumed.
    /// The master key item itself is not obtainable yet.
    /// </summary>
    public class DoorLockController : MonoBehaviour
    {
        [SerializeField] private string requiredSkillId = "lockpicking";
        [SerializeField] private int requiredSkillLevel = 1;
        [SerializeField] private string requiredItemId = "paperclip";
        [SerializeField] private int requiredItemCount = 1;
        [SerializeField] private bool consumeItem = true;
        [SerializeField] private string masterKeyId = "master_key";

        private PlayerSkills _skills;
        private PlayerInventory _pockets;
        private DialogueChoiceController _dialogue;

        public bool IsUnlocked { get; private set; }
        public float LastUnlockNoise { get; private set; }

        public void Configure(Transform player, DialogueChoiceController dialogue = null)
        {
            _skills = player != null ? player.GetComponent<PlayerSkills>() : null;
            _pockets = player != null ? player.GetComponent<PlayerInventory>() : null;
            _dialogue = dialogue;
        }

        public bool TryUnlock()
        {
            if (IsUnlocked) return true;

            // Reserved bypass: master key is not obtainable yet.
            if (_pockets != null && _pockets.CountOf(masterKeyId) > 0)
            {
                IsUnlocked = true;
                Notice("万能钥匙打开了门锁。");
                return true;
            }

            if (_skills == null || !_skills.Has(requiredSkillId, requiredSkillLevel))
            {
                Notice("需要开锁技能。");
                return false;
            }

            if (_pockets == null || _pockets.CountOf(requiredItemId) < requiredItemCount)
            {
                Notice("需要回形针。");
                return false;
            }

            if (consumeItem)
            {
                int level = _skills != null ? _skills.LevelOf(requiredSkillId) : 1;
                float saveChance = SkillEffects.LockpickPaperclipSaveChance(level);
                if (Random.value > saveChance)
                {
                    for (int i = 0; i < requiredItemCount; i++)
                    {
                        if (!_pockets.TryRemove(requiredItemId)) break;
                    }
                }
                else
                {
                    Notice("开锁很干净，回形针没有损坏。");
                }
            }

            int lockLevel = _skills != null ? _skills.LevelOf(requiredSkillId) : 1;
            LastUnlockNoise = 25f * SkillEffects.LockpickNoiseMultiplier(lockLevel);
            IsUnlocked = true;
            if (_skills != null) _skills.AddExperience(requiredSkillId, 12);
            Notice("用回形针打开了门锁。");
            return true;
        }

        public void Lock()
        {
            IsUnlocked = false;
        }

        private void Notice(string message)
        {
            if (_dialogue != null) _dialogue.OpenNotice("门锁", message);
        }
    }
}
