using NUnit.Framework;
using Yumiwa.Application;

namespace Yumiwa.Application.Tests
{
    [TestFixture]
    public sealed class FootstepServiceTests
    {
        [Test]
        public void Tick_IdleOrAirborne_NeverSteps()
        {
            var svc = new FootstepService();
            Assert.IsFalse(svc.Tick(1f, 0f, true, false, false));
            Assert.IsFalse(svc.Tick(1f, 5f, false, false, false));
            Assert.IsFalse(svc.Tick(0f, 5f, true, false, false));
        }

        [Test]
        public void Tick_Walking_StepsOnCadence()
        {
            var svc = new FootstepService { WalkInterval = 0.5f };
            Assert.IsTrue(svc.Tick(0.6f, 2f, true, false, false)); // first beat immediate
            Assert.IsFalse(svc.Tick(0.2f, 2f, true, false, false));
            Assert.IsTrue(svc.Tick(0.3f, 2f, true, false, false));
        }

        [Test]
        public void Tick_Sprint_IsFasterThanWalk()
        {
            var walk = new FootstepService { WalkInterval = 0.5f, SprintInterval = 0.25f };
            var sprint = new FootstepService { WalkInterval = 0.5f, SprintInterval = 0.25f };
            int walkSteps = 0, sprintSteps = 0;
            for (int i = 0; i < 10; i++)
            {
                if (walk.Tick(0.1f, 2f, true, false, false)) walkSteps++;
                if (sprint.Tick(0.1f, 4f, true, true, false)) sprintSteps++;
            }
            Assert.IsTrue(sprintSteps > walkSteps);
        }

        [Test]
        public void Tick_Stopping_ResetsCadence()
        {
            var svc = new FootstepService { WalkInterval = 0.5f };
            Assert.IsTrue(svc.Tick(0.6f, 2f, true, false, false));
            Assert.IsFalse(svc.Tick(0.1f, 0f, true, false, false)); // stopped: reset
            Assert.IsTrue(svc.Tick(0.1f, 2f, true, false, false)); // immediate beat again
        }
    }
}
