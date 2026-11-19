using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmberCity.Prototype
{
    public sealed class PlayerSkills : MonoBehaviour
    {
        public sealed class Skill
        {
            public string Id;
            public string DisplayName;
            public int Level;
            public int Experience;

            public Skill(string id, string displayName, int level)
            {
                Id = id;
                DisplayName = displayName;
                Level = level;
            }

            public int ExperienceForNextLevel => 80 + (Level - 1) * 50;
        }

        private readonly List<Skill> _skills = new List<Skill>();

        public static event Action<string, int> SkillLeveledUp;

        public IReadOnlyList<Skill> Skills => _skills;

        private void Awake()
        {
            _skills.Clear();
            Learn("driving");
        }

        public bool Has(string id, int level = 1)
        {
            Skill skill = Find(id);
            return skill != null && skill.Level >= level;
        }

        public int LevelOf(string id)
        {
            Skill skill = Find(id);
            return skill != null ? skill.Level : 0;
        }

        public int ExperienceOf(string id)
        {
            Skill skill = Find(id);
            return skill != null ? skill.Experience : 0;
        }

        public void AddExperience(string id, int amount)
        {
            if (amount <= 0) return;
            Skill skill = Find(id);
            SkillDefinition definition = SkillCatalog.Get(id);
            if (skill == null || definition == null) return;

            skill.Experience += amount;
            while (skill.Level < definition.MaxLevel &&
                   skill.Experience >= skill.ExperienceForNextLevel)
            {
                skill.Experience -= skill.ExperienceForNextLevel;
                skill.Level++;
                SkillLeveledUp?.Invoke(skill.Id, skill.Level);
            }

            if (skill.Level >= definition.MaxLevel) skill.Experience = 0;
        }

        public bool Learn(string id, int level = 1)
        {
            SkillDefinition definition = SkillCatalog.Get(id);
            if (definition == null) return false;

            Skill skill = Find(id);
            if (skill == null)
            {
                _skills.Add(new Skill(definition.Id, definition.DisplayName,
                    Mathf.Clamp(level, 1, definition.MaxLevel)));
                return true;
            }

            int next = Mathf.Clamp(Mathf.Max(skill.Level, level), 1, definition.MaxLevel);
            if (next == skill.Level) return false;
            skill.Level = next;
            return true;
        }

        public void ClearAll()
        {
            _skills.Clear();
        }

        public void Restore(string id, int level, int experience)
        {
            SkillDefinition definition = SkillCatalog.Get(id);
            if (definition == null) return;
            Skill skill = Find(id);
            if (skill == null)
            {
                skill = new Skill(definition.Id, definition.DisplayName,
                    Mathf.Clamp(level, 1, definition.MaxLevel));
                _skills.Add(skill);
            }
            else
            {
                skill.Level = Mathf.Clamp(level, 1, definition.MaxLevel);
            }
            skill.Experience = Mathf.Max(0, experience);
        }

        private Skill Find(string id)
        {
            foreach (Skill skill in _skills)
            {
                if (skill.Id == id) return skill;
            }
            return null;
        }
    }
}
