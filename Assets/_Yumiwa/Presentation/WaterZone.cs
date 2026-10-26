using UnityEngine;
using VContainer;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>Trigger volume flagging water overlap while the player is inside.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class WaterZone : MonoBehaviour
    {
        private IWaterProvider sensor;

        [Inject]
        public void Construct(IWaterProvider sensor)
        {
            this.sensor = sensor;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerMovementPresenter>() == null && !other.TryGetComponent<PlayerMovementPresenter>(out _)) return;
            if (sensor == null) TryResolveSensor();
            if (sensor == null)
            {
                Debug.LogWarning("[Yumiwa] WaterZone sensor not injected — add WaterZone_Demo under GameScope or to presenterRoots.", this);
                return;
            }
            sensor.Enter();
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<PlayerMovementPresenter>() == null && !other.TryGetComponent<PlayerMovementPresenter>(out _)) return;
            if (sensor == null) TryResolveSensor();
            if (sensor == null) return;
            sensor.Exit();
        }

        private void TryResolveSensor()
        {
            var scope = GetComponentInParent<VContainer.Unity.LifetimeScope>();
            if (scope != null && scope.Container != null)
            {
                try { sensor = scope.Container.Resolve<IWaterProvider>(); } catch (System.Exception e) { Debug.LogWarning($"[Yumiwa] WaterZone Resolve IWaterProvider failed: {e.Message}", this); }
            }
        }
    }
}
