using DontLetThemIn.Economy;
using NUnit.Framework;

namespace DontLetThemIn.Tests.EditMode
{
    public sealed class EconomyTests
    {
        [Test]
        public void ScrapManager_AppliesRewardsAndCostsCorrectly()
        {
            ScrapManager manager = new(60);

            Assert.That(manager.CurrentScrap, Is.EqualTo(60));

            manager.Add(15);
            Assert.That(manager.CurrentScrap, Is.EqualTo(75));

            bool spent = manager.TrySpend(20);
            Assert.That(spent, Is.True);
            Assert.That(manager.CurrentScrap, Is.EqualTo(55));
        }

        [Test]
        public void ScrapManager_BlocksPurchases_WhenInsufficientScrap()
        {
            ScrapManager manager = new(60);

            bool spent = manager.TrySpend(100);

            Assert.That(spent, Is.False);
            Assert.That(manager.CurrentScrap, Is.EqualTo(60));
        }
    }
}
