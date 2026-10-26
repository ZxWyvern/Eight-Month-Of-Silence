namespace Yumiwa.Domain
{
    /// <summary>Abstraction over the clock so stamina timing stays unit-testable.</summary>
    public interface ITimeSource
    {
        float Now { get; }
    }
}
