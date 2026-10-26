namespace Yumiwa.Domain
{
    /// <summary>Published exactly once when a guarded scene transition starts loading.</summary>
    public readonly struct SceneTransitionStarted
    {
        public readonly string SceneName;

        public SceneTransitionStarted(string sceneName)
        {
            SceneName = sceneName;
        }
    }

    /// <summary>Published exactly once when the new scene finished loading.</summary>
    public readonly struct SceneTransitionCompleted
    {
        public readonly string SceneName;

        public SceneTransitionCompleted(string sceneName)
        {
            SceneName = sceneName;
        }
    }

    /// <summary>
    /// Published when a transition is rejected (empty name, already in progress)
    /// or the load itself fails. The player stays put — retry is always allowed,
    /// so this event must never lead to a stuck black screen (GDD-005).
    /// </summary>
    public readonly struct SceneTransitionFailed
    {
        public readonly string SceneName;
        public readonly string Reason;

        public SceneTransitionFailed(string sceneName, string reason)
        {
            SceneName = sceneName;
            Reason = reason;
        }
    }
}
