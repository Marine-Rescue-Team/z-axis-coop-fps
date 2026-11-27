using System;
using System.Collections.Generic;
using System.Numerics;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class GateTests
    {
        [Test]
        public void NewGate_HasFullDurability()
        {
            var gate = new Gate(Team.Player, 500);

            Assert.AreEqual(Team.Player, gate.Team);
            Assert.AreEqual(500, gate.MaxDurability);
            Assert.AreEqual(500, gate.CurrentDurability);
            Assert.IsFalse(gate.IsDestroyed);
            Assert.IsTrue(gate.CanBeDamaged);
        }

        [Test]
        public void Constructor_NonPositiveDurability_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Gate(Team.Player, 0));
        }

        [Test]
        public void TakeDamage_ReducesDurabilityAndRaisesDamaged()
        {
            var gate = new Gate(Team.Player, 500);
            var received = new List<DamageInfo>();
            gate.Damaged += (_, damage) => received.Add(damage);

            gate.TakeDamage(new DamageInfo(120));
            gate.TakeDamage(new DamageInfo(0));

            Assert.AreEqual(380, gate.CurrentDurability);
            Assert.AreEqual(1, received.Count);
            Assert.AreEqual(120, received[0].Amount);
            Assert.IsNull(received[0].Attacker);
        }

        [Test]
        public void DurabilityReachingZero_RaisesDestroyedOnce()
        {
            var gate = new Gate(Team.Player, 500);
            var events = new List<string>();
            gate.Damaged += (_, _) => events.Add("Damaged");
            gate.Destroyed += _ => events.Add("Destroyed");

            gate.TakeDamage(new DamageInfo(9999));

            Assert.AreEqual(0, gate.CurrentDurability);
            Assert.IsTrue(gate.IsDestroyed);
            CollectionAssert.AreEqual(new[] { "Damaged", "Destroyed" }, events);
        }

        [Test]
        public void TakeDamage_ZeroOrNegative_DoesNothing()
        {
            var gate = new Gate(Team.Player, 500);
            var eventCount = 0;
            gate.Damaged += (_, _) => eventCount++;
            gate.Destroyed += _ => eventCount++;

            gate.TakeDamage(new DamageInfo(0));
            gate.TakeDamage(new DamageInfo(-50));

            Assert.AreEqual(500, gate.CurrentDurability);
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void NestedDamageInDamagedHandler_RaisesDestroyedOnce()
        {
            var gate = new Gate(Team.Player, 100);
            var nested = false;
            var destroyedCount = 0;
            gate.Damaged += (_, _) =>
            {
                if (!nested)
                {
                    nested = true;
                    gate.TakeDamage(new DamageInfo(100));
                }
            };
            gate.Destroyed += _ => destroyedCount++;

            gate.TakeDamage(new DamageInfo(10));

            Assert.IsTrue(gate.IsDestroyed);
            Assert.AreEqual(1, destroyedCount);
        }

        [Test]
        public void FatalDamage_DamagedHandlerSeesDestroyedGate()
        {
            var gate = new Gate(Team.Player, 100);
            var damagedCount = 0;
            var destroyedCount = 0;
            gate.Damaged += (_, _) =>
            {
                damagedCount++;
                Assert.IsTrue(gate.IsDestroyed);
                Assert.IsFalse(gate.CanBeDamaged);
                gate.TakeDamage(new DamageInfo(10));
            };
            gate.Destroyed += _ => destroyedCount++;

            gate.TakeDamage(new DamageInfo(100));

            Assert.AreEqual(1, damagedCount);
            Assert.AreEqual(1, destroyedCount);
        }

        [Test]
        public void FatalDamage_DamagedHandlerThrows_StillRaisesDestroyed()
        {
            var gate = new Gate(Team.Player, 100);
            var destroyedCount = 0;
            gate.Damaged += (_, _) => throw new InvalidOperationException("handler bug");
            gate.Destroyed += _ => destroyedCount++;

            Assert.Throws<InvalidOperationException>(() => gate.TakeDamage(new DamageInfo(100)));

            Assert.IsTrue(gate.IsDestroyed);
            Assert.AreEqual(1, destroyedCount);
        }

        [Test]
        public void DestroyedGate_TakesNoDamage()
        {
            var gate = new Gate(Team.Player, 500);
            gate.TakeDamage(new DamageInfo(500));
            var eventCount = 0;
            gate.Damaged += (_, _) => eventCount++;
            gate.Destroyed += _ => eventCount++;

            gate.TakeDamage(new DamageInfo(10));

            Assert.IsFalse(gate.CanBeDamaged);
            Assert.AreEqual(0, gate.CurrentDurability);
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void UnitAndGate_AreDamagedThroughSameContract()
        {
            var registry = new UnitRegistry();
            var unit = registry.Create(TestDefinitions.Player(), Team.Player, Vector3.Zero);
            var gate = new Gate(Team.Player, 500);
            var targets = new IDamageable[] { unit, gate };

            // 범위 피해가 대상의 종류를 모르고 때리는 상황.
            foreach (var target in targets)
            {
                if (target.Team == Team.Player && target.CanBeDamaged)
                {
                    target.TakeDamage(new DamageInfo(25));
                }
            }

            Assert.AreEqual(TestDefinitions.MaxHealth - 25, unit.CurrentHealth);
            Assert.AreEqual(475, gate.CurrentDurability);
        }
    }
}
