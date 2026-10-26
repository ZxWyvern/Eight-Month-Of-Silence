using System.Collections.Generic;
using Yumiwa.Domain;

namespace Yumiwa.Application
{
    /// <summary>O(1) targeting lookup keyed by movement state.</summary>
    public sealed class TargetingService : ITargetingService
    {
        public IReadOnlyList<VitalPoint> GetExposedVitalPoints(MovementState state) =>
            MovementStateTable.Get(state).ExposedVitalPoints;
    }
}
