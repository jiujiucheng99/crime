using System.Collections.Generic;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Static skill metadata. Skills are learned at runtime through PlayerSkills.
    /// </summary>
    public sealed class SkillDefinition
    {
        public string Id { get; private set; }
        public string DisplayName { get; private set; }
        public int MaxLevel { get; private set; }
        public string AcquiredAt { get; private set; }
        public string EffectDescription { get; private set; }

        public SkillDefinition(string id, string displayName, int maxLevel, string acquiredAt,
            string effectDescription)
        {
            Id = id;
            DisplayName = displayName;
            MaxLevel = maxLevel;
            AcquiredAt = acquiredAt;
            EffectDescription = effectDescription;
        }
    }

    public static class SkillCatalog
    {
        private static readonly Dictionary<string, SkillDefinition> Skills =
            new Dictionary<string, SkillDefinition>
            {
                { "driving", new SkillDefinition("driving", "开车", 5, "初始", "暂未启用等级效果") },
                { "lockpicking", new SkillDefinition("lockpicking", "开锁", 5, "酒吧瘦猴", "降低撬门噪音，高等级节省回形针") },
                { "cooking", new SkillDefinition("cooking", "烹饪", 5, "餐厅入职", "扩大烹饪绿色区域") },
                { "mechanics", new SkillDefinition("mechanics", "机械", 5, "修车厂入职", "扩大维修绿色区域，提高工单奖励") },
                { "logistics", new SkillDefinition("logistics", "物流", 5, "仓库入职", "提高搬运速度，提高班次奖励") }
            };

        public static SkillDefinition Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            Skills.TryGetValue(id, out SkillDefinition definition);
            return definition;
        }

        public static IEnumerable<SkillDefinition> All => Skills.Values;
    }
}
