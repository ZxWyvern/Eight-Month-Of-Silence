namespace Yumiwa.Domain
{
    /// <summary>
    /// Presentation contract for puzzle interactability — allows InteractionPromptPresenter
    /// to query without reflection (ADR-0012 C5 GC fix: 0B per-frame Update).
    /// </summary>
    public interface IPuzzleView
    {
        string PuzzleId { get; }
        bool IsSolved { get; }
        bool CanAttempt { get; }
    }
}
