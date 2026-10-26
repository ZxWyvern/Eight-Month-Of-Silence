using UnityEngine;

namespace Yumiwa.Presentation
{
    public sealed partial class PlayerMovementPresenter
    {
        private void ApplyLook(float dt)
        {
            // MVP-01: mouse sensitivity is a real setting, not decoration.
            float turn = movement.TurnSpeed * settings.MouseSensitivity;
            float yawStep = Mathf.Clamp(
                input.LookDeltaX * turn * dt,
                -movement.LookMaxDegreesPerFrame, movement.LookMaxDegreesPerFrame);
            if (orbitPivot != null)
                orbitPivot.Rotate(0f, yawStep, 0f);
            else
                transform.Rotate(0f, yawStep, 0f);
            if (cameraTarget != null)
            {
                float pitchStep = Mathf.Clamp(
                    input.LookDeltaY * turn * dt,
                    -movement.LookMaxDegreesPerFrame, movement.LookMaxDegreesPerFrame);
                cameraPitch = Mathf.Clamp(cameraPitch - pitchStep, -80f, 80f);
                cameraTarget.localEulerAngles = new Vector3(cameraPitch, 0f, StrafeRoll());
            }
        }

        private float StrafeRoll()
        {
            float strafe = input != null ? input.MoveDirection.X : 0f;
            return -strafe * movement.StrafeTiltDegrees;
        }

        private void ApplyCameraPose(float dt, bool grounded)
        {
            if (cameraTarget == null)
                return;
            float speed = planarVelocity.magnitude;
            if (settings.HeadBobEnabled && grounded && speed > 0.5f)
            {
                float freq = sprinting ? movement.HeadBobFreqSprint : movement.HeadBobFreqWalk;
                bobPhase += dt * freq * (0.6f + 0.4f * Mathf.Clamp01(speed / SprintSpeed()));
            }
            float bobY = 0f, bobX = 0f;
            if (settings.HeadBobEnabled)
            {
                bobY = Mathf.Sin(bobPhase * Mathf.PI * 2f) * movement.HeadBobAmplitude
                    * Mathf.Clamp01(speed / movement.WalkSpeed);
                bobX = Mathf.Cos(bobPhase * Mathf.PI) * movement.HeadBobAmplitude * 0.5f
                    * Mathf.Clamp01(speed / movement.WalkSpeed);
            }

            dipVelocity += (-dipOffset * 90f - dipVelocity * 12f) * dt;
            dipOffset += dipVelocity * dt;

            var pos = cameraTarget.localPosition;
            pos.x = Mathf.MoveTowards(pos.x, bobX, dt * 2f);
            float targetEye = movement.StandEyeHeight + bobY + dipOffset;
            if (crouching) targetEye = movement.StandEyeHeight * 0.62f + bobY * 0.5f + dipOffset;
            pos.y = Mathf.MoveTowards(pos.y, targetEye, dt * movement.EyeHeightSpeed)
                + dipOffset * dt * 8f;
            cameraTarget.localPosition = pos;

            ApplyFov(dt, grounded);
            if (wounded)
            {
                float bp = Time.time * 1.5f;
                cameraTarget.localPosition += new Vector3(
                    Mathf.Sin(bp) * 0.002f,
                    Mathf.Cos(bp * 1.3f) * 0.003f,
                    0f);
            }
            ApplyShake();
        }

        private void ApplyFov(float dt, bool grounded)
        {
            if (playerCamera == null)
                return;
            // baseFov tracks the settings FOV (see ApplySettings); boosts compose on top.
            float target = baseFov + landPunch;
            if (settings.FovBoostEnabled)
            {
                if (sprinting)
                    target += movement.SprintFovBoost;
                if (!grounded)
                    target += movement.AirFovBoost;
            }
            float t = 1f - Mathf.Exp(-movement.FovLerpSpeed * dt);
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, target, t);
        }

        private void ApplyShake()
        {
            if (playerCamera == null || cameraTarget == null)
                return;
            if (!settings.CameraShakeEnabled || shakeTimer <= 0f)
            {
                playerCamera.transform.localPosition = Vector3.zero;
                return;
            }
            float s = shakeTimer * 0.12f;
            float t = Time.time * 61f;
            playerCamera.transform.localPosition = new Vector3(
                Mathf.Sin(t * 1.1f) * s, Mathf.Cos(t * 1.7f) * s, 0f);
        }
    }
}
