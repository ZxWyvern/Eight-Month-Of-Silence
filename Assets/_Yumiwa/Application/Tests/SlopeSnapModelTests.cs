using NUnit.Framework;
using Yumiwa.Domain;

namespace Yumiwa.Application.Tests
{
    /// <summary>EditMode tests for slope-stick snap decisions (ramps stick, ledges fall).</summary>
    [TestFixture]
    public sealed class SlopeSnapModelTests
    {
        [Test]
        public void GentleDownhillSnapAcceptedWithExactDelta()
        {
            bool ok = SlopeSnapModel.TryComputeSnap(
                1.84f, 1.78f, 15.8f, 45f, -2f, 0.45f, out float delta);
            Assert.IsTrue(ok);
            Assert.AreEqual(-0.06f, delta, 0.001f);
        }

        [Test]
        public void SteepWallRejected()
        {
            bool ok = SlopeSnapModel.TryComputeSnap(
                1f, 0.9f, 80f, 45f, -2f, 0.45f, out _);
            Assert.IsFalse(ok);
        }

        [Test]
        public void RealLedgeDropRejected()
        {
            bool ok = SlopeSnapModel.TryComputeSnap(
                5f, 0f, 0f, 45f, -2f, 0.45f, out _);
            Assert.IsFalse(ok);
        }

        [Test]
        public void RisingJumpNeverSnaps()
        {
            bool ok = SlopeSnapModel.TryComputeSnap(
                1f, 0.95f, 10f, 45f, 6.9f, 0.45f, out _);
            Assert.IsFalse(ok);
        }

        [Test]
        public void GroundAboveFeetNeverPullsUp()
        {
            bool ok = SlopeSnapModel.TryComputeSnap(
                1f, 1.2f, 10f, 45f, -2f, 0.45f, out _);
            Assert.IsFalse(ok);
        }
    }
}
