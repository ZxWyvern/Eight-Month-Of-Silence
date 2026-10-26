using UnityEngine;
using VContainer;
using VContainer.Unity;
using Yumiwa.Application;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Timeline signal cue per ADR-0025: parameterless entry points for Timeline
    /// SignalReceiver reactions (SignalEmitter at a timestamp → PlayCue()).
    /// Purely reactive — no Update, no physics, no Timeline API references (the
    /// Timeline/Signal/Director are data wired in-editor, so no asmdef change).
    /// Timing/queue ownership stays in <see cref="DialogueQueueService"/> (ARCH-002).
    /// </summary>
    public sealed class DialogueTimelineCue : MonoBehaviour
    {
        [SerializeField] private DialogueTrigger.TriggerMode mode = DialogueTrigger.TriggerMode.Line;
        [SerializeField] private string lineId = "";
        [SerializeField] private string sequenceId = "";

        private DialogueQueueService dialogue;
        private string lastStartedLineId;

        [Inject]
        public void Construct(DialogueQueueService dialogue)
        {
            this.dialogue = dialogue;
        }

        private void Awake()
        {
            TryResolveDependencies();
        }

        /// <summary>Signal reaction: plays the configured line or sequence once.</summary>
        public void PlayCue()
        {
            if (dialogue == null && !TryResolveDependencies())
            {
                Debug.LogWarning("[Yumiwa] DialogueTimelineCue has no DialogueQueueService.", this);
                return;
            }
            bool accepted = mode == DialogueTrigger.TriggerMode.Sequence
                ? dialogue.PlaySequence(sequenceId)
                : dialogue.PlayLine(lineId);
            if (!accepted)
            {
                Debug.LogWarning($"[Yumiwa] DialogueTimelineCue rejected (mode {mode}, line '{lineId}', seq '{sequenceId}').", this);
                return;
            }
            lastStartedLineId = mode == DialogueTrigger.TriggerMode.Sequence ? null : lineId;
        }

        /// <summary>Signal reaction: cancels this cue's line (sequences run to CancelAll).</summary>
        public void CancelCue()
        {
            if (dialogue == null && !TryResolveDependencies())
                return;
            if (!string.IsNullOrEmpty(lastStartedLineId))
                dialogue.CancelLine(lastStartedLineId);
            lastStartedLineId = null;
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
                    Debug.LogWarning($"[Yumiwa] DialogueTimelineCue resolve failed: {e.Message}", this);
                    dialogue = null;
                }
            }
            return dialogue != null;
        }
    }
}
