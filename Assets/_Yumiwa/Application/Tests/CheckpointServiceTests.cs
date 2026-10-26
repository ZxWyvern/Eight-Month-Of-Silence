using NUnit.Framework;
using Yumiwa.Domain;
using Yumiwa.Application;

namespace Yumiwa.Application.Tests
{
    [TestFixture]
    public class CheckpointServiceTests
    {
        [Test]
        public void Service_Starts_Empty()
        {
            var service = new CheckpointService(null);
            Assert.IsFalse(service.HasCheckpoint);
        }

        [Test]
        public void SaveCheckpoint_StoresData()
        {
            var service = new CheckpointService(null);
            var data = new CheckpointData 
            { 
                Health = 100f, 
                ActiveScene = "TestScene" 
            };
            
            service.SaveCheckpoint(data);
            Assert.IsTrue(service.HasCheckpoint);
            
            var loaded = service.LoadCheckpoint();
            Assert.AreEqual(100f, loaded.Health);
            Assert.AreEqual("TestScene", loaded.ActiveScene);
        }

        [Test]
        public void ClearCheckpoint_RemovesData()
        {
            var service = new CheckpointService(null);
            service.SaveCheckpoint(new CheckpointData());
            Assert.IsTrue(service.HasCheckpoint);

            service.ClearCheckpoint();
            Assert.IsFalse(service.HasCheckpoint);
        }
    }
}
