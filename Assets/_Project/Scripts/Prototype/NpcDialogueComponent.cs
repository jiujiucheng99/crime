using System;
using UnityEngine;

namespace AmberCity.Prototype
{
    /// <summary>
    /// Reusable NPC conversation wrapper: fixed speech line plus optional choices.
    /// </summary>
    public class NpcDialogueComponent : MonoBehaviour
    {
        private DialogueChoiceController _dialogue;
        private string _speaker;

        public void Configure(DialogueChoiceController dialogue, string speaker)
        {
            _dialogue = dialogue;
            _speaker = speaker;
        }

        public void Say(string line)
        {
            if (_dialogue != null) _dialogue.OpenNotice(_speaker, line);
        }

        public void Ask(string line, string optionLabel, Action onOption)
        {
            if (_dialogue != null) _dialogue.Open(_speaker, line, optionLabel, onOption);
        }

        public void Ask(string line,
            string optionLabel, Action onOption,
            string optionLabel2, Action onOption2)
        {
            if (_dialogue != null) _dialogue.OpenChoice(_speaker, line, optionLabel, onOption, optionLabel2, onOption2);
        }

        public void Ask(string line,
            string optionLabel, Action onOption,
            string optionLabel2, Action onOption2,
            string optionLabel3, Action onOption3)
        {
            if (_dialogue != null)
            {
                _dialogue.OpenChoice3(_speaker, line, optionLabel, onOption, optionLabel2, onOption2, optionLabel3, onOption3);
            }
        }

        public static NpcDialogueComponent Create(Transform parent, string objectName,
            DialogueChoiceController dialogue, string speaker)
        {
            GameObject go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            NpcDialogueComponent component = go.AddComponent<NpcDialogueComponent>();
            component.Configure(dialogue, speaker);
            return component;
        }
    }
}
