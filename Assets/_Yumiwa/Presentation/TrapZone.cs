using UnityEngine;
using VContainer;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Prototype trap trigger per GDD §03/08 — learnable audio/visual cue before damage (C4 fix).
    /// Warning appears immediately on enter, damage after delay, player can Observe & Avoid.
    /// Disarm via Use cancels warning and prevents damage. Exit before delay cancels damage.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class TrapZone : MonoBehaviour
    {
        [SerializeField] private string trapId = "trap_board_01";
        [SerializeField] private AudioSource cueSource;
        [SerializeField] private GameObject visualCue;
        [Tooltip("Learnable warning delay before damage — player can observe cue and avoid (GDD §08). <!-- TBD#? placeholder 1.8s -->")]
        [SerializeField] private float warningDelay = 1.8f;

        private TrapService trapService;
        private IPlayerInput playerInput;
        private bool playerNearArmed;
        private bool warningActive;
        private Coroutine pendingTrigger;

        [Inject]
        public void Construct(TrapService trapService, IPlayerInput playerInput)
        {
            this.trapService = trapService;
            this.playerInput = playerInput;
        }

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void Awake()
        {
            if (cueSource == null) cueSource = GetComponent<AudioSource>();
            if (visualCue == null)
            {
                var t = transform.Find("TrapVisualCue");
                if (t != null) visualCue = t.gameObject;
                else if (transform.childCount > 0) visualCue = transform.GetChild(0).gameObject;
            }
            if (visualCue != null) visualCue.SetActive(false);
            if (trapService == null || playerInput == null) TryResolveDependencies();
        }

        private void TryResolveDependencies()
        {
            var scope = GetComponentInParent<VContainer.Unity.LifetimeScope>();
            if (scope == null) scope = FindFirstObjectByType<VContainer.Unity.LifetimeScope>();
            if (scope != null)
            {
                try
                {
                    var container = scope.Container;
                    if (container != null)
                    {
                        try { trapService ??= container.Resolve(typeof(TrapService)) as TrapService; } catch (System.Exception e) { Debug.LogWarning($"[Yumiwa] TrapZone Resolve TrapService failed: {e.Message}", this); }
                        try { playerInput ??= container.Resolve(typeof(IPlayerInput)) as IPlayerInput; } catch (System.Exception e) { Debug.LogWarning($"[Yumiwa] TrapZone Resolve IPlayerInput failed: {e.Message}", this); }
                    }
                }
                catch (System.Exception e) { Debug.LogWarning($"[Yumiwa] TrapZone Resolve failed: {e.Message}", this); }
            }
            if (trapService == null)
            {
                // Fallback: find any TrapZone's already injected service as last resort
                var anyZone = FindFirstObjectByType<TrapZone>();
                if (anyZone != null && anyZone != this)
                {
                    var f = typeof(TrapZone).GetField("trapService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var svc = f.GetValue(anyZone) as TrapService;
                    if (svc != null) trapService = svc;
                }
            }
        }

        private void OnEnable()
        {
            if (playerInput != null)
                playerInput.UsePressed += OnUsePressed;
            else TryResolveDependencies();
        }

        private void OnDisable()
        {
            if (playerInput != null)
                playerInput.UsePressed -= OnUsePressed;
            if (pendingTrigger != null) { StopCoroutine(pendingTrigger); pendingTrigger = null; }
            warningActive = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerMovementPresenter>() == null && !other.TryGetComponent<PlayerMovementPresenter>(out _))
                return;
            if (trapService == null)
            {
                Debug.LogWarning("[Yumiwa] TrapZone injection missing.", this);
                return;
            }
            if (!trapService.IsArmed(trapId))
                return;
            playerNearArmed = true;
            trapService.Warn(trapId);
            if (cueSource != null)
            {
                cueSource.loop = true;
                if (!cueSource.isPlaying) cueSource.Play();
            }
            if (visualCue != null)
                visualCue.SetActive(true);
            warningActive = true;
            if (pendingTrigger != null) StopCoroutine(pendingTrigger);
            pendingTrigger = StartCoroutine(TriggerRoutine());
            Debug.Log($"[Yumiwa] Trap '{trapId}' warning — cue before damage {warningDelay}s (learnable).", this);
        }

        private System.Collections.IEnumerator TriggerRoutine()
        {
            yield return new WaitForSecondsRealtime(warningDelay);
            TriggerDamage();
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<PlayerMovementPresenter>() == null && !other.TryGetComponent<PlayerMovementPresenter>(out _))
                return;
            if (warningActive && trapService != null && trapService.IsArmed(trapId))
            {
                if (pendingTrigger != null) { StopCoroutine(pendingTrigger); pendingTrigger = null; }
                if (cueSource != null) cueSource.Stop();
                if (visualCue != null) visualCue.SetActive(false);
                warningActive = false;
                Debug.Log($"[Yumiwa] Trap '{trapId}' avoided — exited warning zone.", this);
            }
            playerNearArmed = false;
        }

        private void TriggerDamage()
        {
            pendingTrigger = null;
            warningActive = false;
            if (trapService == null || !trapService.IsArmed(trapId)) return;
            if (!playerNearArmed) return;
            trapService.Trigger(trapId);
            if (cueSource != null) cueSource.Stop();
            Debug.Log($"[Yumiwa] Trap '{trapId}' triggered after warning.", this);
        }

        private void OnUsePressed()
        {
            if (playerNearArmed && trapService != null && trapService.IsArmed(trapId))
            {
                Disarm();
                Debug.Log($"[Yumiwa] Trap '{trapId}' disarmed by player.", this);
            }
        }

        public void Disarm()
        {
            if (pendingTrigger != null) { StopCoroutine(pendingTrigger); pendingTrigger = null; }
            warningActive = false;
            trapService?.TryDisarm(trapId);
            if (cueSource != null)
                cueSource.Stop();
            if (visualCue != null)
                visualCue.SetActive(false);
            playerNearArmed = false;
        }
    }
}
