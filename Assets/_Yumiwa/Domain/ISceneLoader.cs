namespace Yumiwa.Domain
{
    /// <summary>
    /// Scene-loading boundary per docs/ARCHITECTURE.md §4 (Mission row).
    /// Defined in Domain, implemented in Infrastructure (UnitySceneLoader),
    /// consumed by Application (SceneTransitionService). Same pattern as ISaveRepository.
    /// Engine-free signature: scenes are addressed by Build Settings name only, never by reference.
    /// </summary>
    public interface ISceneLoader
    {
        string ActiveSceneName { get; }

        System.Threading.Tasks.Task LoadSceneAsync(string sceneName);
    }
}
