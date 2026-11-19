using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AmberCity.Prototype
{
    [Serializable]
    public sealed class SaveData
    {
        public int money;
        public List<SkillSave> skills = new List<SkillSave>();
        public List<ItemSave> items = new List<ItemSave>();
        public string jobId;
        public string jobName;
        public string workplace;
        public bool lockpickTeacherMet;
        public bool owesFavor;
        public bool neighborReported;
        public bool policeArrived;
        public bool gangContacted;
        public bool gangResolved;
        public bool gangHostile;
    }

    [Serializable]
    public sealed class SkillSave
    {
        public string id;
        public int level;
        public int experience;
    }

    [Serializable]
    public sealed class ItemSave
    {
        public int slot;
        public string id;
        public string displayName;
        public int count;
    }

    /// <summary>
    /// Minimal player-progress save. World/interior state and time are not saved
    /// yet; this is intentionally focused on progression and inventory.
    /// </summary>
    public static class SaveGameService
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "amber_city_save.json");

        public static bool HasSave => File.Exists(SavePath);

        public static void Save(Transform player)
        {
            if (player == null) return;
            PlayerWallet wallet = player.GetComponent<PlayerWallet>();
            PlayerSkills skills = player.GetComponent<PlayerSkills>();
            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            PlayerEmployment employment = player.GetComponent<PlayerEmployment>();

            SaveData data = new SaveData
            {
                money = wallet != null ? wallet.Money : 0,
                jobId = employment != null ? employment.CurrentJobId : null,
                jobName = employment != null ? employment.CurrentJobName : null,
                workplace = employment != null ? employment.CurrentWorkplace : null,
                lockpickTeacherMet = CrimeStoryState.LockpickTeacherMet,
                owesFavor = CrimeStoryState.OwesFavor,
                neighborReported = CrimeStoryState.NeighborReported,
                policeArrived = CrimeStoryState.PoliceArrived,
                gangContacted = CrimeStoryState.GangContacted,
                gangResolved = CrimeStoryState.GangResolved,
                gangHostile = CrimeStoryState.GangHostile
            };

            if (skills != null)
            {
                foreach (PlayerSkills.Skill skill in skills.Skills)
                {
                    data.skills.Add(new SkillSave
                    {
                        id = skill.Id,
                        level = skill.Level,
                        experience = skill.Experience
                    });
                }
            }

            if (inventory != null)
            {
                for (int i = 0; i < PlayerInventory.TotalSize; i++)
                {
                    ItemStack stack = inventory.GetSlot(i);
                    if (stack == null) continue;
                    data.items.Add(new ItemSave
                    {
                        slot = i,
                        id = stack.Id,
                        displayName = stack.DisplayName,
                        count = stack.Count
                    });
                }
            }

            string directory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        }

        public static bool Load(Transform player)
        {
            if (player == null || !HasSave) return false;

            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            }
            catch
            {
                return false;
            }
            if (data == null) return false;

            PlayerWallet wallet = player.GetComponent<PlayerWallet>();
            PlayerSkills skills = player.GetComponent<PlayerSkills>();
            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            PlayerEmployment employment = player.GetComponent<PlayerEmployment>();

            if (wallet != null) wallet.SetMoney(data.money);
            if (skills != null)
            {
                skills.ClearAll();
                foreach (SkillSave entry in data.skills)
                {
                    skills.Restore(entry.id, entry.level, entry.experience);
                }
            }
            if (inventory != null)
            {
                inventory.ClearAll();
                foreach (ItemSave entry in data.items)
                {
                    inventory.SetSlot(entry.slot, new ItemStack(entry.id, entry.displayName, entry.count));
                }
            }
            if (employment != null) employment.Restore(data.jobId, data.jobName, data.workplace);

            CrimeStoryState.LockpickTeacherMet = data.lockpickTeacherMet;
            CrimeStoryState.OwesFavor = data.owesFavor;
            CrimeStoryState.NeighborReported = data.neighborReported;
            CrimeStoryState.PoliceArrived = data.policeArrived;
            CrimeStoryState.GangContacted = data.gangContacted;
            CrimeStoryState.GangResolved = data.gangResolved;
            CrimeStoryState.GangHostile = data.gangHostile;
            TheftNoise.Reset();
            return true;
        }

        public static void Delete()
        {
            if (HasSave) File.Delete(SavePath);
        }
    }
}
