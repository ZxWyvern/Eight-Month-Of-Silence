using UnityEngine;
using VContainer;
using VContainer.Unity;
using Yumiwa.Application;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Gameplay trigger per plan.json (trigger_volume / scripted_event / radio_call /
    /// sequence_step): fires one line or sequence into <see cref="DialogueQueueService"/>.
    /// Non-blocking, never gates progression (GDD-005); once/repeat governed here.
    /// Fires on enter AND while the player overlaps (polled OverlapBox, same GC-safe
    /// pattern as PlayerInteractor): Rigidbody-less CharacterController teleports and
    /// spawn-inside volumes never produce OnTriggerEnter, so enter alone is not enough.
    /// </summary>
    public sealed class DialogueTrigger : MonoBehaviour
    {
        public enum TriggerMode { Line, Sequence }

        [SerializeField] private TriggerMode mode = TriggerMode.Line;
        [SerializeField] private string lineId = "";
        [SerializeField] private string sequenceId = "";
        [SerializeField] private bool triggerOnce = true;
        [SerializeField] private float repeatCooldownSeconds = 5f;
        [SerializeField] private float overlapCheckIntervalSeconds = 0.2f;
        [SerializeField] private LayerMask overlapMask = ~0;

        private DialogueQueueService dialogue;
        private bool fired;
        private float lastFireTime = float.NegativeInfinity;
        private float lastOverlapCheck = float.NegativeInfinity;
        private BoxCollider triggerVolume;
        private readonly Collider[] overlapHits = new Collider[8];

        [Inject]
        public void Construct(DialogueQueueService dialogue)
        {
            this.dialogue = dialogue;
        }

        private void Awake()
        {
            triggerVolume = GetComponent<BoxCollider>();
            TryResolveDependencies();
            // Spawn-inside volumes never get OnTriggerEnter: check overlap immediately.
            CheckOverlapAndFire();
        }

        private void Update()
        {
            if (triggerOnce && fired)
                return;
            if (Time.time - lastOverlapCheck < overlapCheckIntervalSeconds)
                return;
            lastOverlapCheck = Time.time;
            CheckOverlapAndFire();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || other.GetComponentInParent<PlayerMovementPresenter>() == null)
                return;
            Fire();
        }

        /// <summary>Scripted-event entry point: same rules as the volume (once/cooldown).</summary>
        public void Fire()
        {
            if (dialogue == null && !TryResolveDependencies())
            {
                Debug.LogWarning("[Yumiwa] DialogueTrigger has no DialogueQueueService.", this);
                return;
            }
            if (triggerOnce && fired)
                return;
            if (Time.time - lastFireTime < repeatCooldownSeconds)
                return;
            bool accepted = mode == TriggerMode.Sequence
                ? dialogue.PlaySequence(sequenceId)
                : dialogue.PlayLine(lineId);
            if (!accepted)
            {
                Debug.LogWarning($"[Yumiwa] DialogueTrigger rejected (mode {mode}, line '{lineId}', seq '{sequenceId}').", this);
                return;
            }
            fired = true;
            lastFireTime = Time.time;
        }

        public void ResetTrigger()
        {
            fired = false;
            lastFireTime = float.NegativeInfinity;
        }

        private void CheckOverlapAndFire()
        {
            if (triggerVolume == null || !triggerVolume.enabled)
                return;
            int count = Physics.OverlapBoxNonAlloc(
                triggerVolume.bounds.center, triggerVolume.bounds.extents,
                overlapHits, transform.rotation, overlapMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                var hit = overlapHits[i];
                if (hit != null && hit.GetComponentInParent<PlayerMovementPresenter>() != null)
                {
                    Fire();
                    break;
                }
            }
            for (int i = 0; i < overlapHits.Length; i++)
                overlapHits[i] = null;
        }

        private bool TryResolveDependencies()
        {
            if (dialogue != null)
                return true;
            var scope = GetComponentInParent<LifetimeScope>();
            if (scope != null && scope.Container != null)
            {
                try
                {
                    dialogue = scope.Container.Resolve<DialogueQueueService>();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[Yumiwa] DialogueTrigger resolve failed: {e.Message}", this);
                    dialogue = null;
                }
            }
            return dialogue != null;
        }
    }
}
