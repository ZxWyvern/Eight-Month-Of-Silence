using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Finds the nearest available IInspectable around the player and forwards
    /// the Interact press to the inspection controller. Scans on demand on button
    /// press (no Update loop, no per-press allocation) instead of trigger plumbing,
    /// because a CharacterController does not emit trigger events on itself reliably.
    /// Idle while inspection is active; the Player map is disabled then anyway.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float interactRadius = 2.2f;
        [SerializeField] private LayerMask scanMask = ~0;
        [Tooltip("Drag the InspectionRig controller here; kept as an inspector reference so no scene search is needed.")]
        [SerializeField] private ItemInspectionController inspection;

        private readonly Collider[] scanHits = new Collider[16];

        private InputAction interactAction;
        private IEventBus bus;
        private System.Action<InspectionStartedEvent> startedHandler;
        private System.Action<InspectionEndedEvent> endedHandler;
        private bool locked;

        [Inject]
        public void Construct(InputActionAsset asset, IEventBus bus)
        {
            this.bus = bus;
            if (asset != null)
                interactAction = asset.FindAction("Player/Interact");
        }

        private bool subscribed;

        private void Start()
        {
            if (inspection == null)
            {
                Debug.LogError("[Yumiwa] PlayerInteractor has no inspection controller assigned.", this);
                enabled = false;
                return;
            }
            if (bus == null)
            {
                Debug.LogError("[Yumiwa] PlayerInteractor injection missing (IEventBus).", this);
                enabled = false;
                return;
            }
            if (interactAction == null)
                Debug.LogWarning("[Yumiwa] PlayerInteractor: 'Player/Interact' action not found.", this);
            SubscribeOnce();
        }

        private void OnEnable()
        {
            SubscribeOnce();
        }

        private void SubscribeOnce()
        {
            if (subscribed)
                return;
            if (bus == null || inspection == null)
                return;
            if (startedHandler == null)
                startedHandler = _ => locked = true;
            if (endedHandler == null)
                endedHandler = _ => locked = false;
            bus.Subscribe(startedHandler);
            bus.Subscribe(endedHandler);
            if (interactAction != null)
                interactAction.performed += OnInteractPressed;
            subscribed = true;
        }

        private void OnDisable()
        {
            if (interactAction != null)
                interactAction.performed -= OnInteractPressed;
            if (bus != null)
            {
                if (startedHandler != null) bus.Unsubscribe(startedHandler);
                if (endedHandler != null) bus.Unsubscribe(endedHandler);
            }
            subscribed = false;
        }

        private void OnInteractPressed(InputAction.CallbackContext _)
        {
            if (locked || inspection == null || inspection.IsInspecting)
            {
                Debug.LogWarning(
                    $"[Yumiwa] Interact ignored (locked={locked}, inspecting={inspection != null && inspection.IsInspecting}).", this);
                return;
            }
            var target = FindNearestAvailable(out int scanned);
            if (target != null)
            {
                Debug.Log($"[Yumiwa] Interact: found '{(target as Component)?.name}', opening inspection.", this);
                inspection.BeginInspection(target);
            }
            else
            {
                Debug.LogWarning(
                    $"[Yumiwa] Interact pressed but no available inspectable (scanned {scanned} colliders within {interactRadius}m).", this);
            }
        }

        private IInspectable FindNearestAvailable(out int scanned)
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, interactRadius, scanHits, scanMask, QueryTriggerInteraction.Collide);
            scanned = count;
            IInspectable best = null;
            float bestSqr = float.MaxValue;
            Vector3 origin = transform.position;
            for (int i = 0; i < count; i++)
            {
                var candidate = scanHits[i].GetComponent<IInspectable>();
                if (candidate == null || !candidate.IsAvailable)
                    continue;
                var behaviour = candidate as Component;
                if (behaviour == null)
                    continue;
                float sqr = (behaviour.transform.position - origin).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = candidate;
                }
            }
            return best;
        }
    }
}
