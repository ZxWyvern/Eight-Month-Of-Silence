using NUnit.Framework;
using Yumiwa.Domain;

namespace Yumiwa.Application.Tests
{
    /// <summary>EditMode tests pinning movement costs and vital-point exposure.</summary>
    [TestFixture]
    public sealed class MovementStateTableTests
    {
        [TestCase(MovementState.Idle, 0f)]
        [TestCase(MovementState.Jump, 5f)]
        public void CostsMatchRuleset(MovementState state, float expectedCost)
        {
            Assert.AreEqual(expectedCost, MovementStateTable.Get(state).StaminaCost);
        }

        [Test]
        public void JumpExposesNeckAndUpperSpine()
        {
            CollectionAssert.AreEqual(
                new[] { VitalPoint.Neck, VitalPoint.UpperSpine },
                MovementStateTable.Get(MovementState.Jump).ExposedVitalPoints);
        }
    }
}
