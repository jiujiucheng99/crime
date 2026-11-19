using UnityEngine;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Fridge/container that refills itself whenever it is opened.
    /// </summary>
    public class AutoRestockContainer : MonoBehaviour
    {
        private string[] _itemIds = new string[0];
        private int _countPerItem = 5;
        private string _displayName = "STORAGE";
        private int _capacity;

        public void Configure(string[] itemIds, int countPerItem, string displayName, int capacity)
        {
            _itemIds = itemIds ?? new string[0];
            _countPerItem = Mathf.Max(1, countPerItem);
            _displayName = displayName;
            _capacity = Mathf.Max(1, capacity);
        }

        public void EnsureStock()
        {
            StorageContainer storage = GetComponent<StorageContainer>();
            if (storage == null) return;

            int capacity = _capacity > 0 ? _capacity : Mathf.Max(1, _itemIds.Length * 2);
            storage.Configure(_displayName, capacity);
            for (int i = 0; i < _itemIds.Length; i++)
            {
                storage.SetSlot(i, ItemCatalog.CreateStack(_itemIds[i], _countPerItem));
            }
        }
    }
}
