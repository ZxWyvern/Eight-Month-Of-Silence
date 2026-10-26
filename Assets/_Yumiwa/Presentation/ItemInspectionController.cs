using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Owns ItemInspectionState: opens inspection on interact, coordinates
    /// presenter/rotator/UI/input, confirms Take exactly once via the session
    /// service, and restores gameplay input on exit. World item stays untouched
    /// until Take succeeds; Cancel never mutates inventory or the world.
    /// ADR-0018: now uses IInspectable.Definition/Evidence/Hotspots/InspectionPrefab (Domain) not Infrastructure.ItemData.
    /// Split via partial Flow.cs per SIZE-001 (272 → ~140 + 140).
    /// </summary>
    public sealed partial class ItemInspectionController : MonoBehaviour
    {
        [SerializeField] private ItemInspectionPresenter presenter;
        [SerializeField] private ItemInspectionRotator rotator;
        [SerializeField] private ItemInspectionUI ui;

        private InspectionSessionService session;
        private InspectionInputHandler inspectionInput;
        private InputActionAsset asset;
        private IEventBus bus;
        private InputActionMap playerMap;

        [SerializeField] private InspectionHotspotPresenter hotspotPresenter;

        private System.Action<InspectionTakeFailedEvent> takeFailedHandler;
        private System.Action examinePressedHandler;
        private IInspectable source;
        private bool active;
        private bool takeInFlight;
        private float inspectionStartTime;

        public bool IsInspecting => active;

        [Inject]
        public void Construct(
            InspectionSessionService session,
            InspectionInputHandler inspectionInput,
            InputActionAsset asset,
            IEventBus bus)
        {
            this.session = session;
            this.inspectionInput = inspectionInput;
            this.asset = asset;
            this.bus = bus;
            if (hotspotPresenter == null)
                hotspotPresenter = GetComponentInChildren<InspectionHotspotPresenter>(true);
            if (hotspotPresenter == null)
                hotspotPresenter = GetComponent<InspectionHotspotPresenter>();
        }

        private void Start()
        {
            if (hotspotPresenter == null)
                hotspotPresenter = GetComponentInChildren<InspectionHotspotPresenter>(true);
            if (hotspotPresenter == null)
                hotspotPresenter = GetComponent<InspectionHotspotPresenter>();
            if (hotspotPresenter == null)
            {
                var hpGO = new GameObject("Auto_HotspotPresenter");
                hpGO.transform.SetParent(transform, false);
                hotspotPresenter = hpGO.AddComponent<InspectionHotspotPresenter>();
                try { hotspotPresenter.Construct(session, rotator != null ? rotator.Model : null); } catch (System.Exception e) { Debug.LogWarning($"[Yumiwa] Auto HotspotPresenter Construct failed: {e.Message}", this); }
            }
            if (hotspotPresenter != null && rotator != null)
            {
                try { hotspotPresenter.SetRotationModel(rotator.Model); } catch (System.Exception e) { Debug.LogWarning($"[Yumiwa] HotspotPresenter SetRotationModel failed: {e.Message}", this); }
            }
            if (presenter == null || rotator == null || ui == null)
            {
                Debug.LogError("[Yumiwa] ItemInspectionController references unassigned.", this);
                enabled = false;
                return;
            }
            if (asset != null)
                playerMap = asset.FindActionMap("Player");
            rotator.SetCameraEnabled(false);
            if (bus == null || session == null || inspectionInput == null)
            {
                Debug.LogError("[Yumiwa] ItemInspectionController injection missing.", this);
                enabled = false;
                return;
            }
            takeFailedHandler = OnTakeFailed;
            bus.Subscribe(takeFailedHandler);
            ui.OnTakeClicked += OnTakePressed;
            ui.OnBackClicked += OnCancelPressed;
        }

        private void OnDestroy()
        {
            if (bus != null && takeFailedHandler != null)
                bus.Unsubscribe(takeFailedHandler);
            if (ui != null)
            {
                ui.OnTakeClicked -= OnTakePressed;
                ui.OnBackClicked -= OnCancelPressed;
            }
            if (active)
                Teardown();
        }
    }
}
