using System;
using System.Collections.Generic;

namespace Yumiwa.Domain
{
    /// <summary>
    /// Anti soft-lock recovery point per GDD §03 "Objective item tidak boleh hilang permanen".
    /// Domain-owned interface, implemented in Application. Presentation registers a restore callback
    /// (pure System.Action, no Unity reference) so Domain/Application never knows GameObject.
    /// </summary>
    public interface IRecoveryPoint
    {
        void Register(string itemId, Action restoreAction);
        void Unregister(string itemId, Action restoreAction);
        void EnqueueRecovery(ItemDefinition item);
        IReadOnlyList<ItemDefinition> PendingRecoveries { get; }
        void ResolveAll();
        void Clear();
    }
}
