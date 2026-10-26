using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Inspection-map input behind a sampled boundary.
    /// ADR-0018: moved from Infrastructure to Presentation to remove Presentation→Infrastructure dependency for this input helper.
    /// Enabled only while ItemInspectionState is active.
    /// </summary>
    public sealed class InspectionInputHandler : IDisposable
    {
        private readonly InputActionMap map;
        private readonly InputAction rotate;
        private readonly InputAction zoom;
        private readonly InputAction take;
        private readonly InputAction cancel;
        private readonly InputAction resetRotation;
        private readonly InputAction examine;

        public event Action TakePressed;
        public event Action CancelPressed;
        public event Action ResetRotationPressed;
        public event Action ExaminePressed;

        public InspectionInputHandler(InputActionAsset asset)
        {
            map = asset != null ? asset.FindActionMap("Inspection") : null;
            if (map == null)
                throw new InvalidOperationException(
                    "Missing InputActionMap 'Inspection'. Add the Inspection map to the InputActionAsset.");
            rotate = Require("InspectRotate");
            zoom = Require("InspectZoom");
            take = Require("InspectTake");
            cancel = Require("InspectCancel");
            resetRotation = Require("InspectResetRotation");
            examine = Require("InspectExamine");

            take.performed += _ => TakePressed?.Invoke();
            cancel.performed += _ => CancelPressed?.Invoke();
            resetRotation.performed += _ => ResetRotationPressed?.Invoke();
            examine.performed += _ => ExaminePressed?.Invoke();
        }

        public Vector2 RotateDelta => rotate.ReadValue<Vector2>();
        public float ZoomDelta => zoom.ReadValue<Vector2>().y;
        public bool IsRotateFromGamepad() => IsGamepad(rotate);
        public bool IsZoomFromGamepad() => IsGamepad(zoom);
        public void Enable() => map.Enable();
        public void Disable() => map.Disable();
        public void Dispose()
        {
            TakePressed = null;
            CancelPressed = null;
            ResetRotationPressed = null;
            ExaminePressed = null;
        }
        private InputAction Require(string actionName)
        {
            var action = map.FindAction(actionName);
            if (action == null)
                throw new InvalidOperationException($"Missing InputAction 'Inspection/{actionName}'.");
            return action;
        }
        private static bool IsGamepad(InputAction action)
        {
            var control = action.activeControl;
            return control != null && control.device is Gamepad;
        }
    }
}
