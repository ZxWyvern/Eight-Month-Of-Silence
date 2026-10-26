using System;
using System.Collections.Generic;
using Yumiwa.Domain;

namespace Yumiwa.Application
{
    /// <summary>
    /// Auto-return for objective items per GDD §03:37 + ADR-0005/0012.
    /// Presentation registers restore callbacks per itemId; when EnqueueRecovery is called
    /// the service immediately invokes matching restores and publishes ItemRecoveredEvent.
    /// Also supports checkpoint-based resolve (LoadHouseMilestone triggers ResolveAll).
    /// </summary>
    public sealed class AutoReturnService : IRecoveryPoint
    {
        private readonly IEventBus bus;
        private readonly Dictionary<string, List<Action>> registry = new Dictionary<string, List<Action>>();
        private readonly List<ItemDefinition> pending = new List<ItemDefinition>();

        public AutoReturnService(IEventBus bus)
        {
            this.bus = bus;
        }

        public IReadOnlyList<ItemDefinition> PendingRecoveries => pending;

        public void Register(string itemId, Action restoreAction)
        {
            if (string.IsNullOrEmpty(itemId) || restoreAction == null) return;
            if (!registry.TryGetValue(itemId, out var list))
            {
                list = new List<Action>();
                registry[itemId] = list;
            }
            if (!list.Contains(restoreAction))
                list.Add(restoreAction);
        }

        public void Unregister(string itemId, Action restoreAction)
        {
            if (string.IsNullOrEmpty(itemId) || restoreAction == null) return;
            if (registry.TryGetValue(itemId, out var list))
            {
                list.Remove(restoreAction);
                if (list.Count == 0) registry.Remove(itemId);
            }
        }

        public void EnqueueRecovery(ItemDefinition item)
        {
            if (!item.HasIdentity) return;
            pending.Add(item);
            bus?.Publish(new ItemDroppedEvent(item.ItemId, item.IsObjectiveItem));
            // Immediate restore if world item is registered
            if (registry.TryGetValue(item.ItemId, out var list))
            {
                // Copy to avoid mutation during iteration
                var snapshot = new List<Action>(list);
                int restored = 0;
                foreach (var a in snapshot)
                {
                    try { a.Invoke(); restored++; } catch { /* guard: restore must not throw; failure keeps pending below */ }
                }
                if (restored == 0)
                    return; // Nobody could restore (e.g. only dead callbacks): keep pending for a later ResolveAll instead of silently dropping the item.
                bus?.Publish(new ItemRecoveredEvent(item.ItemId));
                // Remove from pending after successful restore
                pending.RemoveAll(x => x.ItemId == item.ItemId);
            }
        }

        public void ResolveAll()
        {
            // Re-attempt restore for all pending (e.g., after checkpoint load when world items re-registered)
            var toResolve = new List<ItemDefinition>(pending);
            pending.Clear();
            foreach (var item in toResolve)
            {
                EnqueueRecovery(item);
            }
        }

        public void Clear()
        {
            pending.Clear();
        }
    }
}
