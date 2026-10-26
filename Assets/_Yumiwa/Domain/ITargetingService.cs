using System.Collections.Generic;

namespace Yumiwa.Domain
{
    /// <summary>Service contract resolving which vital points a movement state exposes.</summary>
    public interface ITargetingService
    {
        IReadOnlyList<VitalPoint> GetExposedVitalPoints(MovementState state);
    }
}
