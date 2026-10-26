using System.Threading.Tasks;
using Yumiwa.Domain;

namespace Yumiwa.Application
{
    /// <summary>
    /// Explicit Idle → Loading → Idle guard for door scene transitions
    /// (docs/05 Bab 4-5 stage changes, docs/09 stage-based scenes).
    /// Rejects empty names and re-entrant requests with a SceneTransitionFailed event
    /// so the caller can fade back in — the player is never left on a black screen
    /// with no recovery path (docs/03 anti-frustrasi, GDD-005).
    /// Engine-free: the actual load is delegated to ISceneLoader (no UnityEngine here).
    /// Note: awaits stay on the caller's synchronization context (no ConfigureAwait(false))
    /// because event subscribers are Presentation code that must run on Unity's main thread.
    /// </summary>
    public sealed class SceneTransitionService : ISceneTransitionService
    {
        private readonly ISceneLoader _loader;
        private readonly IEventBus _bus;
        private SceneTransitionState _state;
        private string _currentTarget;

        public SceneTransitionService(ISceneLoader loader, IEventBus bus)
        {
            _loader = loader;
            _bus = bus;
        }

        public SceneTransitionState State => _state;

        public bool IsTransitioning => _state != SceneTransitionState.Idle;

        public string CurrentTarget => _currentTarget ?? string.Empty;

        public async Task<bool> RequestTransitionAsync(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Fail(string.Empty, "Empty scene name.");
                return false;
            }
            if (_state != SceneTransitionState.Idle)
            {
                Fail(sceneName, "Transition already in progress.");
                return false;
            }
            if (_loader == null)
            {
                Fail(sceneName, "No scene loader registered.");
                return false;
            }

            _state = SceneTransitionState.Loading;
            _currentTarget = sceneName;
            if (_bus != null)
                _bus.Publish(new SceneTransitionStarted(sceneName));

            try
            {
                await _loader.LoadSceneAsync(sceneName);
            }
            catch (System.Exception e)
            {
                Fail(sceneName, e.Message);
                ResetToIdle();
                return false;
            }

            ResetToIdle();
            if (_bus != null)
                _bus.Publish(new SceneTransitionCompleted(sceneName));
            return true;
        }

        public void Reset() => ResetToIdle();

        private void ResetToIdle()
        {
            _state = SceneTransitionState.Idle;
            _currentTarget = null;
        }

        private void Fail(string sceneName, string reason)
        {
            if (_bus != null)
                _bus.Publish(new SceneTransitionFailed(sceneName, reason));
        }
    }
}
