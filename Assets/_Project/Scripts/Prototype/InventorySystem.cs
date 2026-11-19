using System.Collections.Generic;
using UnityEngine;

namespace AmberCity.Prototype
{
    public sealed class ItemStack
    {
        public string Id;
        public string DisplayName;
        public int Count;

        public ItemStack(string id, string displayName, int count = 1)
        {
            Id = id;
            DisplayName = displayName;
            Count = Mathf.Max(1, count);
        }

        public ItemStack Clone()
        {
            return new ItemStack(Id, DisplayName, Count);
        }
    }

    public sealed class PlayerInventory : MonoBehaviour
    {
        public const int HotbarSize = 9;
        public const int BackpackSize = 27;
        public const int TotalSize = HotbarSize + BackpackSize;

        private readonly ItemStack[] _slots = new ItemStack[TotalSize];
        private int _selectedHotbar;

        public int Capacity => TotalSize;
        public int Count
        {
            get
            {
                int count = 0;
                foreach (ItemStack stack in _slots)
                {
                    if (stack != null) count++;
                }
                return count;
            }
        }

        public int SelectedHotbar => _selectedHotbar;

        public ItemStack GetSlot(int index)
        {
            return index >= 0 && index < TotalSize ? _slots[index] : null;
        }

        public void SetSlot(int index, ItemStack stack)
        {
            if (index < 0 || index >= TotalSize) return;
            _slots[index] = stack;
        }

        public void SelectHotbar(int index)
        {
            _selectedHotbar = Mathf.Clamp(index, 0, HotbarSize - 1);
        }

        public bool TryAdd(ItemStack stack)
        {
            if (stack == null) return false;
            for (int i = 0; i < TotalSize; i++)
            {
                if (_slots[i] == null)
                {
                    _slots[i] = stack;
                    return true;
                }
            }
            return false;
        }

        public ItemStack TakeAt(int index)
        {
            if (index < 0 || index >= TotalSize) return null;
            ItemStack stack = _slots[index];
            _slots[index] = null;
            return stack;
        }

        public bool TryRemove(string id, int amount = 1)
        {
            if (string.IsNullOrEmpty(id) || amount <= 0) return false;
            for (int i = 0; i < TotalSize; i++)
            {
                ItemStack stack = _slots[i];
                if (stack == null || stack.Id != id) continue;

                if (stack.Count > amount)
                {
                    stack.Count -= amount;
                }
                else
                {
                    _slots[i] = null;
                }
                return true;
            }
            return false;
        }

        public int CountOf(string id)
        {
            int total = 0;
            for (int i = 0; i < TotalSize; i++)
            {
                if (_slots[i] != null && _slots[i].Id == id) total += _slots[i].Count;
            }
            return total;
        }

        public void PutAt(int index, ItemStack stack)
        {
            if (index < 0 || index >= TotalSize) return;
            _slots[index] = stack;
        }

        public ItemStack SelectedItem => GetSlot(_selectedHotbar);

        public void ClearAll()
        {
            for (int i = 0; i < TotalSize; i++) _slots[i] = null;
            _selectedHotbar = 0;
        }
    }

    public sealed class StorageContainer : MonoBehaviour
    {
        [SerializeField] private string displayName = "CONTAINER";
        [SerializeField] private int capacity = 1;
        private readonly List<ItemStack> _slots = new List<ItemStack>();

        public string DisplayName => displayName;
        public int Capacity => capacity;
        public int Count
        {
            get
            {
                int count = 0;
                foreach (ItemStack stack in _slots)
                {
                    if (stack != null) count++;
                }
                return count;
            }
        }

        public void Configure(string label, int slotCount)
        {
            displayName = label;
            capacity = Mathf.Max(1, slotCount);
            _slots.Clear();
            for (int i = 0; i < capacity; i++) _slots.Add(null);
        }

        public ItemStack GetSlot(int index)
        {
            return index >= 0 && index < _slots.Count ? _slots[index] : null;
        }

        public void SetSlot(int index, ItemStack stack)
        {
            if (index < 0 || index >= _slots.Count) return;
            _slots[index] = stack;
        }

        public bool TryAdd(ItemStack stack)
        {
            if (stack == null) return false;
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] == null)
                {
                    _slots[i] = stack;
                    return true;
                }
            }
            return false;
        }

        public ItemStack TakeAt(int index)
        {
            if (index < 0 || index >= _slots.Count) return null;
            ItemStack stack = _slots[index];
            _slots[index] = null;
            return stack;
        }
    }
}
