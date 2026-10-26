using UnityEngine;
using Yumiwa.Domain;

namespace Yumiwa.Infrastructure
{
    [CreateAssetMenu(menuName = "Yumiwa/Dialogue/Dialogue Sequence", fileName = "DialogueSequence")]
    public sealed class DialogueSequenceAsset : ScriptableObject
    {
        public string SequenceId = "sequence_new";
        public string[] LineIds = new string[0];
        public float GapBetweenLinesSeconds = 0.5f;
        public string OnCompleteEventId = "";

        public DialogueSequence ToDefinition() => new DialogueSequence(
            SequenceId, LineIds ?? new string[0], GapBetweenLinesSeconds,
            string.IsNullOrEmpty(OnCompleteEventId) ? null : OnCompleteEventId);

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(SequenceId))
                Debug.LogWarning($"[Yumiwa] Dialogue sequence '{name}' has empty id.", this);
            if (LineIds == null || LineIds.Length == 0)
                Debug.LogWarning($"[Yumiwa] Dialogue sequence '{name}' has no lines.", this);
        }
    }
}
