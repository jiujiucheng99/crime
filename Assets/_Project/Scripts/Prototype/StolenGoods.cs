using System.Collections.Generic;
using UnityEngine;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Shared definition and sale values for neighbour loot. Item names/values
    /// come from ItemCatalog.
    /// </summary>
    public static class StolenGoods
    {
        private static readonly string[] LootIds =
        {
            "watch", "jewelry", "electronics", "collectible"
        };

        public static ItemStack CreateRandomStack()
        {
            string id = LootIds[Random.Range(0, LootIds.Length)];
            return ItemCatalog.CreateStack(id);
        }

        public static bool IsStolen(string id)
        {
            foreach (string lootId in LootIds)
            {
                if (lootId == id) return true;
            }
            return false;
        }

        public static int ValueOf(string id)
        {
            return IsStolen(id) ? ItemCatalog.ValueOf(id) : 0;
        }

        public static int CountStolen(PlayerInventory inventory)
        {
            if (inventory == null) return 0;
            int total = 0;
            for (int i = 0; i < PlayerInventory.TotalSize; i++)
            {
                ItemStack stack = inventory.GetSlot(i);
                if (stack != null && IsStolen(stack.Id)) total += stack.Count;
            }
            return total;
        }

        public static void RemoveAll(PlayerInventory inventory)
        {
            if (inventory == null) return;
            for (int i = 0; i < PlayerInventory.TotalSize; i++)
            {
                ItemStack stack = inventory.GetSlot(i);
                if (stack != null && IsStolen(stack.Id)) inventory.TryRemove(stack.Id, stack.Count);
            }
        }

        public static int SellAll(PlayerInventory inventory, PlayerWallet wallet)
        {
            if (inventory == null) return 0;

            Dictionary<string, int> counts = new Dictionary<string, int>();
            int total = 0;
            for (int i = 0; i < PlayerInventory.TotalSize; i++)
            {
                ItemStack stack = inventory.GetSlot(i);
                if (stack == null || !IsStolen(stack.Id)) continue;

                total += ValueOf(stack.Id) * stack.Count;
                if (counts.ContainsKey(stack.Id)) counts[stack.Id] += stack.Count;
                else counts[stack.Id] = stack.Count;
            }

            foreach (KeyValuePair<string, int> pair in counts)
            {
                inventory.TryRemove(pair.Key, pair.Value);
            }

            if (total > 0 && wallet != null) wallet.Add(total);
            return total;
        }
    }
}
