using UnityEngine;
using VContainer;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Drives inspection orientation from the inspection input map through the
    /// pure InspectionRotationModel. Runs only while enabled by the controller
    /// (no Update cost otherwise) and uses unscaled time so pause never freezes
    /// the preview. Zoom dollies the camera; rotation turns the pivot.
    /// ADR-0018: no longer references Infrastructure.
    /// </summary>
    public sealed class ItemInspectionRotator : MonoBehaviour
    {
        [SerializeField] private Camera inspectionCamera;
        [SerializeField] private Transform pivot;

        private InspectionRotationModel model;
        private InspectionTuningData tuning;
        private InspectionInputHandler input;

        [Inject]
        public void Construct(InspectionTuningData tuning, InspectionInputHandler input)
        {
            this.tuning = tuning;
            this.input = input;
            model = new InspectionRotationModel(tuning);
        }

        public InspectionRotationModel Model => model;

        private void Awake()
        {
            if (model == null)
                model = new InspectionRotationModel(tuning);
            enabled = false;
        }

        public void ResetView()
        {
            if (model == null)
                model = new InspectionRotationModel(tuning);
            model.Reset();
            ApplyToScene();
        }

        /// <summary>
        /// Kamera inspeksi default MATI di scene (hemat render pass + tidak
        /// menimpa MainCamera). Controller menyalakan hanya saat inspection.
        /// </summary>
        public void SetCameraEnabled(bool enabled)
        {
            if (inspectionCamera != null)
                inspectionCamera.enabled = enabled;
        }

        private void Update()
        {
            if (model == null || input == null)
                return;
            float dt = Time.unscaledDeltaTime;
            Vector2 rotate = input.RotateDelta;
            if (input.IsRotateFromGamepad())
                model.ApplyRotateDelta(
                    rotate.x * tuning.StickDegreesPerSecond * dt,
                    rotate.y * tuning.StickDegreesPerSecond * dt);
            else
                model.ApplyRotateDelta(
                    rotate.x * tuning.MouseDegreesPerPixel,
                    rotate.y * tuning.MouseDegreesPerPixel);
            float zoom = input.ZoomDelta;
            if (input.IsZoomFromGamepad())
                model.ApplyZoomDelta(-zoom * tuning.PadZoomRate * dt);
            else
                model.ApplyZoomDelta(-zoom * tuning.MouseZoomStep);
            model.Tick(dt);
            ApplyToScene();
        }

        private void ApplyToScene()
        {
            if (pivot == null || inspectionCamera == null)
                return;
            pivot.rotation = Quaternion.Euler(model.Pitch, model.Yaw, 0f);
            Vector3 toCamera = inspectionCamera.transform.position - pivot.position;
            if (toCamera.sqrMagnitude < 0.000001f)
                toCamera = -pivot.forward;
            inspectionCamera.transform.position =
                pivot.position + toCamera.normalized * model.Distance;
        }
    }
}
