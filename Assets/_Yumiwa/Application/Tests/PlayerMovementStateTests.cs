using NUnit.Framework;
using Yumiwa.Domain;

namespace Yumiwa.Application.Tests
{
    /// <summary>EditMode tests for the movement transition matrix.</summary>
    [TestFixture]
    public sealed class PlayerMovementStateTests
    {
        private static PlayerMovementState Fresh() => new PlayerMovementState();

        [Test]
        public void IdleToJumpRequiresFiveStamina()
        {
            var state = Fresh();
            state.SetGrounded(true);
            Assert.IsTrue(state.CanEnter(MovementState.Jump, 5f, true));
            Assert.IsFalse(state.CanEnter(MovementState.Jump, 4f, true));
        }

        [Test]
        public void AmmoHasNoEffectOnJumpButAllowsIdleExit()
        {
            var state = Fresh();
            state.SetGrounded(true);
            Assert.IsTrue(state.CanEnter(MovementState.Jump, 100f, false)); // Ammo doesn't block Jump
            state.Enter(MovementState.Jump);
            Assert.IsTrue(state.CanEnter(MovementState.Idle, 0f, false));
        }

        [Test]
        public void EnterResetsStateTimer()
        {
            var state = Fresh();
            state.SetGrounded(true);
            state.Enter(MovementState.Jump);
            state.Tick(0.4f);
            state.Enter(MovementState.Idle);
            Assert.AreEqual(0f, state.StateTime);
            Assert.AreEqual(MovementState.Idle, state.Current);
        }
    }
}
