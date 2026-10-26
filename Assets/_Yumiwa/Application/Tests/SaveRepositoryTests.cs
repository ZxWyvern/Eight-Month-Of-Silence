using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Yumiwa.Domain;
using Yumiwa.Infrastructure;

namespace Yumiwa.Application.Tests
{
    /// <summary>
    /// Real temp-directory repository tests per verification-matrix Save row:
    /// round-trip, rapid consecutive saves, clear semantics. All writes are
    /// observed via JsonFileSaveRepository.DrainAsync (fire-and-forget lifter
    /// kept per ADR-0016, but tests never assert on unobserved I/O).
    /// </summary>
    [TestFixture]
    public sealed class SaveRepositoryTests
    {
        private string dir;

        [SetUp]
        public void SetUp()
        {
            dir = Path.Combine(Path.GetTempPath(), "yumiwa_save_test_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch { }
        }

        private static CheckpointData Sample(float x, int mission)
        {
            return new CheckpointData
            {
                PosX = x,
                PosY = 1f,
                PosZ = 2f,
                Health = 100f,
                Stamina = 80f,
                ActiveScene = "PunyaPandu",
                MissionIndex = mission,
                PendingRecoveredIds = new[] { "brass_key" },
                DiscoveredHotspotKeys = new[] { "watch|blood" }
            };
        }

        [Test]
        public async Task SaveLoadChapter_RoundTrip()
        {
            var repo = new JsonFileSaveRepository(dir);
            repo.SaveChapter(Sample(3.5f, 2));
            await JsonFileSaveRepository.DrainAsync();

            Assert.IsTrue(repo.HasChapterSave);
            var loaded = repo.LoadChapter();
            Assert.AreEqual(3.5f, loaded.PosX);
            Assert.AreEqual(2, loaded.MissionIndex);
            Assert.AreEqual("PunyaPandu", loaded.ActiveScene);
            Assert.AreEqual("4", loaded.SaveVersion);
            Assert.AreEqual(new[] { "brass_key" }, loaded.PendingRecoveredIds);
            Assert.AreEqual(new[] { "watch|blood" }, loaded.DiscoveredHotspotKeys);
        }

        [Test]
        public async Task RapidSaves_LastWriteWins()
        {
            var repo = new JsonFileSaveRepository(dir);
            for (int i = 0; i < 20; i++)
                repo.SaveChapter(Sample(i, i));
            await JsonFileSaveRepository.DrainAsync();

            var loaded = repo.LoadChapter();
            Assert.AreEqual(19f, loaded.PosX);
            Assert.AreEqual(19, loaded.MissionIndex);
            // No orphan tmp files left behind
            Assert.AreEqual(0, Directory.GetFiles(dir, "*.tmp").Length);
        }

        [Test]
        public async Task HouseCheckpoint_MilestoneRoundTrip()
        {
            var repo = new JsonFileSaveRepository(dir);
            repo.SaveHouseCheckpoint(Sample(7f, 4), CheckpointMilestone.Shovel);
            await JsonFileSaveRepository.DrainAsync();

            Assert.IsTrue(repo.HasHouseCheckpoint);
            var loaded = repo.LoadHouseCheckpoint(out var milestone);
            Assert.AreEqual(CheckpointMilestone.Shovel, milestone);
            Assert.AreEqual(7f, loaded.PosX);
            Assert.AreEqual("4", loaded.SaveVersion);
        }

        [Test]
        public async Task ClearHouseCheckpoint_RemovesFile()
        {
            var repo = new JsonFileSaveRepository(dir);
            repo.SaveHouseCheckpoint(Sample(1f, 1), CheckpointMilestone.DisplayCaseKey);
            await JsonFileSaveRepository.DrainAsync();

            repo.ClearHouseCheckpoint();
            Assert.IsFalse(repo.HasHouseCheckpoint);
            Assert.IsTrue(repo.HasChapterSave == false);
        }

        [Test]
        public async Task ChapterAndHouseStores_AreIndependent()
        {
            var repo = new JsonFileSaveRepository(dir);
            repo.SaveChapter(Sample(1f, 1));
            repo.SaveHouseCheckpoint(Sample(9f, 5), CheckpointMilestone.BasementAccess);
            await JsonFileSaveRepository.DrainAsync();

            repo.ClearHouseCheckpoint();
            Assert.IsFalse(repo.HasHouseCheckpoint);
            Assert.IsTrue(repo.HasChapterSave);
            Assert.AreEqual(1f, repo.LoadChapter().PosX);
        }
    }
}
