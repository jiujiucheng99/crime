using UnityEngine;

namespace AmberCity.Prototype
{
    /// <summary>
    /// One civilian job at a time. Gang/special affiliations will use a separate system.
    /// </summary>
    public class PlayerEmployment : MonoBehaviour
    {
        public string CurrentJobId { get; private set; }
        public string CurrentJobName { get; private set; }
        public string CurrentWorkplace { get; private set; }

        public bool HasJob => !string.IsNullOrEmpty(CurrentJobId);

        public bool TryAccept(string jobId, string jobName, string workplace)
        {
            if (HasJob || string.IsNullOrEmpty(jobId)) return false;
            CurrentJobId = jobId;
            CurrentJobName = jobName;
            CurrentWorkplace = workplace;
            return true;
        }

        public void Quit()
        {
            CurrentJobId = null;
            CurrentJobName = null;
            CurrentWorkplace = null;
        }

        public void Restore(string jobId, string jobName, string workplace)
        {
            CurrentJobId = jobId;
            CurrentJobName = jobName;
            CurrentWorkplace = workplace;
        }
    }

    /// <summary>
    /// Simple NPC-side job offer. The actual job duties remain separate.
    /// </summary>
    public class JobOfferComponent : MonoBehaviour
    {
        private PlayerEmployment _employment;
        private DialogueChoiceController _dialogue;
        private string _jobId;
        private string _jobName;
        private string _workplace;
        private string _grantSkillId;

        public void Configure(PlayerEmployment employment, DialogueChoiceController dialogue,
            string jobId, string jobName, string workplace, string grantSkillId = null)
        {
            _employment = employment;
            _dialogue = dialogue;
            _jobId = jobId;
            _jobName = jobName;
            _workplace = workplace;
            _grantSkillId = grantSkillId;
        }

        public static JobOfferComponent Create(Transform parent, string objectName,
            PlayerEmployment employment, DialogueChoiceController dialogue,
            string jobId, string jobName, string workplace, string grantSkillId = null)
        {
            GameObject go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            JobOfferComponent offer = go.AddComponent<JobOfferComponent>();
            offer.Configure(employment, dialogue, jobId, jobName, workplace, grantSkillId);
            return offer;
        }

        public void Offer()
        {
            if (_dialogue == null || _employment == null) return;

            if (_employment.HasJob)
            {
                if (_employment.CurrentJobId == _jobId)
                {
                    _dialogue.OpenChoice3("招聘", "你已经在做" + _jobName + "。",
                        "工作说明", ShowInstructions,
                        "离职", QuitJob,
                        "结束对话", null);
                }
                else
                {
                    _dialogue.OpenNotice("招聘", "你已经有一份工作了：" + _employment.CurrentJobName + "。");
                }
                return;
            }

            _dialogue.OpenChoice3("招聘", "你好，我是这里的负责人。我们正在招" + _jobName + "，要接受吗？",
                "接受工作", Accept,
                "工作说明", ShowInstructions,
                "结束对话", null);
        }

        private void ShowInstructions()
        {
            if (_dialogue == null) return;
            string title = "工作说明：" + _jobName;
            _dialogue.OpenInfo(title, JobCatalog.Instructions(_jobId));
        }

        private void QuitJob()
        {
            if (_employment != null) _employment.Quit();
            if (_dialogue != null) _dialogue.OpenNotice("招聘", "你已离职。");
        }

        private void Accept()
        {
            if (_employment != null && _employment.TryAccept(_jobId, _jobName, _workplace))
            {
                if (!string.IsNullOrEmpty(_grantSkillId))
                {
                    PlayerSkills skills = _employment.GetComponent<PlayerSkills>();
                    if (skills != null) skills.Learn(_grantSkillId);
                }
                if (_dialogue != null) _dialogue.OpenNotice("招聘", "你已入职：" + _workplace + " · " + _jobName + "。");
            }
            else if (_dialogue != null)
            {
                _dialogue.OpenNotice("招聘", "你已经有工作了。");
            }
        }
    }
}
