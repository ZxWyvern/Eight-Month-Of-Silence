using System.IO;
using NUnit.Framework;
using Yumiwa.Domain;
using Yumiwa.Infrastructure;

namespace Yumiwa.Application.Tests
{
    /// <summary>MVP-01 settings persistence: one JSON store, no PlayerPrefs split.</summary>
    [TestFixture]
    public sealed class SettingsRepositoryTests
    {
        private string dir;

        [SetUp]
        public void SetUp()
        {
            dir = Path.Combine(Path.GetTempPath(), "yumiwa_settings_test_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch { }
        }

        [Test]
        public void Load_MissingFile_ReturnsDefaults()
        {
            var store = new JsonFileSettingsRepository(dir);
            var loaded = ((ISettingsStore)store).Load();
            Assert.AreEqual(SettingsData.Defaults().Fov, loaded.Fov);
            Assert.AreEqual(SettingsData.Defaults().MouseSensitivity, loaded.MouseSensitivity);
        }

        [Test]
        public void SaveThenLoad_RoundTripsAllFields()
        {
            var store = new JsonFileSettingsRepository(dir);
            var expected = new SettingsData(
                headBobEnabled: true,
                cameraShakeEnabled: true,
                fovBoostEnabled: true,
                subtitlesEnabled: false,
                mouseSensitivity: 2.25f,
                fov: 72f,
                textSize: DialogueTextSize.Large,
                subtitleBackgroundOpacity: 0.85f,
                readingSpeed: DialogueReadingSpeed.Fast);

            ((ISettingsStore)store).Save(expected);
            var loaded = ((ISettingsStore)store).Load();

            Assert.AreEqual(expected.HeadBobEnabled, loaded.HeadBobEnabled);
            Assert.AreEqual(expected.CameraShakeEnabled, loaded.CameraShakeEnabled);
            Assert.AreEqual(expected.FovBoostEnabled, loaded.FovBoostEnabled);
            Assert.AreEqual(expected.SubtitlesEnabled, loaded.SubtitlesEnabled);
            Assert.AreEqual(expected.MouseSensitivity, loaded.MouseSensitivity);
            Assert.AreEqual(expected.Fov, loaded.Fov);
            Assert.AreEqual(expected.TextSize, loaded.TextSize);
            Assert.AreEqual(expected.SubtitleBackgroundOpacity, loaded.SubtitleBackgroundOpacity);
            Assert.AreEqual(expected.ReadingSpeed, loaded.ReadingSpeed);
            Assert.AreEqual(0, Directory.GetFiles(dir, "*.tmp").Length);
        }

        [Test]
        public void Load_CorruptFile_ReturnsDefaults()
        {
            File.WriteAllText(Path.Combine(dir, "yumiwa_settings.json"), "{");
            var store = new JsonFileSettingsRepository(dir);
            var loaded = ((ISettingsStore)store).Load();
            Assert.AreEqual(SettingsData.Defaults(), loaded);
        }
    }
}
