using NUnit.Framework;
using Yumiwa.Domain;
using Yumiwa.Application;

namespace Yumiwa.Application.Tests
{
    [TestFixture]
    public class EvidenceLogServiceTests
    {
        private Evidence CreateEvidence(string id, EvidenceType type, EvidenceCategory cat)
        {
            return new Evidence(id, id, "Desc", "Hot", "Fact", "Dep", "Conseq", type, cat);
        }

        [Test]
        public void AddEvidence_StoresEvidence()
        {
            var log = new EvidenceLogService();
            var ev = CreateEvidence("ev1", EvidenceType.Critical, EvidenceCategory.Fact);
            log.AddEvidence(ev);

            Assert.IsTrue(log.HasEvidence("ev1"));
            Assert.AreEqual(1, log.GetAllEvidence().Count);
        }

        [Test]
        public void RemoveEvidence_PreventsRemoving_CriticalEvidence()
        {
            var log = new EvidenceLogService();
            var ev = CreateEvidence("ev1", EvidenceType.Critical, EvidenceCategory.Fact);
            log.AddEvidence(ev);

            log.RemoveEvidence("ev1"); // Should be ignored
            Assert.IsTrue(log.HasEvidence("ev1"));
        }

        [Test]
        public void RemoveEvidence_AllowsRemoving_OptionalEvidence()
        {
            var log = new EvidenceLogService();
            var ev = CreateEvidence("ev1", EvidenceType.Optional, EvidenceCategory.Fact);
            log.AddEvidence(ev);

            log.RemoveEvidence("ev1"); // Should succeed
            Assert.IsFalse(log.HasEvidence("ev1"));
        }

        [Test]
        public void GetEvidenceByCategory_FiltersCorrectly()
        {
            var log = new EvidenceLogService();
            log.AddEvidence(CreateEvidence("ev1", EvidenceType.Critical, EvidenceCategory.Fact));
            log.AddEvidence(CreateEvidence("ev2", EvidenceType.Critical, EvidenceCategory.Lead));

            var facts = log.GetEvidenceByCategory(EvidenceCategory.Fact);
            Assert.AreEqual(1, facts.Count);
            Assert.AreEqual("ev1", facts[0].EvidenceId);
        }
    }
}
