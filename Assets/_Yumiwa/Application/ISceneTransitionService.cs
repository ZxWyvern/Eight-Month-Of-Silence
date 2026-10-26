using System.Threading.Tasks;
using Yumiwa.Domain;

namespace Yumiwa.Application
{
    /// <summary>
    /// Guards door-portal scene transitions: one at a time, explicit Idle/Loading state,
    /// loud failure events. Fade in/out is owned by Presentation (ScreenFader);
    /// this service never touches visuals, only load legality and load ordering.
    /// </summary>
    public interface ISceneTransitionService
    {
        SceneTransitionState State { get; }
        bool IsTransitioning { get; }
        string CurrentTarget { get; }
        Task<bool> RequestTransitionAsync(string sceneName);
        void Reset();
    }
}
