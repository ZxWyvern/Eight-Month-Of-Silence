using NUnit.Framework;
using Yumiwa.Domain;
using Yumiwa.Infrastructure;

namespace Yumiwa.Application.Tests
{
    /// <summary>EditMode tests pinning SO defaults to rules.json tunables.</summary>
    [TestFixture]
    public sealed class ConfigDefaultsTests
    {
        [Test]
        public void StaminaDefaultsMirrorRuleset()
        {
            var config = UnityEngine.ScriptableObject.CreateInstance<StaminaConfig>();
            Assert.AreEqual(100f, config.MaxStamina);
            Assert.AreEqual(10f, config.RegenPerSecond);
            Assert.AreEqual(1.5f, config.RegenDelaySeconds);
            Assert.AreEqual(8f, config.SprintDrainPerSecond);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void MovementLocomotionDefaultsAreSane()
        {
            var config = UnityEngine.ScriptableObject.CreateInstance<MovementConfig>();
            Assert.Greater(config.WalkSpeed, 0f);
            Assert.Greater(config.SprintMultiplier, 1f);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void MovementFeelDefaultsAreSaneAssumptions()
        {
            var config = UnityEngine.ScriptableObject.CreateInstance<MovementConfig>();
            Assert.Greater(config.GroundAccel, config.AirAccel);
            Assert.Greater(config.CoyoteSeconds, 0f);
            Assert.Greater(config.JumpBufferSeconds, 0f);
            Assert.Greater(config.JumpCutMultiplier, 0f);
            Assert.Less(config.JumpCutMultiplier, 1f);
            Assert.Greater(config.WaterMoveMultiplier, 0f);
            Assert.Less(config.WaterMoveMultiplier, 1f);
            Assert.Greater(config.SlopeSnapDistance, 0f);
            Assert.Greater(config.LandDipMinFallSpeed, 0f);
            var data = config.ToData();
            Assert.AreEqual(config.WalkSpeed, data.WalkSpeed);
            Assert.AreEqual(config.CoyoteSeconds, data.CoyoteSeconds);
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void DirectProviderDeliversMatchingSnapshots()
        {
            var movement = UnityEngine.ScriptableObject.CreateInstance<MovementConfig>();
            var stamina = UnityEngine.ScriptableObject.CreateInstance<StaminaConfig>();
            IConfigProvider provider = new DirectConfigProvider(movement, stamina);
            Assert.AreEqual(movement.WalkSpeed, provider.GetMovement().WalkSpeed);
            Assert.AreEqual(stamina.MaxStamina, provider.GetStamina().MaxStamina);
            UnityEngine.Object.DestroyImmediate(movement);
            UnityEngine.Object.DestroyImmediate(stamina);
        }
    }
}
