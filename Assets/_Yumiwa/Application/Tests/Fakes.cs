using System.Collections.Generic;
using Yumiwa.Domain;

namespace Yumiwa.Application.Tests
{
    /// <summary>Hand-rolled test doubles for Domain service boundaries.</summary>
    internal sealed class FakeTimeSource : ITimeSource
    {
        public float Now { get; set; }
    }

    internal sealed class StubAmmoReadModel : IAmmoReadModel
    {
        public StubAmmoReadModel(bool hasAmmo)
        {
            HasAmmo = hasAmmo;
        }

        public bool HasAmmo { get; set; }
    }

    internal sealed class FakeMovementStateMachine : IMovementStateMachine
    {
        public MovementState Current { get; set; } = MovementState.Idle;
        public float StateTime => 0f;
        public bool Grounded => true;
        public readonly List<MovementState> Requested = new List<MovementState>();
        public int ExitCalls;

        public bool RequestEnter(MovementState next) => RequestEnter(next, hasJumpObstacle: true);

        public bool RequestEnter(MovementState next, bool hasJumpObstacle)
        {
            Requested.Add(next);
            Current = next;
            return true;
        }

        public void RequestExit()
        {
            ExitCalls++;
            Current = MovementState.Idle;
        }

        public void SetGrounded(bool grounded) { }
        public void Tick(float deltaTime) { }
    }

    internal sealed class FakeInventoryService : IInventoryService
    {
        public bool Accept = true;
        public string RejectReason = "Inventory full.";
        public readonly System.Collections.Generic.Dictionary<string, int> Added =
            new System.Collections.Generic.Dictionary<string, int>();

        public bool CanAccept(ItemDefinition item, out string rejectReason)
        {
            rejectReason = Accept ? null : RejectReason;
            return Accept;
        }

        public bool TryAdd(ItemDefinition item, out string rejectReason)
        {
            if (!Accept)
            {
                rejectReason = RejectReason;
                return false;
            }
            if (Added.TryGetValue(item.ItemId, out int held))
                Added[item.ItemId] = held + item.Quantity;
            else
                Added[item.ItemId] = item.Quantity;
            rejectReason = null;
            return true;
        }

        public int GetCount(string itemId) =>
            string.IsNullOrEmpty(itemId) || !Added.TryGetValue(itemId, out int held) ? 0 : held;

        public bool IsInHand(string itemId) => GetCount(itemId) > 0;

        public string GetHandItemId(int handIndex) => null;

        public ItemDefinition[] ExportInventory()
        {
            var list = new System.Collections.Generic.List<ItemDefinition>();
            foreach (var kv in Added) list.Add(new ItemDefinition(kv.Key, kv.Key, "", kv.Value, ItemKind.PhysicalTool, false));
            return list.ToArray();
        }

        public void ImportInventory(ItemDefinition[] items)
        {
            Added.Clear();
            if (items == null) return;
            foreach (var it in items) if (ItemDefinition.IsValid(it)) Added[it.ItemId] = it.Quantity;
        }

        public void Clear() => Added.Clear();
    }
}
