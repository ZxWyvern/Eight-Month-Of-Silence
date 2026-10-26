using UnityEngine;
using Yumiwa.Domain;

namespace Yumiwa.Infrastructure
{
    [CreateAssetMenu(menuName = "Yumiwa/Dialogue/Dialogue Line", fileName = "DialogueLine")]
    public sealed class DialogueLineAsset : ScriptableObject
    {
        public string LineId = "line_new";
        public string SpeakerId = "";
        [TextArea(2, 4)] public string Text = "";
        public DialoguePriority Priority = DialoguePriority.Ambient;
        public float DurationOverrideSeconds = 0f;
        public float ExpiresAfterSeconds = 0f;
        public bool ResumableIfInterrupted = false;

        public DialogueLine ToDefinition() => new DialogueLine(
            LineId, SpeakerId, Text, Priority, DurationOverrideSeconds, ExpiresAfterSeconds, ResumableIfInterrupted);

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(LineId))
                Debug.LogWarning($"[Yumiwa] Dialogue line '{name}' has empty id.", this);
            if (string.IsNullOrEmpty(Text))
                Debug.LogWarning($"[Yumiwa] Dialogue line '{name}' has empty text.", this);
        }
    }
}
