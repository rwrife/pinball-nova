using NUnit.Framework;
using Nova.Core;

namespace Nova.Core.Tests
{
    public class CoreSmokeTests
    {
        [Test]
        public void CoreAssemblyIsAvailableWithoutUnityEngine()
        {
            Assert.AreEqual("Nova.Core", Identity.Name);
        }

        [Test] public void ScoringTable() => CoreRulesChecks.ScoringTable();
        [Test] public void LaneChainRaisesMultiplier() => CoreRulesChecks.LaneChainRaisesMultiplier();
        [Test] public void BallSaveExpires() => CoreRulesChecks.BallSaveExpires();
        [Test] public void MultiballStartAndEnd() => CoreRulesChecks.MultiballStartAndEnd();
        [Test] public void DrainEdgeCases() => CoreRulesChecks.DrainEdgeCases();
        [Test] public void RecordBookRoundTrip() => CoreRulesChecks.RecordBookRoundTrip();
        [Test] public void RecordBookDetectsTamper() => CoreRulesChecks.RecordBookDetectsTamper();
    }
}
