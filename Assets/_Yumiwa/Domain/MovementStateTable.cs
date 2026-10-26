using System.Collections.Generic;

namespace Yumiwa.Domain
{
    /// <summary>O(1) data table for movement-state vital points; stamina costs are data-driven via MovementConfigData.</summary>
    public static class MovementStateTable
    {
        private static readonly Dictionary<MovementState, VitalPoint[]> VitalTable =
            new Dictionary<MovementState, VitalPoint[]>
            {
                { MovementState.Idle, new[] { VitalPoint.Torso, VitalPoint.Chest } },
                { MovementState.Walk, new[] { VitalPoint.Torso, VitalPoint.Chest } },
                { MovementState.Sprint, new[] { VitalPoint.Torso, VitalPoint.Chest } },
                { MovementState.Crouch, new[] { VitalPoint.Torso, VitalPoint.Chest } },
                { MovementState.Jump, new[] { VitalPoint.Neck, VitalPoint.UpperSpine } },
            };

        // DEPRECATED per ADR-0007: use Get(state, cfg) overload with MovementConfigData (ScriptableObject). Kept for test compat, do not use in new code.
        // DATA-001: hardcoded costs 3f/1f/5f are legacy fallback only; real tuning via MovementConfigData.StaminaCost* (SO).
        public static MovementStateData Get(MovementState state)
        {
            float cost;
            switch (state)
            {
                case MovementState.Sprint: cost = 3f; break;
                case MovementState.Crouch: cost = 1f; break;
                case MovementState.Jump: cost = 5f; break;
                default: cost = 0f; break;
            }
            return new MovementStateData(cost, VitalTable[state]);
        }

        public static MovementStateData Get(MovementState state, MovementConfigData cfg)
        {
            float cost;
            switch (state)
            {
                case MovementState.Sprint: cost = cfg.StaminaCostSprint; break;
                case MovementState.Crouch: cost = cfg.StaminaCostCrouch; break;
                case MovementState.Jump: cost = cfg.StaminaCostJump; break;
                default: cost = 0f; break;
            }
            return new MovementStateData(cost, VitalTable[state]);
        }

        // DEPRECATED per ADR-0007: use TryGet(state, cfg, out data). Kept for test compat.
        public static bool TryGet(MovementState state, out MovementStateData data)
        {
            float cost;
            switch (state)
            {
                case MovementState.Sprint: cost = 3f; break;
                case MovementState.Crouch: cost = 1f; break;
                case MovementState.Jump: cost = 5f; break;
                default: cost = 0f; break;
            }
            return TryGetWithCost(state, cost, out data);
        }

        public static bool TryGet(MovementState state, MovementConfigData cfg, out MovementStateData data)
        {
            float cost;
            switch (state)
            {
                case MovementState.Sprint: cost = cfg.StaminaCostSprint; break;
                case MovementState.Crouch: cost = cfg.StaminaCostCrouch; break;
                case MovementState.Jump: cost = cfg.StaminaCostJump; break;
                default: cost = 0f; break;
            }
            return TryGetWithCost(state, cost, out data);
        }

        private static bool TryGetWithCost(MovementState state, float cost, out MovementStateData data)
        {
            if (!VitalTable.TryGetValue(state, out var vitals))
            {
                data = default;
                return false;
            }
            data = new MovementStateData(cost, vitals);
            return true;
        }

        // Backward-compatible alias used by PlayerMovementState.CanEnter
        public static bool TryGetLegacy(MovementState state, MovementConfigData cfg, out MovementStateData data) => TryGet(state, cfg, out data);
    }
}
