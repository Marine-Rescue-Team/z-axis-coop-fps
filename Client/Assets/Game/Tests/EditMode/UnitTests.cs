using System.Collections.Generic;
using System.Numerics;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class UnitTests
    {
        private UnitRegistry registry;

        [SetUp]
        public void SetUp()
        {
            registry = new UnitRegistry();
        }

        private static void Kill(Unit unit)
        {
            unit.TakeDamage(new DamageInfo(unit.CurrentHealth));
        }

        [Test]
        public void NewUnit_IsAliveWithFullHealth()
        {
            var unit = registry.Create(TestDefinitions.Boss(), Team.Enemy, new Vector3(1, 2, 3));

            Assert.AreEqual(UnitLifeState.Alive, unit.State);
            Assert.AreEqual(TestDefinitions.MaxHealth, unit.MaxHealth);
            Assert.AreEqual(TestDefinitions.MaxHealth, unit.CurrentHealth);
            Assert.AreEqual(new Vector3(1, 2, 3), unit.Position);
            Assert.AreEqual(Team.Enemy, unit.Team);
            Assert.IsTrue(unit.CanBeDamaged);
            Assert.IsFalse(unit.IsRemoved);
        }

        [Test]
        public void TakeDamage_ReducesHealthAndRaisesDamaged()
        {
            var attacker = registry.Create(TestDefinitions.Mob(), Team.Enemy, Vector3.Zero);
            var unit = registry.Create(TestDefinitions.Player(), Team.Player, Vector3.Zero);
            var received = new List<DamageInfo>();
            unit.Damaged += (_, damage) => received.Add(damage);

            unit.TakeDamage(new DamageInfo(30, attacker));

            Assert.AreEqual(70, unit.CurrentHealth);
            Assert.AreEqual(UnitLifeState.Alive, unit.State);
            Assert.AreEqual(1, received.Count);
            Assert.AreEqual(30, received[0].Amount);
            Assert.AreSame(attacker, received[0].Attacker);
        }

        [Test]
        public void TakeDamage_ZeroOrNegative_DoesNothing()
        {
            var unit = registry.Create(TestDefinitions.Player(), Team.Player, Vector3.Zero);
            var damagedCount = 0;
            unit.Damaged += (_, _) => damagedCount++;

            unit.TakeDamage(new DamageInfo(0));
            unit.TakeDamage(new DamageInfo(-50));

            Assert.AreEqual(TestDefinitions.MaxHealth, unit.CurrentHealth);
            Assert.AreEqual(0, damagedCount);
        }

        [Test]
        public void HealthReachingZero_DownsUnitAndRecordsPosition()
        {
            var unit = registry.Create(TestDefinitions.Boss(), Team.Enemy, Vector3.Zero);
            unit.Position = new Vector3(5, 0, 7);
            var events = new List<string>();
            unit.Damaged += (_, _) => events.Add("Damaged");
            unit.Downed += _ => events.Add("Downed");

            unit.TakeDamage(new DamageInfo(TestDefinitions.MaxHealth + 999));

            Assert.AreEqual(0, unit.CurrentHealth);
            Assert.AreEqual(UnitLifeState.Downed, unit.State);
            Assert.AreEqual(new Vector3(5, 0, 7), unit.DownedPosition);
            CollectionAssert.AreEqual(new[] { "Damaged", "Downed" }, events);
        }

        [Test]
        public void FatalDamage_DamagedHandlerSeesDownedUnit_AndExtraDamageIsIgnored()
        {
            var unit = registry.Create(TestDefinitions.Boss(), Team.Enemy, Vector3.Zero);
            unit.Position = new Vector3(5, 0, 7);
            var damagedCount = 0;
            var downedCount = 0;
            unit.Downed += _ => downedCount++;
            unit.Damaged += (_, _) =>
            {
                damagedCount++;
                Assert.AreEqual(0, unit.CurrentHealth);
                Assert.AreEqual(UnitLifeState.Downed, unit.State);
                Assert.IsFalse(unit.CanBeDamaged);
                Assert.AreEqual(new Vector3(5, 0, 7), unit.DownedPosition);
                Assert.AreEqual(0, registry.CountAlive(Team.Enemy));
                Assert.AreEqual(1, registry.CountDowned(Team.Enemy));
                Assert.AreEqual(0, downedCount);

                unit.TakeDamage(new DamageInfo(10));
            };

            unit.TakeDamage(new DamageInfo(TestDefinitions.MaxHealth));

            Assert.AreEqual(1, damagedCount);
            Assert.AreEqual(1, downedCount);
            Assert.AreEqual(UnitLifeState.Downed, unit.State);
        }

        [Test]
        public void FatalDamage_RemovedInDamagedHandler_EndsRemovedWithoutDownedEvent()
        {
            var unit = registry.Create(TestDefinitions.Boss(), Team.Enemy, Vector3.Zero);
            var events = new List<string>();
            unit.Damaged += (_, _) =>
            {
                events.Add("Damaged");
                unit.Remove();
            };
            unit.Downed += _ => events.Add("Downed");
            unit.Removed += _ => events.Add("Removed");
            registry.UnitDowned += _ => events.Add("RegistryDowned");
            registry.UnitRemoved += _ => events.Add("RegistryRemoved");

            unit.TakeDamage(new DamageInfo(TestDefinitions.MaxHealth));

            // 제거된 유닛에는 Downed를 올리지 않는다. 상태 값은 쓰러짐으로 남는다.
            CollectionAssert.AreEqual(new[] { "Damaged", "RegistryRemoved", "Removed" }, events);
            Assert.IsTrue(unit.IsRemoved);
            Assert.AreEqual(UnitLifeState.Downed, unit.State);
            Assert.AreEqual(0, unit.CurrentHealth);
            Assert.IsFalse(registry.TryGet(unit.Id, out _));
            Assert.AreEqual(0, registry.CountDowned(Team.Enemy));
        }

        [Test]
        public void DownedUnit_TakesNoDamage()
        {
            var unit = registry.Create(TestDefinitions.Boss(), Team.Enemy, Vector3.Zero);
            Kill(unit);
            var eventCount = 0;
            unit.Damaged += (_, _) => eventCount++;
            unit.Downed += _ => eventCount++;

            unit.TakeDamage(new DamageInfo(10));

            Assert.IsFalse(unit.CanBeDamaged);
            Assert.AreEqual(0, unit.CurrentHealth);
            Assert.AreEqual(UnitLifeState.Downed, unit.State);
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void Revive_WhenDowned_RestoresAtGivenPositionAndHealth()
        {
            var unit = registry.Create(TestDefinitions.Boss(), Team.Enemy, Vector3.Zero);
            Kill(unit);
            var revivedCount = 0;
            unit.Revived += _ => revivedCount++;

            var revived = unit.Revive(new Vector3(9, 0, 9), 40);

            Assert.IsTrue(revived);
            Assert.AreEqual(UnitLifeState.Alive, unit.State);
            Assert.AreEqual(40, unit.CurrentHealth);
            Assert.AreEqual(new Vector3(9, 0, 9), unit.Position);
            Assert.IsTrue(unit.CanBeDamaged);
            Assert.AreEqual(1, revivedCount);
        }

        [Test]
        public void Revive_ClampsHealthBetweenOneAndMax()
        {
            var unit = registry.Create(TestDefinitions.Boss(), Team.Enemy, Vector3.Zero);

            Kill(unit);
            unit.Revive(Vector3.Zero, 0);
            Assert.AreEqual(1, unit.CurrentHealth);

            Kill(unit);
            unit.Revive(Vector3.Zero, TestDefinitions.MaxHealth + 500);
            Assert.AreEqual(TestDefinitions.MaxHealth, unit.CurrentHealth);
        }

        [Test]
        public void Revive_WhenAlive_DoesNothing()
        {
            var unit = registry.Create(TestDefinitions.Boss(), Team.Enemy, new Vector3(1, 0, 1));
            unit.TakeDamage(new DamageInfo(30));
            var revivedCount = 0;
            unit.Revived += _ => revivedCount++;

            var revived = unit.Revive(new Vector3(9, 0, 9), 100);

            Assert.IsFalse(revived);
            Assert.AreEqual(70, unit.CurrentHealth);
            Assert.AreEqual(new Vector3(1, 0, 1), unit.Position);
            Assert.AreEqual(0, revivedCount);
        }

        [Test]
        public void Remove_RaisesRemovedOnce_AndBlocksDamageAndRevive()
        {
            var unit = registry.Create(TestDefinitions.Boss(), Team.Enemy, Vector3.Zero);
            Kill(unit);
            var removedCount = 0;
            unit.Removed += _ => removedCount++;

            unit.Remove();
            unit.Remove();

            Assert.IsTrue(unit.IsRemoved);
            Assert.AreEqual(1, removedCount);
            Assert.IsFalse(unit.CanBeDamaged);
            Assert.IsFalse(unit.Revive(Vector3.Zero, 100));
            Assert.AreEqual(UnitLifeState.Downed, unit.State);
        }

        [Test]
        public void Remove_AliveUnit_BlocksDamage()
        {
            var unit = registry.Create(TestDefinitions.Player(), Team.Player, Vector3.Zero);

            unit.Remove();
            unit.TakeDamage(new DamageInfo(10));

            Assert.IsTrue(unit.IsRemoved);
            Assert.IsFalse(unit.CanBeDamaged);
            Assert.AreEqual(TestDefinitions.MaxHealth, unit.CurrentHealth);
        }

        [Test]
        public void MobDefinition_IsRemovedImmediatelyWhenDowned()
        {
            var unit = registry.Create(TestDefinitions.Mob(), Team.Enemy, Vector3.Zero);
            var events = new List<string>();
            unit.Downed += _ => events.Add("Downed");
            unit.Removed += _ => events.Add("Removed");

            Kill(unit);

            Assert.IsTrue(unit.IsRemoved);
            CollectionAssert.AreEqual(new[] { "Downed", "Removed" }, events);
        }

        [Test]
        public void BossDefinition_StaysDownedUntilRevivedFromOutside()
        {
            var unit = registry.Create(TestDefinitions.Boss(), Team.Enemy, Vector3.Zero);
            Kill(unit);

            registry.Tick(1000f);

            Assert.IsFalse(unit.IsRemoved);
            Assert.AreEqual(UnitLifeState.Downed, unit.State);
        }
    }
}
