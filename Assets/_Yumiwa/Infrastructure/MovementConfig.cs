using UnityEngine;
using Yumiwa.Domain;

namespace Yumiwa.Infrastructure
{
    /// <summary>ScriptableObject movement tunables; defaults tuned toward Grounded Horror (Pilar 1).
    /// Final numbers remain TBD per GDD §03 — these are baseline grounded values.</summary>
    [CreateAssetMenu(menuName = "Yumiwa/MovementConfig", fileName = "MovementConfig")]
    public sealed class MovementConfig : ScriptableObject
    {
        [Header("Locomotion")]
        public float WalkSpeed = 3.5f;
        public float SprintMultiplier = 1.3f;
        public float JumpHeight = 0.8f;
        public float Gravity = -15f;
        public float TurnSpeed = 3000f;
        public float LookMaxDegreesPerFrame = 5f;
        public float GroundStickForce = 2f;
        public float TerminalVelocity = -30f;
        public float FaceTurnSpeed = 6f;
        public float StandEyeHeight = 1.6f;
        public float EyeHeightSpeed = 4f;

        [Header("Momentum / air feel (Pilar 1: grounded — small coyote/buffer keeps strictness but passes ConfigDefaults pinning)")]
        public float GroundAccel = 25f;
        public float GroundDecel = 20f;
        public float AirAccel = 5f;
        public float AirDrag = 1.5f;
        public float CoyoteSeconds = 0.12f;
        public float JumpBufferSeconds = 0.15f;
        public float JumpCutMultiplier = 0.45f;

        [Header("Water (*)")]
        public float WaterMoveMultiplier = 0.5f;
        public float WaterJumpMultiplier = 0.7f;
        public float WaterGravityMultiplier = 0.45f;

        [Header("Camera juice (Pilar 1: subtle)")]
        public float SprintFovBoost = 3f;
        public float AirFovBoost = 2f;
        public float FovLerpSpeed = 8f;
        public float HeadBobAmplitude = 0.05f;
        public float HeadBobFreqWalk = 9f;
        public float HeadBobFreqSprint = 12.5f;
        public float LandDipDepth = 0.16f;
        public float StrafeTiltDegrees = 2f;

        [Header("Slope stick (*)")]
        public float SlopeSnapDistance = 0.45f;
        public float LandDipMinFallSpeed = 3.5f;

        [Header("Stamina Costs (data-driven, not hardcoded)")]
        public float StaminaCostSprint = 3f;
        public float StaminaCostCrouch = 1f;
        public float StaminaCostJump = 5f;

        [Header("Grounded Horror Modifiers")]
        [Range(0.1f, 1f)] public float CrouchSpeedMultiplier = 0.55f;
        public bool LimitedJumpOnlyAtObstacle = true;
        public float JumpObstacleCheckDistance = 1.2f;

        public MovementConfigData ToData() => new MovementConfigData(
            WalkSpeed, SprintMultiplier, JumpHeight, Gravity,
            TurnSpeed, LookMaxDegreesPerFrame,
            GroundStickForce, TerminalVelocity, FaceTurnSpeed,
            StandEyeHeight, EyeHeightSpeed,
            GroundAccel, GroundDecel, AirAccel, AirDrag,
            CoyoteSeconds, JumpBufferSeconds, JumpCutMultiplier,
            WaterMoveMultiplier, WaterJumpMultiplier, WaterGravityMultiplier,
            SprintFovBoost,
            AirFovBoost, FovLerpSpeed,
            HeadBobAmplitude, HeadBobFreqWalk, HeadBobFreqSprint,
            LandDipDepth, StrafeTiltDegrees,
            SlopeSnapDistance, LandDipMinFallSpeed,
            StaminaCostSprint, StaminaCostCrouch, StaminaCostJump,
            CrouchSpeedMultiplier, LimitedJumpOnlyAtObstacle, JumpObstacleCheckDistance);
    }
}
