using NUnit.Framework;
using Yumiwa.Domain;

namespace Yumiwa.Application.Tests
{
    /// <summary>EditMode tests for machine orchestration: spend, exits, events.</summary>
    [TestFixture]
    public sealed class MovementStateMachineTests
    {
        private FakeTimeSource clock;
        private EventBus bus;
        private StubAmmoReadModel ammo;
        private MovementStateMachine machine;
        private StaminaService stamina;

        [SetUp]
        public void SetUp()
        {
            clock = new FakeTimeSource();
            bus = new EventBus();
            ammo = new StubAmmoReadModel(true);
            stamina = new StaminaService(new StaminaPool(100f, 10f, 1.5f), clock, bus);
            machine = new MovementStateMachine(
                new PlayerMovementState(), stamina, ammo, bus);
        }

        [Test]
        public void EnterJumpSpendsFiveAndPublishesTransition()
        {
            MovementStateChangedEvent evt = default;
            bus.Subscribe<MovementStateChangedEvent>(e => evt = e);
            machine.SetGrounded(true); // Jump requires grounded
            Assert.IsTrue(machine.RequestEnter(MovementState.Jump));
            Assert.AreEqual(MovementState.Jump, machine.Current);
            Assert.AreEqual(95f, stamina.Current); // 100 - 5 cost = 95
            Assert.AreEqual(MovementState.Idle, evt.Previous);
            Assert.AreEqual(MovementState.Jump, evt.Current);
        }

        [Test]
        public void LandingExitsJump()
        {
            machine.SetGrounded(true);
            machine.RequestEnter(MovementState.Jump);
            // Simulate air
            machine.SetGrounded(false);
            // Simulate landing
            machine.SetGrounded(true);
            Assert.AreEqual(MovementState.Idle, machine.Current);
        }

        [Test]
        public void InsufficientStaminaDeniesEntry()
        {
            machine.SetGrounded(true);
            stamina.TrySpend(96f); // 4 remaining, jump needs 5
            Assert.IsFalse(machine.RequestEnter(MovementState.Jump));
            Assert.AreEqual(MovementState.Idle, machine.Current);
        }

        [Test]
        public void ReenteringSameStateDoesNotSpendStamina()
        {
            machine.SetGrounded(true);
            Assert.IsTrue(machine.RequestEnter(MovementState.Sprint)); // Costs 3f
            Assert.AreEqual(97f, stamina.Current);
            Assert.IsTrue(machine.RequestEnter(MovementState.Sprint)); // Should not cost anything
            Assert.AreEqual(97f, stamina.Current);
        }
    }
}
