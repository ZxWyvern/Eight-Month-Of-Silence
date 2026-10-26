using UnityEngine;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    public sealed partial class PlayerMovementPresenter
    {
        private void HandleLandingTransition(bool grounded, float dt)
        {
            if (grounded)
            {
                if (!wasGrounded)
                {
                    if (fallPeak < -movement.LandDipMinFallSpeed)
                    {
                        float impact = Mathf.Clamp01(-fallPeak / 15f);
                        dipVelocity = -Mathf.Max(1.5f, impact * 7f);
                        landPunch = 2f + impact * 6f;
                        shakeTimer = Mathf.Max(shakeTimer, 0.05f + impact * 0.12f);
                    }
                }
                if (verticalVelocity <= 0f)
                    verticalVelocity = -movement.GroundStickForce;
                fallPeak = 0f;
                coyoteTimer = movement.CoyoteSeconds;
            }
            else
            {
                if (wasGrounded && verticalVelocity <= 0.01f)
                {
                    airMaxSpeed = Mathf.Max(SprintSpeed(), planarVelocity.magnitude);
                }
                coyoteTimer -= dt;
                if (verticalVelocity < fallPeak)
                    fallPeak = verticalVelocity;
            }
            if (jumpBufferTimer > 0f)
                jumpBufferTimer -= dt;
            if (landPunch > 0f)
                landPunch = Mathf.Max(0f, landPunch - dt * 24f);
            if (shakeTimer > 0f)
                shakeTimer -= dt;
        }

        private void UpdateJump(float dt, bool grounded)
        {
            if (jumpBufferTimer <= 0f)
                return;
            if (!(grounded || coyoteTimer > 0f))
                return;
            bool hasObstacle = HasJumpObstacle();
            // Domain now owns LimitedJump gate (P1-M4); keep early-exit for clarity but also pass flag to machine so Domain is authoritative.
            if (movement.LimitedJumpOnlyAtObstacle && !hasObstacle)
                return;
            if (!machine.RequestEnter(MovementState.Jump, hasObstacle))
                return;
            float jumpScale = water != null && water.IsInWater ? movement.WaterJumpMultiplier : 1f;
            verticalVelocity = AirborneMomentum.JumpSpeed(movement.Gravity, movement.JumpHeight) * jumpScale;
            airMaxSpeed = Mathf.Max(SprintSpeed(), planarVelocity.magnitude);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        private bool HasJumpObstacle()
        {
            if (movement.JumpObstacleCheckDistance <= 0f)
                return true;
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Vector3 dir = transform.forward;
            if (Physics.Raycast(origin, dir, out var hit, movement.JumpObstacleCheckDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                float h = hit.point.y - (transform.position.y);
                return h > 0.15f && h < 1.2f;
            }
            return false;
        }



        private void ApplyMovement(float dt, bool grounded)
        {
            Vector3 wishDir = ToWorld(input.MoveDirection);
            if (grounded)
            {
                bool isCrouch = machine.Current == MovementState.Crouch;
                float target = movement.WalkSpeed
                    * (sprinting ? movement.SprintMultiplier : 1f)
                    * (isCrouch ? movement.CrouchSpeedMultiplier : 1f)
                    * WoundedModifier()
                    * WaterMoveScale();
                Vector3 goal = wishDir * target;
                float rate = wishDir.sqrMagnitude > 0.001f
                    ? movement.GroundAccel
                    : movement.GroundDecel;
                planarVelocity = Vector3.MoveTowards(planarVelocity, goal, rate * dt);
            }
            else
            {
                float px = planarVelocity.x;
                float pz = planarVelocity.z;
                AirborneMomentum.ApplyAirControl(
                    ref px, ref pz, wishDir.x, wishDir.z,
                    movement.AirAccel, movement.AirDrag, dt);
                float cap = Mathf.Max(airMaxSpeed, SprintSpeed() * WoundedModifier() * WaterMoveScale());
                float mag = Mathf.Sqrt(px * px + pz * pz);
                if (mag > cap && mag > 0.001f)
                {
                    float scale = cap / mag;
                    px *= scale;
                    pz *= scale;
                }
                planarVelocity = new Vector3(px, 0f, pz);
            }

            if (grounded)
            {
                verticalVelocity = Mathf.Max(verticalVelocity, -movement.GroundStickForce);
            }
            else
            {
                float gravScale = water != null && water.IsInWater
                    ? movement.WaterGravityMultiplier
                    : 1f;
                verticalVelocity = Mathf.Max(
                    verticalVelocity + movement.Gravity * gravScale * dt,
                    movement.TerminalVelocity);
            }

            controller.Move((planarVelocity + Vector3.up * verticalVelocity) * dt);

            if (orbitPivot == null)
                return;
            Vector3 face = planarVelocity;
            if (face.sqrMagnitude > 0.01f)
            {
                var target = Quaternion.LookRotation(new Vector3(face.x, 0f, face.z));
                float t = 1f - Mathf.Exp(-movement.FaceTurnSpeed * dt);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, t);
            }
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static bool IsClickPressed()
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
        }

        private bool TrySlopeSnap()
        {
            if (verticalVelocity > 0.01f || movement.SlopeSnapDistance <= 0f)
                return false;
            float feetY = transform.position.y + controller.center.y - controller.height * 0.5f;
            Vector3 origin = new Vector3(transform.position.x, feetY + 0.25f, transform.position.z);
            if (!Physics.Raycast(
                    origin, Vector3.down, out RaycastHit hit,
                    0.25f + movement.SlopeSnapDistance,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return false;
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (!SlopeSnapModel.TryComputeSnap(
                    feetY, hit.point.y, slopeAngle, controller.slopeLimit,
                    verticalVelocity, movement.SlopeSnapDistance, out float deltaY))
                return false;
            controller.Move(new Vector3(0f, deltaY, 0f));
            verticalVelocity = -movement.GroundStickForce;
            return true;
        }

        private Vector3 ToWorld(PlanarDirection dir)
        {
            float yaw = (orbitPivot != null ? orbitPivot.eulerAngles.y : transform.eulerAngles.y) * Mathf.Deg2Rad;
            float sin = Mathf.Sin(yaw);
            float cos = Mathf.Cos(yaw);
            return new Vector3(dir.X * cos + dir.Y * sin, 0f, -dir.X * sin + dir.Y * cos);
        }
    }
}
