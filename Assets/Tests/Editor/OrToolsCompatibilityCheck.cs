using System;
using NUnit.Framework;

namespace VirtualDarkStore.Tests.Editor
{
    [TestFixture]
    public class OrToolsCompatibilityCheck
    {
        [Test]
        public void MinimalOrToolsCheck()
        {
            try
            {
                Type routingType = Type.GetType("Google.OrTools.ConstraintSolver.RoutingModel, Google.OrTools");
                if (routingType == null)
                {
                    Assert.Inconclusive("Google.OrTools assembly not loaded.");
                    return;
                }

                // Attempt to instantiate RoutingIndexManager
                var managerType = Type.GetType("Google.OrTools.ConstraintSolver.RoutingIndexManager, Google.OrTools");
                var ctor = managerType.GetConstructor(new[] { typeof(int), typeof(int), typeof(int) });
                var manager = ctor.Invoke(new object[] { 4, 1, 0 });
                Assert.IsNotNull(manager);
            }
            catch (Exception ex)
            {
                Assert.Fail("Failed to load or instantiate OR-Tools: " + ex);
            }
        }
    }
}
