using System.Collections.Generic;
using UnityEngine;

namespace AmberCity.Prototype
{
    public sealed class CookingRecipe
    {
        public string Name { get; private set; }
        public string[] IngredientIds { get; private set; }
        public int BaseReward { get; private set; }

        public CookingRecipe(string name, string[] ingredientIds, int baseReward)
        {
            Name = name;
            IngredientIds = ingredientIds;
            BaseReward = baseReward;
        }

        public bool HasIngredients(PlayerInventory inventory)
        {
            if (inventory == null) return false;
            foreach (string id in IngredientIds)
            {
                if (inventory.CountOf(id) <= 0) return false;
            }
            return true;
        }

        public void Consume(PlayerInventory inventory)
        {
            if (inventory == null) return;
            foreach (string id in IngredientIds)
            {
                inventory.TryRemove(id);
            }
        }

        public string IngredientList()
        {
            List<string> names = new List<string>();
            foreach (string id in IngredientIds) names.Add(ItemCatalog.NameOf(id));
            return string.Join("、", names);
        }
    }

    public static class CookingRecipes
    {
        public static readonly CookingRecipe Burger = new CookingRecipe(
            "汉堡", new[] { "bun", "patty", "lettuce" }, 20);
        public static readonly CookingRecipe Salad = new CookingRecipe(
            "蔬菜沙拉", new[] { "lettuce", "tomato" }, 14);
        public static readonly CookingRecipe Pasta = new CookingRecipe(
            "番茄意面", new[] { "pasta", "tomato", "cheese" }, 18);
        public static readonly CookingRecipe Sandwich = new CookingRecipe(
            "芝士三明治", new[] { "bun", "cheese", "lettuce" }, 15);
        public static readonly CookingRecipe MeatPlate = new CookingRecipe(
            "肉饼拼盘", new[] { "patty", "cheese" }, 16);

        public static readonly CookingRecipe[] All =
        {
            Burger, Salad, Pasta, Sandwich, MeatPlate
        };

        public static CookingRecipe RandomRecipe(CookingRecipe except = null)
        {
            if (All.Length == 1) return All[0];

            CookingRecipe selected = All[Random.Range(0, All.Length)];
            if (except == null || selected != except) return selected;

            int index = System.Array.IndexOf(All, selected);
            return All[(index + 1) % All.Length];
        }
    }

    public static class CookingIngredients
    {
        public static readonly string[] All =
        {
            "bun", "patty", "lettuce", "tomato", "cheese", "pasta"
        };

        public static void ClearFrom(PlayerInventory inventory)
        {
            if (inventory == null) return;
            foreach (string id in All)
            {
                int count = inventory.CountOf(id);
                if (count > 0) inventory.TryRemove(id, count);
            }
        }
    }
}
