using NUnit.Framework;
using Yumiwa.Domain;
using Yumiwa.Application;

namespace Yumiwa.Application.Tests
{
    [TestFixture]
    public class ObjectiveServiceTests
    {
        [Test]
        public void StartChapter_InitializesState()
        {
            var service = new ObjectiveService();
            service.StartChapter(ChapterState.Chapter1_BloodTrail);
            
            Assert.AreEqual(ChapterState.Chapter1_BloodTrail, service.CurrentChapter);
            Assert.AreEqual(ObjectiveStatus.Active, service.ChapterStatus);
            Assert.AreEqual(0, service.CurrentObjectives.Count);
        }

        [Test]
        public void AddObjective_StoresActiveObjective()
        {
            var service = new ObjectiveService();
            var obj = new Objective("obj1", "Desc", ObjectiveStatus.Active, ChapterState.Chapter1_BloodTrail);
            
            service.AddObjective(obj);
            
            Assert.AreEqual(1, service.CurrentObjectives.Count);
            Assert.IsTrue(service.IsObjectiveActive("obj1"));
        }

        [Test]
        public void UpdateObjectiveStatus_ToCompleted_MovesToCompletedList()
        {
            var service = new ObjectiveService();
            var obj = new Objective("obj1", "Desc", ObjectiveStatus.Active, ChapterState.Chapter1_BloodTrail);
            service.AddObjective(obj);
            
            service.UpdateObjectiveStatus("obj1", ObjectiveStatus.Completed);
            
            Assert.IsFalse(service.IsObjectiveActive("obj1"));
            Assert.IsTrue(service.IsObjectiveCompleted("obj1"));
            Assert.AreEqual(0, service.CurrentObjectives.Count);
            Assert.AreEqual(1, service.CompletedObjectives.Count);
        }

        [Test]
        public void CompleteChapter_SetsChapterStatusAndClearsActive()
        {
            var service = new ObjectiveService();
            service.StartChapter(ChapterState.Chapter1_BloodTrail);
            service.AddObjective(new Objective("obj1", "Desc", ObjectiveStatus.Active, ChapterState.Chapter1_BloodTrail));
            
            service.CompleteChapter(ChapterState.Chapter1_BloodTrail);
            
            Assert.AreEqual(ObjectiveStatus.Completed, service.ChapterStatus);
            Assert.AreEqual(0, service.CurrentObjectives.Count);
        }
    }
}
