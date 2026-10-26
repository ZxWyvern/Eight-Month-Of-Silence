using System.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Door portal: stand in range + press Use (same button as PuzzleView) →
    /// fade out → guarded single scene load → destination fader fades back in.
    /// The target scene is data (inspector string = Build Settings name), never hardcoded,
    /// per docs/09 data-driven content. Mission advance is optional (advanceOnEnter /
    /// advanceMissionId, same convention as CheckpointTrigger); on load failure the
    /// mission index rolls back and the screen fades back in, so the run can never
    /// get stuck (docs/03 anti-frustrasi, GDD-005). No async void: the input callback
    /// is sync and hands off to a Task-returning method (PATTERN-005).
    /// Injection: preferred path is GameLifetimeScope.presenterRoots (ADR-0009);
    /// TryResolveDependencies mirrors TrapZone (ADR-0014) as self-heal fallback when
    /// the door was placed outside presenterRoots.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class DoorScenePortal : MonoBehaviour
    {
        [Tooltip("Build Settings scene name, e.g. M02_Motel. Must also be added via File > Build Settings.")]
        [SerializeField] private string targetSceneName = "";
        [Tooltip("Fader in THIS scene. If unassigned the cut is instant, the load still goes through the guarded service.")]
        [SerializeField] private ScreenFader fader;
        [SerializeField] private bool advanceOnEnter = true;
        [Tooltip("Optional explicit mission jump (e.g. M02_Motel). Empty = auto Advance().")]
        [SerializeField] private string advanceMissionId = "";

        private ISceneTransitionService _transition;
        private IPlayerInput _input;
        private MissionFlowService _missionFlow;
        private bool _playerInRange;
        private bool _subscribed;

        [Inject]
        public void Construct(ISceneTransitionService transition, IPlayerInput input, MissionFlowService missionFlow = null)
        {
            bool wasNull = _input == null;
            _transition = transition;
            _input = input;
            _missionFlow = missionFlow;
            // Construct may run after OnEnable when VContainer injects late:
            // ensure subscription if we are already enabled.
            if (wasNull && _input != null && isActiveAndEnabled)
                SubscribeInput();
        }

        // Prototype fallback without DI (same convention as CheckpointTrigger).
        public void SetService(ISceneTransitionService svc) => _transition = svc;
        public void SetInput(IPlayerInput input)
        {
            _input = input;
            if (_input != null && isActiveAndEnabled)
                SubscribeInput();
        }
        public void SetMissionFlow(MissionFlowService flow) => _missionFlow = flow;
        public void SetFader(ScreenFader screenFader) => fader = screenFader;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void Awake()
        {
            if (fader == null)
                Debug.LogWarning("[Yumiwa] DoorScenePortal has no ScreenFader — transition will cut instantly.", this);
            if (_transition == null || _input == null)
                TryResolveDependencies();
            if (_missionFlow == null)
                Debug.LogWarning("[Yumiwa] DoorScenePortal missionFlow not injected — mission will not advance on enter.", this);
        }

        private void TryResolveDependencies()
        {
            LifetimeScope scope = GetComponentInParent<LifetimeScope>();
            if (scope == null)
                scope = FindFirstObjectByType<LifetimeScope>();
            if (scope == null || scope.Container == null)
                return;
            try
            {
                var container = scope.Container;
                try { _transition ??= container.Resolve(typeof(ISceneTransitionService)) as ISceneTransitionService; }
                catch (System.Exception e) { Debug.LogWarning("[Yumiwa] DoorScenePortal Resolve ISceneTransitionService failed: " + e.Message, this); }
                try { _input ??= container.Resolve(typeof(IPlayerInput)) as IPlayerInput; }
                catch (System.Exception e) { Debug.LogWarning("[Yumiwa] DoorScenePortal Resolve IPlayerInput failed: " + e.Message, this); }
                try { _missionFlow ??= container.Resolve(typeof(MissionFlowService)) as MissionFlowService; }
                catch { /* optional — mission advance stays disabled */ }
            }
            catch (System.Exception e) { Debug.LogWarning("[Yumiwa] DoorScenePortal Resolve failed: " + e.Message, this); }
        }

        private void SubscribeInput()
        {
            if (_subscribed || _input == null)
                return;
            _input.UsePressed += OnUsePressed;
            _subscribed = true;
        }

        private void UnsubscribeInput()
        {
            if (!_subscribed || _input == null)
                return;
            _input.UsePressed -= OnUsePressed;
            _subscribed = false;
        }

        private void OnEnable()
        {
            if (_input == null)
                TryResolveDependencies();
            SubscribeInput();
        }

        private void OnDisable()
        {
            UnsubscribeInput();
        }

        private static bool IsPlayer(Collider other)
        {
            if (other.TryGetComponent<PlayerMovementPresenter>(out _))
                return true;
            return other.GetComponentInParent<PlayerMovementPresenter>() != null;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other))
                return;
            _playerInRange = true;
            Debug.Log("[Yumiwa] Door → '" + targetSceneName + "': tekan [F] Use untuk masuk.", this);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other))
                return;
            _playerInRange = false;
        }

        private void OnUsePressed()
        {
            if (!_playerInRange)
                return;
            if (_transition == null)
            {
                Debug.LogError("[Yumiwa] DoorScenePortal transition service missing (not injected).", this);
                return;
            }
            if (_transition.IsTransitioning)
                return;
            if (string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogError("[Yumiwa] DoorScenePortal targetSceneName is empty — set the destination scene name.", this);
                return;
            }
            _ = EnterAsync();
        }

        private async Task EnterAsync()
        {
            int prevMission = _missionFlow != null ? _missionFlow.CurrentIndex : -1;
            bool advanced = AdvanceMission();

            if (fader != null)
                await fader.FadeOutAsync();

            bool ok = false;
            try
            {
                ok = await _transition.RequestTransitionAsync(targetSceneName);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Yumiwa] Door transition threw: " + e.Message, this);
                ok = false;
            }

            if (ok)
            {
                Debug.Log("[Yumiwa] Door entered '" + targetSceneName + "'.", this);
                return; // Destination scene's ScreenFader (startOpaque + fadeInOnStart) fades back in.
            }

            if (advanced)
                RollbackMission(prevMission);
            Debug.LogWarning("[Yumiwa] Door to '" + targetSceneName + "' failed — faded back in, retry allowed.", this);
            if (fader != null)
                await fader.FadeInAsync();
        }

        private bool AdvanceMission()
        {
            if (!advanceOnEnter || _missionFlow == null)
                return false;
            if (!string.IsNullOrEmpty(advanceMissionId))
            {
                var seq = _missionFlow.Sequence;
                for (int i = 0; i < seq.Count; i++)
                {
                    if (seq[i].MissionId == advanceMissionId)
                    {
                        _missionFlow.SetIndex(i);
                        return true;
                    }
                }
                Debug.LogWarning("[Yumiwa] Door advanceMissionId '" + advanceMissionId + "' not in sequence.", this);
                return false;
            }
            if (_missionFlow.CanAdvance)
            {
                _missionFlow.Advance();
                return true;
            }
            return false;
        }

        private void RollbackMission(int prevMission)
        {
            if (_missionFlow == null)
                return;
            if (prevMission >= 0)
            {
                _missionFlow.SetIndex(prevMission);
                return;
            }
            Debug.LogWarning("[Yumiwa] Door load failed after mission advance with no prior index — check MissionSequence.", this);
        }
    }
}
