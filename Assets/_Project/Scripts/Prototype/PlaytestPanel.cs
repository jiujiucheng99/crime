using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Editor-only test shortcuts. This component is not included in player builds.
    /// </summary>
    public class PlaytestPanel : MonoBehaviour
    {
        private PlayerSkills _skills;
        private PlayerInventory _inventory;
        private PlayerWallet _wallet;
        private Transform _player;
        private BarController _bar;
        private NeighborApartmentController _neighbor;
        private SafehouseController _safehouse;
        private CrimeLoopController _crimeLoop;
        private bool _show;
        private string _message = string.Empty;
        private float _messageTimer;

        private void Awake()
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                _player = player.transform;
                _skills = player.GetComponent<PlayerSkills>();
                _inventory = player.GetComponent<PlayerInventory>();
                _wallet = player.GetComponent<PlayerWallet>();
            }

            GameObject bootstrap = GameObject.Find("MovementTestBootstrap");
            if (bootstrap != null)
            {
                _bar = bootstrap.GetComponent<BarController>();
                _safehouse = bootstrap.GetComponent<SafehouseController>();
                _crimeLoop = bootstrap.GetComponent<CrimeLoopController>();
            }

            _neighbor = Object.FindObjectOfType<NeighborApartmentController>();
        }

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current.f12Key.wasPressedThisFrame) _show = !_show;
            if (Keyboard.current.f8Key.wasPressedThisFrame) LearnLockpicking();
            if (Keyboard.current.f9Key.wasPressedThisFrame) AddMoney();
            if (Keyboard.current.f7Key.wasPressedThisFrame) AddSkillExperience();
            if (Keyboard.current.f5Key.wasPressedThisFrame) SaveProgress();
            if (Keyboard.current.f6Key.wasPressedThisFrame) LoadProgress();
            if (_messageTimer > 0f) _messageTimer -= Time.deltaTime;
        }

        private void LearnLockpicking()
        {
            if (_skills != null) _skills.Learn("lockpicking");
            if (_inventory != null)
            {
                _inventory.TryAdd(new ItemStack("paperclip", "回形针", 5));
            }
            Message("已学会开锁，获得 5 个回形针");
        }

        private void AddMoney()
        {
            if (_wallet != null) _wallet.Add(100);
            Message("已增加 100 块");
        }

        private void AddSkillExperience()
        {
            if (_skills == null) return;
            _skills.AddExperience("driving", 80);
            _skills.AddExperience("cooking", 80);
            _skills.AddExperience("mechanics", 80);
            _skills.AddExperience("logistics", 80);
            _skills.AddExperience("lockpicking", 80);
            Message("所有已学习技能 +80 经验");
        }

        private void SaveProgress()
        {
            SaveGameService.Save(_player);
            Message("进度已保存");
        }

        private void LoadProgress()
        {
            Message(SaveGameService.Load(_player) ? "进度已读取" : "没有可读取的存档");
        }

        private void DeleteProgress()
        {
            SaveGameService.Delete();
            Message("存档已删除");
        }

        private void ShowBar()
        {
            if (_bar == null) _bar = Object.FindObjectOfType<BarController>();
            if (_bar == null) return;
            InvokePrivate(_bar, "Enter");
            Message("进入酒吧");
        }

        private void ShowNeighbor()
        {
            if (_neighbor == null) _neighbor = Object.FindObjectOfType<NeighborApartmentController>();
            if (_neighbor == null) return;
            InvokePrivate(_neighbor, "Enter");
            Message("进入邻居家");
        }

        private void ShowSafehouse()
        {
            if (_safehouse != null) _safehouse.EnterApartment();
            Message("进入安全屋");
        }

        private void SpawnGang()
        {
            if (_crimeLoop != null) InvokePrivate(_crimeLoop, "SpawnGang");
            Message("生成灰庭手下");
        }

        private void SpawnPolice()
        {
            if (_crimeLoop != null) InvokePrivate(_crimeLoop, "SpawnPolice");
            Message("生成警察");
        }

        private void TriggerReport()
        {
            if (_crimeLoop != null) InvokePrivate(_crimeLoop, "ReportTheft");
            Message("触发邻居报警");
        }

        private void ClearCrime()
        {
            CrimeStoryState.NeighborReported = false;
            CrimeStoryState.PoliceArrived = false;
            CrimeStoryState.GangContacted = false;
            CrimeStoryState.GangResolved = false;
            CrimeStoryState.GangHostile = false;
            TheftNoise.Reset();
            if (_crimeLoop != null)
            {
                SetPrivate(_crimeLoop, "_reportTimer", 0f);
                SetPrivate(_crimeLoop, "_policeTimer", 0f);
                SetPrivate(_crimeLoop, "_gangTimer", 0f);
            }

            GameObject gang = GameObject.Find("GangCollector");
            if (gang != null) Destroy(gang);
            GameObject police = GameObject.Find("PoliceResponder");
            if (police != null) Destroy(police);
            Message("犯罪测试状态已清理");
        }

        private void Message(string message)
        {
            _message = message;
            _messageTimer = 3f;
        }

        private static object InvokePrivate(object target, string method)
        {
            if (target == null) return null;
            MethodInfo info = target.GetType().GetMethod(method,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return info != null ? info.Invoke(target, null) : null;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            if (target == null) return;
            FieldInfo info = target.GetType().GetField(field,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (info != null) info.SetValue(target, value);
        }

        private void OnGUI()
        {
            if (!_show) return;
            GUILayout.BeginArea(new Rect(Screen.width - 300f, 20f, 280f, 480f), GUI.skin.box);
            GUILayout.Label("PLAYTEST  F12 关闭");
            GUILayout.Space(4f);
            if (GUILayout.Button("学会开锁 + 5 回形针")) LearnLockpicking();
            if (GUILayout.Button("+100 块")) AddMoney();
            if (GUILayout.Button("所有技能 +80 经验 (F7)")) AddSkillExperience();
            if (GUILayout.Button("保存进度 (F5)")) SaveProgress();
            if (GUILayout.Button("读取进度 (F6)")) LoadProgress();
            if (GUILayout.Button("删除存档")) DeleteProgress();
            GUILayout.Space(6f);
            if (GUILayout.Button("进入酒吧")) ShowBar();
            if (GUILayout.Button("进入邻居家")) ShowNeighbor();
            if (GUILayout.Button("进入安全屋")) ShowSafehouse();
            GUILayout.Space(6f);
            if (GUILayout.Button("生成灰庭手下")) SpawnGang();
            if (GUILayout.Button("生成警察")) SpawnPolice();
            if (GUILayout.Button("触发邻居报警")) TriggerReport();
            if (GUILayout.Button("清理犯罪测试状态")) ClearCrime();
            GUILayout.Space(6f);
            if (!string.IsNullOrEmpty(_message) && _messageTimer > 0f)
            {
                GUILayout.Label(_message);
            }
            GUILayout.EndArea();
        }
    }
}
