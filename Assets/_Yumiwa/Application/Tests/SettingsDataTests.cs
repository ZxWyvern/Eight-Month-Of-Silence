using NUnit.Framework;
using Yumiwa.Domain;

namespace Yumiwa.Application.Tests
{
    /// <summary>First-run settings defaults must not zoom or freeze the player camera.</summary>
    [TestFixture]
    public sealed class SettingsDataTests
    {
        [Test]
        public void Defaults_UseAuthoredCameraFov()
        {
            SettingsData settings = SettingsData.Defaults();
            Assert.AreEqual(60f, settings.Fov,
                "First-run FOV must match the authored camera to avoid a startup zoom.");
        }

        [Test]
        public void Defaults_UsePlayableMouseSensitivity()
        {
            SettingsData settings = SettingsData.Defaults();
            Assert.AreEqual(1f, settings.MouseSensitivity,
                "Zero sensitivity freezes look on the first run.");
        }

        [Test]
        public void Defaults_KeepPresentationTogglesOff()
        {
            SettingsData settings = SettingsData.Defaults();
            Assert.IsFalse(settings.HeadBobEnabled);
            Assert.IsFalse(settings.CameraShakeEnabled);
            Assert.IsFalse(settings.FovBoostEnabled);
        }
    }
}
