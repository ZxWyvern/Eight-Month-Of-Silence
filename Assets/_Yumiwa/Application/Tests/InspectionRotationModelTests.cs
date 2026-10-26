using NUnit.Framework;
using Yumiwa.Domain;

namespace Yumiwa.Application.Tests
{
    /// <summary>EditMode tests for the pure inspection rotation/zoom model.</summary>
    [TestFixture]
    public sealed class InspectionRotationModelTests
    {
        private static InspectionRotationModel Create() =>
            new InspectionRotationModel(new InspectionTuningData(
                smoothing: 1000f,
                minPitch: -60f, maxPitch: 60f,
                minDistance: 0.35f, maxDistance: 2f,
                defaultDistance: 0.9f));

        [Test]
        public void RotateChangesYawAndPitch()
        {
            var model = Create();
            model.ApplyRotateDelta(10f, -5f);
            model.Tick(1f);
            Assert.AreEqual(10f, model.Yaw, 0.001f);
            Assert.AreEqual(-5f, model.Pitch, 0.001f);
        }

        [Test]
        public void PitchClampsAtLimits()
        {
            var model = Create();
            model.ApplyRotateDelta(0f, 500f);
            model.Tick(1f);
            Assert.AreEqual(60f, model.Pitch, 0.001f);
        }

        [Test]
        public void ZoomClampsAtMinAndMaxDistance()
        {
            var model = Create();
            model.ApplyZoomDelta(-100f);
            model.Tick(1f);
            Assert.AreEqual(0.35f, model.Distance, 0.001f);
            model.ApplyZoomDelta(100f);
            model.Tick(1f);
            Assert.AreEqual(2f, model.Distance, 0.001f);
        }

        [Test]
        public void ResetRestoresDefaults()
        {
            var model = Create();
            model.ApplyRotateDelta(45f, 20f);
            model.ApplyZoomDelta(-0.4f);
            model.Tick(1f);
            model.Reset();
            Assert.AreEqual(0f, model.Yaw, 0.001f);
            Assert.AreEqual(0f, model.Pitch, 0.001f);
            Assert.AreEqual(0.9f, model.Distance, 0.001f);
        }
    }
}
