using UnityEngine;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Small editor/gameplay toast for skill level-ups.
    /// </summary>
    public class SkillNotificationUI : MonoBehaviour
    {
        private string _message;
        private float _timer;
        private GUIStyle _style;

        private void OnEnable()
        {
            PlayerSkills.SkillLeveledUp += OnLeveledUp;
        }

        private void OnDisable()
        {
            PlayerSkills.SkillLeveledUp -= OnLeveledUp;
        }

        private void OnLeveledUp(string id, int level)
        {
            SkillDefinition definition = SkillCatalog.Get(id);
            string name = definition != null ? definition.DisplayName : id;
            _message = name + " 提升到 LV" + level;
            _timer = 3f;
        }

        private void Update()
        {
            if (_timer > 0f) _timer -= Time.deltaTime;
        }

        private void OnGUI()
        {
            if (_timer <= 0f || string.IsNullOrEmpty(_message)) return;
            if (_style == null)
            {
                Font font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Microsoft YaHei", "Microsoft YaHei UI", "SimHei", "Noto Sans CJK SC", "Arial" }, 16);
                _style = new GUIStyle(GUI.skin.label)
                {
                    font = font, fontSize = 15, alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.98f, 0.88f, 0.56f) }
                };
            }

            Rect box = new Rect(Screen.width * 0.5f - 170f, 92f, 340f, 28f);
            Color previous = GUI.color;
            GUI.color = new Color(0.04f, 0.05f, 0.06f, 0.92f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Label(box, "技能提升：" + _message, _style);
        }
    }
}
