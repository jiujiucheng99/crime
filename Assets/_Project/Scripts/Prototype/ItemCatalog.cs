using System.Collections.Generic;
using UnityEngine;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Central item metadata. Gameplay code should use item IDs and this catalog
    /// instead of repeating display names and base values.
    /// </summary>
    public sealed class ItemDefinition
    {
        public string Id { get; private set; }
        public string DisplayName { get; private set; }
        public int BaseValue { get; private set; }
        public string Category { get; private set; }

        public ItemDefinition(string id, string displayName, int baseValue, string category)
        {
            Id = id;
            DisplayName = displayName;
            BaseValue = baseValue;
            Category = category;
        }
    }

    public static class ItemCatalog
    {
        private static readonly Dictionary<string, ItemDefinition> Items =
            new Dictionary<string, ItemDefinition>
            {
                { "test_item", new ItemDefinition("test_item", "TEST ITEM", 0, "misc") },
                { "contraband", new ItemDefinition("contraband", "CONTRABAND", 0, "misc") },
                { "first_aid", new ItemDefinition("first_aid", "急救包", 5, "utility") },
                { "flashlight", new ItemDefinition("flashlight", "手电筒", 2, "utility") },
                { "wrench", new ItemDefinition("wrench", "扳手", 3, "tool") },
                { "gloves", new ItemDefinition("gloves", "手套", 1, "utility") },
                { "paperclip", new ItemDefinition("paperclip", "回形针", 1, "tool") },
                { "bun", new ItemDefinition("bun", "面包", 0, "ingredient") },
                { "patty", new ItemDefinition("patty", "肉饼", 0, "ingredient") },
                { "lettuce", new ItemDefinition("lettuce", "生菜", 0, "ingredient") },
                { "tomato", new ItemDefinition("tomato", "番茄", 0, "ingredient") },
                { "cheese", new ItemDefinition("cheese", "奶酪", 0, "ingredient") },
                { "pasta", new ItemDefinition("pasta", "意面", 0, "ingredient") },
                { "tire", new ItemDefinition("tire", "轮胎", 0, "part") },
                { "battery", new ItemDefinition("battery", "电瓶", 0, "part") },
                { "brake_pads", new ItemDefinition("brake_pads", "刹车片", 0, "part") },
                { "spark_plug", new ItemDefinition("spark_plug", "火花塞", 0, "part") },
                { "watch", new ItemDefinition("watch", "手表", 18, "loot") },
                { "jewelry", new ItemDefinition("jewelry", "首饰", 32, "loot") },
                { "electronics", new ItemDefinition("electronics", "电子产品", 24, "loot") },
                { "collectible", new ItemDefinition("collectible", "收藏品", 28, "loot") }
            };

        public static ItemDefinition Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            Items.TryGetValue(id, out ItemDefinition definition);
            return definition;
        }

        public static string NameOf(string id)
        {
            ItemDefinition definition = Get(id);
            return definition != null ? definition.DisplayName : id;
        }

        public static int ValueOf(string id)
        {
            ItemDefinition definition = Get(id);
            return definition != null ? definition.BaseValue : 0;
        }

        public static ItemStack CreateStack(string id, int count = 1)
        {
            ItemDefinition definition = Get(id);
            string name = definition != null ? definition.DisplayName : id;
            return new ItemStack(id, name, count);
        }

        public static IEnumerable<ItemDefinition> All => Items.Values;
    }
}
