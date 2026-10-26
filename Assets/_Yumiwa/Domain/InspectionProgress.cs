using System.Collections.Generic;

namespace Yumiwa.Domain
{
    /// <summary>
    /// Pure progress tracking for inspection hotspots.
    /// Survives Cancel per GDD §03: "inspection bisa dibatalkan tanpa kehilangan progres hotspot yang sudah valid".
    /// Persisted per-item, not per-session.
    /// </summary>
    public sealed class InspectionProgress
    {
        private readonly HashSet<string> discovered = new HashSet<string>();

        public bool IsDiscovered(string hotspotId) => discovered.Contains(hotspotId);
        public IReadOnlyCollection<string> DiscoveredIds => discovered;
        public int DiscoveredCount => discovered.Count;

        public bool MarkDiscovered(string hotspotId)
        {
            if (string.IsNullOrEmpty(hotspotId))
                return false;
            return discovered.Add(hotspotId);
        }

        public void Clear() => discovered.Clear();
    }
}
