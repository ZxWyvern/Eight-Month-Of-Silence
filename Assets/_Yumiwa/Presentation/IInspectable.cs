using UnityEngine;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Contract for world objects that can enter the inspection system.
    /// ADR-0018: now exposes Domain types + GameObject prefab, not Infrastructure.ItemData.
    /// </summary>
    public interface IInspectable
    {
        string ItemId { get; }
        ItemDefinition Definition { get; }
        Evidence LinkedEvidence { get; }
        InspectionHotspot[] Hotspots { get; }
        GameObject InspectionPrefab { get; }
        bool IsAvailable { get; }
        void Consume();
    }
}
