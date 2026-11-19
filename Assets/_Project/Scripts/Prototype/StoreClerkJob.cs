using System.Collections.Generic;
using UnityEngine;

namespace AmberCity.Prototype
{
    /// <summary>
    /// First work shift: convenience-store clerk. Serve three customers, then get paid.
    /// </summary>
    public class StoreClerkJob : MonoBehaviour
    {
        private const int RequiredCustomers = 3;
        private const int Wage = 12;
        public static readonly Vector2 ServicePoint = new Vector2(2001.4f, 0.35f);

        private Transform _player;
        private DialogueChoiceController _dialogue;
        private PlayerWallet _wallet;
        private PlayerEmployment _employment;
        private readonly List<GameObject> _customers = new List<GameObject>();
        private GameObject _customerRoot;
        private bool _shiftActive;
        private int _served;
        private GUIStyle _statusStyle;

        public bool IsShiftActive => _shiftActive;

        public void Configure(Transform player, DialogueChoiceController dialogue,
            PlayerWallet wallet, PlayerEmployment employment)
        {
            _player = player;
            _dialogue = dialogue;
            _wallet = wallet;
            _employment = employment;
        }

        public void StartShift()
        {
            if (_shiftActive) return;

            if (_employment == null || _employment.CurrentJobId != "store_clerk")
            {
                if (_dialogue != null) _dialogue.OpenNotice("店员", "你还没有入职便利店。");
                return;
            }

            _shiftActive = true;
            _served = 0;
            BuildCustomers();
            if (_dialogue != null) _dialogue.OpenNotice("店员", "轮班开始：服务 3 位顾客。");
        }

        public void ServeCustomer()
        {
            if (!_shiftActive || _customers.Count == 0) return;

            GameObject customer = _customers[_customers.Count - 1];
            _customers.RemoveAt(_customers.Count - 1);
            if (customer != null) Object.Destroy(customer);
            _served++;

            if (_served >= RequiredCustomers) EndShift();
        }

        public void CancelShift()
        {
            _shiftActive = false;
            _served = 0;
            ClearCustomers();
        }

        private void EndShift()
        {
            _shiftActive = false;
            ClearCustomers();
            if (_wallet != null) _wallet.Add(Wage);
            if (_dialogue != null) _dialogue.OpenNotice("店员", "轮班结束，工资 " + Wage + " 块。");
        }

        private void BuildCustomers()
        {
            ClearCustomers();
            _customerRoot = new GameObject("StoreCustomers");
            _customerRoot.transform.position = new Vector3(2000f, 0f, 0f);

            Vector3[] positions =
            {
                new Vector3(1.8f, 0.2f, 0f),
                new Vector3(2.5f, 0.2f, 0f),
                new Vector3(3.2f, 0.2f, 0f)
            };

            for (int i = 0; i < RequiredCustomers; i++)
            {
                string resource = "Characters/BarPatrons/patron_0" + (i + 1);
                Sprite sprite = Resources.Load<Sprite>(resource);
                if (sprite == null) continue;

                GameObject customer = new GameObject("StoreCustomer" + (i + 1));
                customer.transform.SetParent(_customerRoot.transform, false);
                customer.transform.localPosition = positions[i];
                SpriteRenderer renderer = customer.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = Mathf.RoundToInt(-positions[i].y * 100f);
                float scale = 1.35f / Mathf.Max(0.001f, sprite.bounds.size.y);
                customer.transform.localScale = new Vector3(scale, scale, 1f);
                _customers.Add(customer);
            }
        }

        private void ClearCustomers()
        {
            foreach (GameObject customer in _customers)
            {
                if (customer != null) Object.Destroy(customer);
            }
            _customers.Clear();
            if (_customerRoot != null) Object.Destroy(_customerRoot);
            _customerRoot = null;
        }

        private void OnGUI()
        {
            if (!_shiftActive) return;
            if (_statusStyle == null)
            {
                Font font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Microsoft YaHei", "Microsoft YaHei UI", "SimHei", "Noto Sans CJK SC", "Arial" }, 16);
                _statusStyle = new GUIStyle(GUI.skin.label)
                {
                    font = font,
                    fontSize = 15,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.95f, 0.89f, 0.72f) }
                };
            }

            Rect rect = new Rect(Screen.width * 0.5f - 120f, 18f, 240f, 30f);
            Color previous = GUI.color;
            GUI.color = new Color(0.04f, 0.05f, 0.06f, 0.9f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(0.62f, 0.52f, 0.32f, 1f);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Label(rect, "便利店轮班 " + _served + " / " + RequiredCustomers, _statusStyle);
        }
    }
}
