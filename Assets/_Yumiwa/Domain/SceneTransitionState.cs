namespace Yumiwa.Domain
{
    /// <summary>
    /// Explicit transition states for door portals (docs/CODING_STANDARDS.md §3).
    /// Only two states by design: fade timing is owned by Presentation (ScreenFader),
    /// so Application only guards "is a load already running".
    /// </summary>
    public enum SceneTransitionState
    {
        Idle = 0,
        Loading = 1
    }
}
