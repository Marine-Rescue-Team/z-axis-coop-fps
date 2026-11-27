using System;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class UnitDefinitionTests
    {
        private static UnitDefinition Create(int maxHealth = 100, float autoReviveDelay = 10f,
            float reviveHealthRatio = 1f)
        {
            return new UnitDefinition(maxHealth, removeOnDowned: false, autoRevive: true,
                autoReviveDelay: autoReviveDelay, revivePoint: RevivePointRule.DownedPosition,
                reviveHealthRatio: reviveHealthRatio);
        }

        [Test]
        public void Constructor_KeepsGivenValues()
        {
            var definition = new UnitDefinition(250, removeOnDowned: true, autoRevive: true, autoReviveDelay: 7.5f,
                revivePoint: RevivePointRule.TeamRevivePoint, reviveHealthRatio: 0.4f);

            Assert.AreEqual(250, definition.MaxHealth);
            Assert.IsTrue(definition.RemoveOnDowned);
            Assert.IsTrue(definition.AutoRevive);
            Assert.AreEqual(7.5f, definition.AutoReviveDelay);
            Assert.AreEqual(RevivePointRule.TeamRevivePoint, definition.RevivePoint);
            Assert.AreEqual(0.4f, definition.ReviveHealthRatio);
            Assert.AreEqual(100, definition.ReviveHealth);
        }

        [Test]
        public void ReviveHealth_IsAtLeastOne()
        {
            Assert.AreEqual(1, Create(maxHealth: 10, reviveHealthRatio: 0.01f).ReviveHealth);
            Assert.AreEqual(10, Create(maxHealth: 10, reviveHealthRatio: 1f).ReviveHealth);
        }

        [Test]
        public void Constructor_RejectsOutOfRangeValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(maxHealth: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(autoReviveDelay: -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(autoReviveDelay: float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(reviveHealthRatio: 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(reviveHealthRatio: 1.5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(reviveHealthRatio: float.NaN));
        }

        [Test]
        public void Constructor_AcceptsZeroDelay()
        {
            Assert.DoesNotThrow(() => Create(autoReviveDelay: 0f));
        }
    }
}
