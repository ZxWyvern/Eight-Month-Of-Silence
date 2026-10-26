using UnityEngine;
using VContainer;
using Yumiwa.Application;
using Yumiwa.Domain;
namespace Yumiwa.Presentation
{
    /// <summary>
    /// Death/retry presenter (B1-04 fail/retry): reloads the house checkpoint when
    /// one exists, otherwise falls back to the chapter save (Bab 1 has no house
    /// milestones by design). On cold boot with a chapter save but no house
    /// checkpoint, resumes the chapter position once instead of spawning fresh.
    /// Only repositions + resets health; world/inventory/evidence state comes
    /// from CheckpointService imports + CheckpointLoadedEvent reconciliation.
    /// </summary>
    public sealed class DeathReloadPresenter : MonoBehaviour
    {
        private IHealthService health;
        private ICheckpointService checkpoint;
        private bool reloading;
        private bool bootResumed;
        [Inject] public void Construct(IHealthService h, ICheckpointService c) { health=h; checkpoint=c; }
        void OnEnable() { if(health!=null) health.OnChanged += OnHealth; }
        void OnDisable() { if(health!=null) health.OnChanged -= OnHealth; }

        void Start()
        {
            // Cold-start resume: a chapter save without any house checkpoint
            // means the player returns to Bab 1 — restore it once.
            if (bootResumed || checkpoint == null || health == null)
                return;
            if (!checkpoint.HasCheckpoint && checkpoint.HasChapterSave)
            {
                bootResumed = true;
                var data = checkpoint.LoadChapter();
                ApplySnapshot(data);
                Debug.Log($"[Yumiwa] Resumed chapter save in {data.ActiveScene}.", this);
            }
        }

        void OnHealth(HealthChangedEvent e)
        {
            if (e.Current > 0 || reloading) return;
            reloading = true;
            Invoke(nameof(DoReload), 1f);
        }
        void DoReload()
        {
            CheckpointData data = default;
            bool have = false;
            if (checkpoint.HasCheckpoint) { data = checkpoint.LoadCheckpoint(); have = true; }
            else if (checkpoint.HasChapterSave) { data = checkpoint.LoadChapter(); have = true; }
            if (!have) { health.ResetHealth(); reloading=false; return; }
            ApplySnapshot(data);
            Debug.Log($"[Yumiwa] Death reload to {data.ActiveScene} milestone {data.Milestone}", this);
        }

        private void ApplySnapshot(CheckpointData data)
        {
            var cc = GetComponent<CharacterController>();
            if (cc) cc.enabled=false;
            transform.position = new Vector3(data.PosX, data.PosY, data.PosZ);
            if (data.RotW != 0 || data.RotX !=0) transform.rotation = new Quaternion(data.RotX,data.RotY,data.RotZ,data.RotW);
            if (cc) cc.enabled=true;
            Physics.SyncTransforms();
            health.ResetHealth();
            reloading=false;
        }
    }
}
