using UnityEngine;
using UnityEngine.InputSystem;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    public sealed partial class ItemInspectionController
    {
        /// <summary>Entry point from PlayerInteractor. Ignored while busy.</summary>
        public void BeginInspection(IInspectable inspectable)
        {
            if (active || session == null)
            {
                Debug.LogWarning($"[Yumiwa] BeginInspection ignored (active={active}, sessionNull={session == null}).", this);
                return;
            }
            if (!IsAlive(inspectable) || !inspectable.IsAvailable)
            {
                Debug.LogWarning("[Yumiwa] BeginInspection ignored: inspectable not available.", this);
                return;
            }
            ItemDefinition definition = inspectable.Definition;
            if (!ItemDefinition.IsValid(definition))
            {
                Debug.LogWarning($"[Yumiwa] IInspectable '{(inspectable as Object)?.name}' produced an invalid definition; ignoring.", inspectable as Object);
                return;
            }
            var evidence = inspectable.LinkedEvidence;
            var hotspots = inspectable.Hotspots;
            if (!session.Begin(definition, evidence, hotspots))
            {
                Debug.LogWarning("[Yumiwa] BeginInspection ignored: session busy or item invalid.", this);
                return;
            }

            source = inspectable;
            active = true;
            takeInFlight = false;
            inspectionStartTime = Time.unscaledTime;
            Debug.Log($"[Yumiwa] Inspecting '{definition.DisplayName}'.", this);

            if (playerMap != null)
                playerMap.Disable();
            inspectionInput.Enable();
            inspectionInput.TakePressed += OnTakePressed;
            inspectionInput.CancelPressed += OnCancelPressed;
            inspectionInput.ResetRotationPressed += OnResetPressed;
            examinePressedHandler = OnExaminePressed;
            inspectionInput.ExaminePressed += examinePressedHandler;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            bool shown = presenter.Show(inspectable.InspectionPrefab);
            rotator.ResetView();
            rotator.SetCameraEnabled(true);
            rotator.enabled = true;
            ui.Bind(definition);
            if (!shown)
                ui.ShowMessage("Preview unavailable for this item.");
            ui.Show();
        }

        private void OnTakePressed()
        {
            if (!active || takeInFlight)
                return;
            if (Time.unscaledTime - inspectionStartTime < 0.3f)
            {
                Debug.Log("[Yumiwa] Take ignored — debounce after inspection open.", this);
                return;
            }
            takeInFlight = true;
            InspectionTakeResult result = session.ConfirmTake();
            if (result == InspectionTakeResult.Success)
            {
                IInspectable taken = source;
                source = null;
                if (IsAlive(taken))
                {
                    try
                    {
                        taken.Consume();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[Yumiwa] Consume failed after successful Take: {e.Message}");
                    }
                }
                session.EndAfterTake();
                Teardown();
            }
            else if (result == InspectionTakeResult.RejectedByInventory)
            {
                takeInFlight = false;
            }
            else
            {
                takeInFlight = false;
            }
        }

        private void OnCancelPressed()
        {
            if (!active)
                return;
            source = null;
            session.Cancel();
            Teardown();
        }

        private void OnResetPressed()
        {
            if (active && rotator != null)
                rotator.ResetView();
        }

        private void OnExaminePressed()
        {
            if (!active || hotspotPresenter == null)
                return;
            bool ok = hotspotPresenter.ExamineAnyValid();
            if (ui != null)
            {
                if (ok)
                    ui.ShowMessage("Detail ditemukan!");
                else
                    ui.ShowMessage("Tidak ada detail di sudut ini.");
            }
            Debug.Log($"[Yumiwa] Examine pressed — result={ok}.", this);
        }

        private void OnTakeFailed(InspectionTakeFailedEvent evt)
        {
            if (active && ui != null)
                ui.ShowMessage(evt.Reason);
        }

        private void Teardown()
        {
            inspectionInput.TakePressed -= OnTakePressed;
            inspectionInput.CancelPressed -= OnCancelPressed;
            inspectionInput.ResetRotationPressed -= OnResetPressed;
            if (examinePressedHandler != null)
                inspectionInput.ExaminePressed -= examinePressedHandler;
            inspectionInput.Disable();
            inspectionStartTime = 0f;
            if (presenter != null)
                presenter.Hide();
            if (rotator != null)
            {
                rotator.enabled = false;
                rotator.SetCameraEnabled(false);
            }
            if (ui != null)
                ui.Hide();
            if (playerMap != null)
                playerMap.Enable();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            source = null;
            active = false;
            takeInFlight = false;
        }

        private static bool IsAlive(IInspectable inspectable)
        {
            if (inspectable == null)
                return false;
            if (inspectable is Object unityObject)
                return unityObject != null;
            return true;
        }
    }
}
