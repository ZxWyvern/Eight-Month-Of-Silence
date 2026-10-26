namespace Yumiwa.Domain
{
    /// <summary>Plain-data movement tuning snapshot (no UnityEngine dependency).
    /// Values tuned toward Grounded Horror (Pilar 1); final numbers remain TBD per GDD §03.
    /// Stamina costs are data-driven here (not hardcoded in MovementStateTable) per DATA-001.</summary>
    public readonly struct MovementConfigData
    {
        public readonly float WalkSpeed;
        public readonly float SprintMultiplier;
        public readonly float JumpHeight;
        public readonly float Gravity;
        public readonly float TurnSpeed;
        public readonly float LookMaxDegreesPerFrame;
        public readonly float GroundStickForce;
        public readonly float TerminalVelocity;
        public readonly float FaceTurnSpeed;
        public readonly float StandEyeHeight;
        public readonly float EyeHeightSpeed;
        public readonly float GroundAccel;
        public readonly float GroundDecel;
        public readonly float AirAccel;
        public readonly float AirDrag;
        public readonly float CoyoteSeconds;
        public readonly float JumpBufferSeconds;
        public readonly float JumpCutMultiplier;
        public readonly float WaterMoveMultiplier;
        public readonly float WaterJumpMultiplier;
        public readonly float WaterGravityMultiplier;
        public readonly float SprintFovBoost;
        public readonly float AirFovBoost;
        public readonly float FovLerpSpeed;
        public readonly float HeadBobAmplitude;
        public readonly float HeadBobFreqWalk;
        public readonly float HeadBobFreqSprint;
        public readonly float LandDipDepth;
        public readonly float StrafeTiltDegrees;
        public readonly float SlopeSnapDistance;
        public readonly float LandDipMinFallSpeed;
        // Data-driven stamina costs per state (replaces hardcoded MovementStateTable).
        public readonly float StaminaCostSprint;
        public readonly float StaminaCostCrouch;
        public readonly float StaminaCostJump;
        // Grounded horror modifiers
        public readonly float CrouchSpeedMultiplier;
        public readonly bool LimitedJumpOnlyAtObstacle;
        public readonly float JumpObstacleCheckDistance;

        public MovementConfigData(
            float walkSpeed, float sprintMultiplier, float jumpHeight, float gravity,
            float turnSpeed, float lookMaxDegreesPerFrame,
            float groundStickForce, float terminalVelocity, float faceTurnSpeed,
            float standEyeHeight, float eyeHeightSpeed,
            float groundAccel = 25f, float groundDecel = 20f,
            float airAccel = 5f, float airDrag = 1.5f,
            float coyoteSeconds = 0f, float jumpBufferSeconds = 0f, float jumpCutMultiplier = 0.45f,
            float waterMoveMultiplier = 0.5f, float waterJumpMultiplier = 0.7f, float waterGravityMultiplier = 0.45f,
            float sprintFovBoost = 3f,
            float airFovBoost = 2f, float fovLerpSpeed = 8f,
            float headBobAmplitude = 0.05f, float headBobFreqWalk = 9f, float headBobFreqSprint = 12.5f,
            float landDipDepth = 0.16f, float strafeTiltDegrees = 2f,
            float slopeSnapDistance = 0.45f, float landDipMinFallSpeed = 3.5f,
            float staminaCostSprint = 3f, float staminaCostCrouch = 1f, float staminaCostJump = 5f,
            float crouchSpeedMultiplier = 0.55f, bool limitedJumpOnlyAtObstacle = true, float jumpObstacleCheckDistance = 1.2f)
        {
            WalkSpeed = walkSpeed;
            SprintMultiplier = sprintMultiplier;
            JumpHeight = jumpHeight;
            Gravity = gravity;
            TurnSpeed = turnSpeed;
            LookMaxDegreesPerFrame = lookMaxDegreesPerFrame;
            GroundStickForce = groundStickForce;
            TerminalVelocity = terminalVelocity;
            FaceTurnSpeed = faceTurnSpeed;
            StandEyeHeight = standEyeHeight;
            EyeHeightSpeed = eyeHeightSpeed;
            GroundAccel = groundAccel;
            GroundDecel = groundDecel;
            AirAccel = airAccel;
            AirDrag = airDrag;
            CoyoteSeconds = coyoteSeconds;
            JumpBufferSeconds = jumpBufferSeconds;
            JumpCutMultiplier = jumpCutMultiplier;
            WaterMoveMultiplier = waterMoveMultiplier;
            WaterJumpMultiplier = waterJumpMultiplier;
            WaterGravityMultiplier = waterGravityMultiplier;
            SprintFovBoost = sprintFovBoost;
            AirFovBoost = airFovBoost;
            FovLerpSpeed = fovLerpSpeed;
            HeadBobAmplitude = headBobAmplitude;
            HeadBobFreqWalk = headBobFreqWalk;
            HeadBobFreqSprint = headBobFreqSprint;
            LandDipDepth = landDipDepth;
            StrafeTiltDegrees = strafeTiltDegrees;
            SlopeSnapDistance = slopeSnapDistance;
            LandDipMinFallSpeed = landDipMinFallSpeed;
            StaminaCostSprint = staminaCostSprint;
            StaminaCostCrouch = staminaCostCrouch;
            StaminaCostJump = staminaCostJump;
            CrouchSpeedMultiplier = crouchSpeedMultiplier;
            LimitedJumpOnlyAtObstacle = limitedJumpOnlyAtObstacle;
            JumpObstacleCheckDistance = jumpObstacleCheckDistance;
        }

        public static MovementConfigData Defaults() => new MovementConfigData(3.5f,1.3f,0.8f,-15f,3000f,5f,2f,-30f,6f,1.6f,4f);
    }
}
