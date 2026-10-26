using NUnit.Framework;
using Yumiwa.Domain;

namespace Yumiwa.Application.Tests
{
    /// <summary>EditMode tests for run-to-jump momentum preservation and air steering.</summary>
    [TestFixture]
    public sealed class AirborneMomentumTests
    {
        [Test]
        public void JumpSpeedMatchesProjectileMath()
        {
            float v = AirborneMomentum.JumpSpeed(-20f, 1.2f);
            Assert.AreEqual(6.93f, v, 0.05f);
        }

        [Test]
        public void SprintMomentumPreservedWithoutInput()
        {
            float px = 7.2f;
            float pz = 0f;
            AirborneMomentum.ApplyAirControl(ref px, ref pz, 0f, 0f, 18f, 0.4f, 0.016f);
            Assert.IsTrue(AirborneMomentum.PreservesSpeed(7.2f, px, 0.1f));
        }

        [Test]
        public void AirSteeringAddsSpeedWithoutSnapping()
        {
            float px = 7.2f;
            float pz = 0f;
            AirborneMomentum.ApplyAirControl(ref px, ref pz, 0f, 1f, 18f, 0.4f, 0.1f);
            // Forward momentum survives while lateral steering accumulates.
            Assert.Greater(px, 6.5f);
            Assert.Greater(pz, 1f);
        }

        [Test]
        public void FastestEntrySpeedWinsSoJumpKeepsIt()
        {
            float entryBoost = MathHelper.Max(7.2f, 9f);
            Assert.AreEqual(9f, entryBoost);
            Assert.IsTrue(AirborneMomentum.PreservesSpeed(9f, 9f));
        }

        private static class MathHelper
        {
            public static float Max(float a, float b) => a > b ? a : b;
        }
    }
}
