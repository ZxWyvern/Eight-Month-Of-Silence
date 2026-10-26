using UnityEngine;
using VContainer;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Bab 1 entry trigger (B1-04): writes a chapter save (GDD §52, never a house
    /// milestone) and notifies the objective chain. Presentation only packs the
    /// player pose and calls Application services — no mission rules live here.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class ChapterSavePoint : MonoBehaviour
    {
        [SerializeField] private string pointId = "b1_entry";
        [Tooltip("Fire once, then disable (entry points). Uncheck for re-save zones.")]
        [SerializeField] private bool fireOnce = true;

        private ICheckpointService checkpoint;
        private IHealthService health;
        private IStaminaService stamina;
        private Act1ObjectiveChain chain;

        [Inject]
        public void Construct(
            ICheckpointService checkpoint,
            IHealthService health,
            IStaminaService stamina,
            Act1ObjectiveChain chain = null)
        {
            this.checkpoint = checkpoint;
            this.health = health;
            this.stamina = stamina;
            this.chain = chain;
        }

        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent<PlayerMovementPresenter>(out _)) return;
            if (checkpoint == null)
            {
                Debug.LogWarning($"[Yumiwa] ChapterSavePoint '{pointId}' has no checkpoint service.", this);
                return;
            }
            var t = other.transform;
            var data = new CheckpointData
            {
                PosX = t.position.x,
                PosY = t.position.y,
                PosZ = t.position.z,
                RotX = t.rotation.x,
                RotY = t.rotation.y,
                RotZ = t.rotation.z,
                RotW = t.rotation.w,
                Health = health != null ? health.Current : 100f,
                Stamina = stamina != null ? stamina.Current : 100f,
                ActiveScene = gameObject.scene.name,
                Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            checkpoint.SaveChapter(data);
            chain?.OnEnterCrimeScene();
            Debug.Log($"[Yumiwa] Chapter saved at '{pointId}'.", this);
            if (fireOnce)
            {
                var col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
        }
    }
}
