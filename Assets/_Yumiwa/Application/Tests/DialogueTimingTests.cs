using NUnit.Framework;
using Yumiwa.Domain;

namespace Yumiwa.Application.Tests
{
    /// <summary>EditMode tests for DialogueTiming — pure arithmetic per plan.json reading_speed_formula.</summary>
    [TestFixture]
    public sealed class DialogueTimingTests
    {
        [Test]
        public void EmptyText_ClampsToMin()
        {
            Assert.AreEqual(DialogueTiming.MinDurationSeconds, DialogueTiming.ComputeDuration("", 0f, 1f), 0.001f);
        }

        [Test]
        public void ShortText_ClampsToMin()
        {
            // 1.0 + 0.06*5 = 1.3 -> min 1.5
            Assert.AreEqual(1.5f, DialogueTiming.ComputeDuration("Hello", 0f, 1f), 0.001f);
        }

        [Test]
        public void MediumText_UsesFormula()
        {
            string text = new string('a', 50); // 1.0 + 3.0 = 4.0
            Assert.AreEqual(4.0f, DialogueTiming.ComputeDuration(text, 0f, 1f), 0.001f);
        }

        [Test]
        public void LongText_ClampsToMax()
        {
            string text = new string('a', 500); // 1.0 + 30 = 31 -> max 6.0
            Assert.AreEqual(6.0f, DialogueTiming.ComputeDuration(text, 0f, 1f), 0.001f);
        }

        [Test]
        public void Override_ReplacesFormulaButStillClamped()
        {
            Assert.AreEqual(1.5f, DialogueTiming.ComputeDuration(new string('a', 50), 0.2f, 1f), 0.001f);
            Assert.AreEqual(6.0f, DialogueTiming.ComputeDuration("Hi", 99f, 1f), 0.001f);
            Assert.AreEqual(3.0f, DialogueTiming.ComputeDuration("Hi", 3f, 1f), 0.001f);
        }

        [Test]
        public void SpeedMultiplier_ScalesDuration()
        {
            string text = new string('a', 50); // base 4.0
            Assert.AreEqual(4.0f * 1.3f, DialogueTiming.ComputeDuration(text, 0f, DialogueReadingSpeed.Slow), 0.001f);
            Assert.AreEqual(4.0f, DialogueTiming.ComputeDuration(text, 0f, DialogueReadingSpeed.Normal), 0.001f);
            Assert.AreEqual(4.0f * 0.75f, DialogueTiming.ComputeDuration(text, 0f, DialogueReadingSpeed.Fast), 0.001f);
        }

        [Test]
        public void InvalidMultiplier_FallsBackToNormal()
        {
            string text = new string('a', 50);
            Assert.AreEqual(4.0f, DialogueTiming.ComputeDuration(text, 0f, 0f), 0.001f);
            Assert.AreEqual(4.0f, DialogueTiming.ComputeDuration(text, 0f, -2f), 0.001f);
        }
    }
}
