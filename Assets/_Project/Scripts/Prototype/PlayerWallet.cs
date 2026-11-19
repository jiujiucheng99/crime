using UnityEngine;

namespace AmberCity.Prototype
{
    /// <summary>Simple on-hand cash. No banking or save system yet.</summary>
    public class PlayerWallet : MonoBehaviour
    {
        [SerializeField] private int startingMoney = 10;
        private int _money;

        public int Money => _money;

        private void Awake()
        {
            _money = startingMoney;
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || _money < amount) return false;
            _money -= amount;
            return true;
        }

        public void Add(int amount)
        {
            if (amount > 0) _money += amount;
        }

        public void SetMoney(int amount)
        {
            _money = Mathf.Max(0, amount);
        }
    }
}
