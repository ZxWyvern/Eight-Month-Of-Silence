using UnityEngine;
using Yumiwa.Domain;

namespace Yumiwa.Infrastructure
{
    [CreateAssetMenu(menuName = "Yumiwa/Dialogue/Speaker", fileName = "Speaker")]
    public sealed class SpeakerAsset : ScriptableObject
    {
        public string SpeakerId = "speaker_new";
        public string DisplayName = "Speaker";
        public string NameColorHex = "#FFFFFF";

        public Speaker ToDefinition() => new Speaker(SpeakerId, DisplayName, NameColorHex);

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(SpeakerId))
                Debug.LogWarning($"[Yumiwa] Speaker '{name}' has empty id.", this);
        }
    }
}
