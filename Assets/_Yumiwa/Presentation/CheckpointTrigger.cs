using UnityEngine;
using VContainer;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    [RequireComponent(typeof(Collider))]
    public sealed class CheckpointTrigger : MonoBehaviour
    {
        [SerializeField] private CheckpointMilestone milestone = CheckpointMilestone.DisplayCaseKey;
        [SerializeField] private string advanceMissionId = ""; // optional: set M02_Motel etc. to jump directly; empty = auto Advance()
        private ICheckpointService checkpointService;
        private MissionFlowService missionFlow;

        [Inject]
        public void Construct(ICheckpointService checkpointService, MissionFlowService missionFlow = null)
        {
            this.checkpointService = checkpointService;
            this.missionFlow = missionFlow;
        }

        // For prototype without DI fallback
        public void SetService(ICheckpointService svc) => checkpointService = svc;
        public void SetMissionFlow(MissionFlowService flow) => missionFlow = flow;

        private void Awake()
        {
            if (missionFlow == null)
            {
                Debug.LogWarning($"[Yumiwa] CheckpointTrigger {milestone} missionFlow not injected — add {name} to GameLifetimeScope.presenterRoots for mission wiring.", this);
            }
        }

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent<PlayerMovementPresenter>(out _)) return;
            if (checkpointService == null)
            {
                Debug.LogWarning($"[Yumiwa] CheckpointTrigger {milestone} no service.", this);
                return;
            }
            var data = new CheckpointData
            {
                PosX = other.transform.position.x,
                PosY = other.transform.position.y,
                PosZ = other.transform.position.z,
                Health = 100f,
                Stamina = 100f,
                Milestone = milestone,
                ActiveScene = gameObject.scene.name,
                Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                SaveVersion = "1"
            };
            // Support both interface and concrete extra API
            if (checkpointService is CheckpointService concrete)
                concrete.SaveHouseMilestone(data, milestone);
            else
                checkpointService.SaveCheckpoint(data);
            Debug.Log($"[Yumiwa] Checkpoint reached: {milestone}", this);
            // Wiring mission: checkpoint advances mission (rework-friendly: set advanceMissionId in inspector for explicit jump)
            if (missionFlow != null)
            {
                if (!string.IsNullOrEmpty(advanceMissionId))
                {
                    var seq = missionFlow.Sequence;
                    for (int i = 0; i < seq.Count; i++)
                        if (seq[i].MissionId == advanceMissionId) { missionFlow.SetIndex(i); break; }
                    Debug.Log($"[Yumiwa] Mission jumped via checkpoint {milestone} → {advanceMissionId}", this);
                }
                else if (missionFlow.CanAdvance)
                {
                    missionFlow.Advance();
                    Debug.Log($"[Yumiwa] Mission advanced via checkpoint {milestone} → {missionFlow.Current?.DisplayName} [{missionFlow.CurrentIndex + 1}/{missionFlow.Count}]", this);
                }
            }
            // Disable after first trigger to prevent spam
            GetComponent<Collider>().enabled = false;
        }
    }
}
