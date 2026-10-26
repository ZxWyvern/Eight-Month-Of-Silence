using UnityEngine;
using TMPro;
using UnityEngine.UI;
using VContainer;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>HUD presenter reacting to stamina and movement-state events.</summary>
    public sealed class StaminaHudPresenter : MonoBehaviour
    {
        private Slider slider;
        private TMP_Text stateLabel;
        private Canvas canvas;
        private IEventBus bus;
        private bool isInspecting;
        private float hideAt;

        [Inject]
        public void Construct(IEventBus bus)
        {
            this.bus = bus;
        }

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            slider = GetComponentInChildren<Slider>();
            var labelTransform = transform.Find("StateLabel");
            if (labelTransform != null)
                stateLabel = labelTransform.GetComponent<TMP_Text>();
            // Sesuai docs/07 HUD minimal — hanya tampil saat dipakai.
            // Saat inspection, HUD lain dimatikan total (hanya InspectionUI yang hidup).
            if (canvas != null)
                canvas.enabled = false;
        }

        private bool subscribed;

        private void Start()
        {
            if (bus == null)
            {
                Debug.LogWarning("[Yumiwa] StaminaHudPresenter not injected (IEventBus).", this);
                return;
            }
            SubscribeOnce();
        }

        private void OnEnable()
        {
            SubscribeOnce();
        }

        private void SubscribeOnce()
        {
            if (subscribed || bus == null)
                return;
            bus.Subscribe<StaminaChangedEvent>(OnStaminaChanged);
            bus.Subscribe<MovementStateChangedEvent>(OnStateChanged);
            bus.Subscribe<InspectionStartedEvent>(OnInspectionStarted);
            bus.Subscribe<InspectionEndedEvent>(OnInspectionEnded);
            subscribed = true;
        }

        private void OnDisable()
        {
            if (bus == null)
                return;
            bus.Unsubscribe<StaminaChangedEvent>(OnStaminaChanged);
            bus.Unsubscribe<MovementStateChangedEvent>(OnStateChanged);
            bus.Unsubscribe<InspectionStartedEvent>(OnInspectionStarted);
            bus.Unsubscribe<InspectionEndedEvent>(OnInspectionEnded);
            subscribed = false;
        }

        private void Update()
        {
            if (hideAt > 0f && Time.unscaledTime >= hideAt)
            {
                hideAt = 0f;
                HideHud();
            }
        }

        private void OnStaminaChanged(StaminaChangedEvent e)
        {
            if (slider != null && e.Max > 0f)
                slider.value = e.Current / e.Max;
            if (canvas != null && !isInspecting)
                canvas.enabled = true;
            hideAt = isInspecting ? 0f : Time.unscaledTime + 2.5f;
        }

        private void OnStateChanged(MovementStateChangedEvent e)
        {
            if (stateLabel != null)
                stateLabel.text = e.Current.ToString();
            if (canvas != null && !isInspecting)
                canvas.enabled = true;
        }

        private void OnInspectionStarted(InspectionStartedEvent _)
        {
            isInspecting = true;
            hideAt = 0f;
            if (canvas != null)
                canvas.enabled = false;
        }

        private void OnInspectionEnded(InspectionEndedEvent _)
        {
            isInspecting = false;
            // Tetap hidden sampai ada event stamina/state berikutnya (HUD minimal).
            if (canvas != null)
                canvas.enabled = false;
        }

        private void HideHud()
        {
            if (canvas != null && !isInspecting)
                canvas.enabled = false;
        }
    }
}
