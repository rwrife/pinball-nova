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
    }
}
