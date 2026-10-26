using UnityEngine;
using Yumiwa.Domain;

namespace Yumiwa.Infrastructure
{
    [CreateAssetMenu(menuName = "Yumiwa/Trap/Trap Definition", fileName = "Trap")]
    public sealed class TrapDefinitionAsset : ScriptableObject
    {
        public string TrapId = "trap_new";
        public string DisplayName = "Trap";
        public float Damage = 25f;
        public string AudioCueId = "trap_click";
        public string VisualCueId = "trap_board";
        public bool IsLearnable = true;

        public TrapDefinition ToDefinition() => new TrapDefinition(TrapId, DisplayName, Damage, AudioCueId, VisualCueId, IsLearnable);
    }
}
