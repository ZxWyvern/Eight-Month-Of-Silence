using System.Collections.Generic;
using Yumiwa.Domain;

namespace Yumiwa.Application
{
    /// <summary>
    /// Trap observation / trigger service per GDD §08 audio learnable cues.
    /// Damage goes through IHealthService; presenter plays consistent cue so player can learn.
    /// </summary>
    public sealed class TrapService
    {
        private readonly Dictionary<string, TrapDefinition> defs = new Dictionary<string, TrapDefinition>();
        private readonly HashSet<string> triggered = new HashSet<string>();
        private readonly HashSet<string> disarmed = new HashSet<string>();
        private readonly IHealthService health;
        private readonly IEventBus bus;

        public TrapService(IEnumerable<TrapDefinition> definitions, IHealthService health, IEventBus bus)
        {
            this.health = health;
            this.bus = bus;
            foreach (var d in definitions)
                if (d.IsValid)
                    defs[d.TrapId] = d;
        }

        public bool IsArmed(string trapId) => !string.IsNullOrEmpty(trapId) && !triggered.Contains(trapId) && !disarmed.Contains(trapId) && (defs.ContainsKey(trapId) || true); // P1: allow duplicated traps with new IDs (fallback def) — defs check removed for flexibility, only triggered/disarmed block

        /// <summary>Learnable warning — publishes cue without damage (GDD §08). Returns false if not armed.</summary>
        public bool Warn(string trapId)
        {
            if (!IsArmed(trapId))
                return false;
            TrapDefinition def;
            if (!defs.TryGetValue(trapId, out def))
                def = new TrapDefinition(trapId, trapId, 25f, "trap_click", "trap_board", true);
            bus?.Publish(new TrapWarningEvent(trapId, def.AudioCueId, def.VisualCueId));
            return true;
        }

        public bool TryDisarm(string trapId)
        {
            if (!IsArmed(trapId))
                return false;
            disarmed.Add(trapId);
            bus?.Publish(new TrapDisarmedEvent(trapId));
            return true;
        }

        public bool Trigger(string trapId)
        {
            if (!IsArmed(trapId))
                return false;
            triggered.Add(trapId);
            TrapDefinition def;
            if (!defs.TryGetValue(trapId, out def))
                def = new TrapDefinition(trapId, trapId, 25f, "trap_click", "trap_board", true);
            health.TakeDamage(def.Damage);
            bus?.Publish(new TrapTriggeredEvent(trapId, def.Damage, def.AudioCueId, def.VisualCueId));
            return true;
        }

        public void Reset(string trapId)
        {
            triggered.Remove(trapId);
            disarmed.Remove(trapId);
        }
    }

    public readonly struct TrapTriggeredEvent
    {
        public readonly string TrapId; public readonly float Damage; public readonly string AudioCue; public readonly string VisualCue;
        public TrapTriggeredEvent(string id, float dmg, string audio, string visual) { TrapId = id; Damage = dmg; AudioCue = audio; VisualCue = visual; }
    }
    public readonly struct TrapDisarmedEvent { public readonly string TrapId; public TrapDisarmedEvent(string id) => TrapId = id; }
    /// <summary>Published on warning — cue is learnable, appears before damage (C4).</summary>
    public readonly struct TrapWarningEvent
    {
        public readonly string TrapId; public readonly string AudioCue; public readonly string VisualCue;
        public TrapWarningEvent(string id, string audio, string visual) { TrapId = id; AudioCue = audio; VisualCue = visual; }
    }
}
