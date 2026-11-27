using System;
using System.Collections.Generic;
using System.Numerics;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class UnitRegistryTests
    {
        private static readonly Vector3 GateFront = new Vector3(0, 0, -20);

        private UnitRegistry registry;

        [SetUp]
        public void SetUp()
        {
            registry = new UnitRegistry();
            registry.SetRevivePoint(Team.Player, GateFront);
        }

        private static void Kill(Unit unit)
        {
            unit.TakeDamage(new DamageInfo(unit.CurrentHealth));
        }

        [Test]
        public void FatalDamage_RevivedAndDownedAgainInDamagedHandler_RaisesDownedOncePerDown()
        {
            var boss = registry.Create(TestDefinitions.Boss(), Team.Enemy, Vector3.Zero);
            var events = new List<string>();
            var nested = false;
            boss.Damaged += (_, _) =>
            {
                events.Add("Damaged");
                if (!nested)
                {
                    nested = true;
                    boss.Revive(Vector3.Zero, 50);
                    boss.TakeDamage(new DamageInfo(50));
                }
            };
            boss.Revived += _ => events.Add("Revived");
            boss.Downed += _ => events.Add("Downed");
            registry.UnitDowned += _ => events.Add("RegistryDowned");

            Kill(boss);

            // 첫 쓰러짐은 핸들러가 되살려서 Downed가 없고, 두 번째 쓰러짐의 Downed만 한 번 나온다.
            CollectionAssert.AreEqual(
                new[] { "Damaged", "Revived", "Damaged", "RegistryDowned", "Downed" }, events);
            Assert.AreEqual(UnitLifeState.Downed, boss.State);
        }

        [Test]
        public void FatalDamage_DamagedHandlerThrows_MobIsStillRemoved()
        {
            var mob = registry.Create(TestDefinitions.Mob(), Team.Enemy, Vector3.Zero);
            var downedCount = 0;
            var removedCount = 0;
            mob.Damaged += (_, _) => throw new InvalidOperationException("handler bug");
            registry.UnitDowned += _ => downedCount++;
            registry.UnitRemoved += _ => removedCount++;

            Assert.Throws<InvalidOperationException>(() => Kill(mob));

            Assert.IsTrue(mob.IsRemoved);
            Assert.AreEqual(1, downedCount);
            Assert.AreEqual(1, removedCount);
            Assert.IsFalse(registry.TryGet(mob.Id, out _));
        }

        [Test]
        public void FatalDamage_DamagedHandlerThrows_PlayerStillAutoRevives()
        {
            var player = registry.Create(TestDefinitions.Player(), Team.Player, Vector3.Zero);
            var downedCount = 0;
            player.Damaged += (_, _) => throw new InvalidOperationException("handler bug");
            registry.UnitDowned += _ => downedCount++;

            Assert.Throws<InvalidOperationException>(() => Kill(player));
            Assert.AreEqual(UnitLifeState.Downed, player.State);
            Assert.AreEqual(1, downedCount);

            registry.Tick(TestDefinitions.PlayerReviveDelay);

            Assert.AreEqual(UnitLifeState.Alive, player.State);
            Assert.AreEqual(GateFront, player.Position);
        }

        [Test]
        public void Create_AssignsUniqueIds_AndTryGetFindsUnit()
        {
            var a = registry.Create(TestDefinitions.Player(), Team.Player, Vector3.Zero);
            var b = registry.Create(TestDefinitions.Mob(), Team.Enemy, Vector3.Zero);

            Assert.AreNotEqual(a.Id, b.Id);
            Assert.IsTrue(registry.TryGet(a.Id, out var foundA));
            Assert.AreSame(a, foundA);
            Assert.IsTrue(registry.TryGet(b.Id, out var foundB));
            Assert.AreSame(b, foundB);
            Assert.IsFalse(registry.TryGet(9999, out _));
        }

        [Test]
        public void Create_NullDefinition_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => registry.Create(null, Team.Player, Vector3.Zero));
        }

        [Test]
        public void Counts_TrackAliveAndDownedPerTeam()
        {
            var p1 = registry.Create(TestDefinitions.Player(), Team.Player, Vector3.Zero);
            registry.Create(TestDefinitions.Player(), Team.Player, Vector3.Zero);
            registry.Create(TestDefinitions.Player(), Team.Player, Vector3.Zero);
            registry.Create(TestDefinitions.Boss(), Team.Enemy, Vector3.Zero);

            Kill(p1);

            Assert.AreEqual(2, registry.CountAlive(Team.Player));
            Assert.AreEqual(1, registry.CountDowned(Team.Player));
            Assert.AreEqual(1, registry.CountAlive(Team.Enemy));
            Assert.AreEqual(0, registry.CountDowned(Team.Enemy));
        }

        [Test]
        public void GetUnits_FiltersByTeamAndState_AndClearsResults()
        {
            var p1 = registry.Create(TestDefinitions.Player(), Team.Player, Vector3.Zero);
            var p2 = registry.Create(TestDefinitions.Player(), Team.Player, Vector3.Zero);
            var boss = registry.Create(TestDefinitions.Boss(), Team.Enemy, Vector3.Zero);
            Kill(p2);
            var results = new List<Unit> { boss };

            registry.GetUnits(Team.Player, UnitLifeState.Alive, results);
            CollectionAssert.AreEqual(new[] { p1 }, results);

            registry.GetUnits(Team.Player, UnitLifeState.Downed, results);
            CollectionAssert.AreEqual(new[] { p2 }, results);

            registry.GetUnits(Team.Enemy, UnitLifeState.Alive, results);
            CollectionAssert.AreEqual(new[] { boss }, results);
        }

        [Test]
        public void RemovedUnit_LeavesRegistry()
        {
            var mob = registry.Create(TestDefinitions.Mob(), Team.Enemy, Vector3.Zero);
            var removed = new List<Unit>();
            registry.UnitRemoved += removed.Add;

            Kill(mob);

            Assert.IsFalse(registry.TryGet(mob.Id, out _));
            Assert.AreEqual(0, registry.CountAlive(Team.Enemy));
            Assert.AreEqual(0, registry.CountDowned(Team.Enemy));
            CollectionAssert.AreEqual(new[] { mob }, removed);
        }

        [Test]
        public void Ids_AreNotReusedAfterRemoval()
        {
            var first = registry.Create(TestDefinitions.Mob(), Team.Enemy, Vector3.Zero);
            Kill(first);

            var second = registry.Create(TestDefinitions.Mob(), Team.Enemy, Vector3.Zero);

            Assert.AreNotEqual(first.Id, second.Id);
        }

        [Test]
        public void PlayerDefinition_AutoRevivesAtTeamPointAfterDelay()
        {
            var player = registry.Create(TestDefinitions.Player(), Team.Player, Vector3.Zero);
            player.Position = new Vector3(30, 0, 30);
            Kill(player);

            registry.Tick(TestDefinitions.PlayerReviveDelay - 0.5f);
            Assert.AreEqual(UnitLifeState.Downed, player.State);

            registry.Tick(0.5f);
            Assert.AreEqual(UnitLifeState.Alive, player.State);
            Assert.AreEqual(GateFront, player.Position);
            Assert.AreEqual(TestDefinitions.MaxHealth / 2, player.CurrentHealth);
        }

        [Test]
        public void AutoRevive_TimerRestartsOnEachDown()
        {
            var player = registry.Create(TestDefinitions.Player(), Team.Player, Vector3.Zero);
            Kill(player);
            registry.Tick(TestDefinitions.PlayerReviveDelay);
            Assert.AreEqual(UnitLifeState.Alive, player.State);

            Kill(player);
            registry.Tick(TestDefinitions.PlayerReviveDelay - 1f);
            Assert.AreEqual(UnitLifeState.Downed, player.State);

            registry.Tick(1f);
            Assert.AreEqual(UnitLifeState.Alive, player.State);
        }

        [Test]
        public void AutoRevive_WithoutTeamPoint_FallsBackToDownedPosition()
        {
            var noPoints = new UnitRegistry();
            var player = noPoints.Create(TestDefinitions.Player(), Team.Player, new Vector3(3, 0, 4));
            Kill(player);

            noPoints.Tick(TestDefinitions.PlayerReviveDelay);

            Assert.AreEqual(UnitLifeState.Alive, player.State);
            Assert.AreEqual(new Vector3(3, 0, 4), player.Position);
        }

        [Test]
        public void AutoRevive_DoesNotFireForUnitRemovedWhileDowned()
        {
            var player = registry.Create(TestDefinitions.Player(), Team.Player, Vector3.Zero);
            Kill(player);
            player.Remove();
            var revivedCount = 0;
            registry.UnitRevived += _ => revivedCount++;

            registry.Tick(TestDefinitions.PlayerReviveDelay * 2);

            Assert.AreEqual(UnitLifeState.Downed, player.State);
            Assert.AreEqual(0, revivedCount);
        }

        [Test]
        public void ManualRevive_IsNotFollowedByAutoRevive()
        {
            var player = registry.Create(TestDefinitions.Player(), Team.Player, Vector3.Zero);
            var revivedCount = 0;
            registry.UnitRevived += _ => revivedCount++;
            Kill(player);
            registry.Tick(TestDefinitions.PlayerReviveDelay - 1f);
            player.Revive(new Vector3(1, 0, 1), 30);

            // 원래 자동 부활 시각을 지나도 손으로 되살린 결과가 그대로다.
            registry.Tick(2f);

            Assert.AreEqual(1, revivedCount);
            Assert.AreEqual(UnitLifeState.Alive, player.State);
            Assert.AreEqual(new Vector3(1, 0, 1), player.Position);
            Assert.AreEqual(30, player.CurrentHealth);
        }

        [Test]
        public void DownedAgainAfterManualRevive_WaitsFullDelay()
        {
            var player = registry.Create(TestDefinitions.Player(), Team.Player, Vector3.Zero);
            Kill(player);
            registry.Tick(TestDefinitions.PlayerReviveDelay - 1f);
            player.Revive(new Vector3(1, 0, 1), 100);
            Kill(player);

            // 처음 쓰러졌을 때의 남은 1초가 아니라 새로 10초를 기다린다.
            registry.Tick(2f);
            Assert.AreEqual(UnitLifeState.Downed, player.State);

            registry.Tick(TestDefinitions.PlayerReviveDelay - 2f);
            Assert.AreEqual(UnitLifeState.Alive, player.State);
        }

        [Test]
        public void Tick_DoesNotReviveUnitWhoseTimerRestartedInSameTick()
        {
            var a = registry.Create(TestDefinitions.Player(), Team.Player, Vector3.Zero);
            var b = registry.Create(TestDefinitions.Player(), Team.Player, Vector3.Zero);
            Kill(a);
            Kill(b);
            registry.UnitRevived += unit =>
            {
                if (unit == a)
                {
                    b.Revive(Vector3.Zero, 100);
                    Kill(b);
                }
            };

            registry.Tick(TestDefinitions.PlayerReviveDelay);

            Assert.AreEqual(UnitLifeState.Alive, a.State);
            Assert.AreEqual(UnitLifeState.Downed, b.State);

            registry.Tick(TestDefinitions.PlayerReviveDelay);
            Assert.AreEqual(UnitLifeState.Alive, b.State);
        }

        [Test]
        public void BossDefinition_RevivesAtDownedPositionOnlyWhenCalled()
        {
            var boss = registry.Create(TestDefinitions.Boss(), Team.Enemy, Vector3.Zero);
            registry.SetRevivePoint(Team.Enemy, new Vector3(99, 0, 99));
            boss.Position = new Vector3(12, 0, 34);
            Kill(boss);
            boss.Position = new Vector3(50, 0, 50);

            registry.Tick(1000f);
            Assert.AreEqual(UnitLifeState.Downed, boss.State);

            Assert.IsTrue(registry.Revive(boss));
            Assert.AreEqual(UnitLifeState.Alive, boss.State);
            Assert.AreEqual(new Vector3(12, 0, 34), boss.Position);
            Assert.AreEqual(TestDefinitions.MaxHealth, boss.CurrentHealth);
        }

        [Test]
        public void Revive_AliveUnit_ReturnsFalse()
        {
            var boss = registry.Create(TestDefinitions.Boss(), Team.Enemy, Vector3.Zero);

            Assert.IsFalse(registry.Revive(boss));
        }

        [Test]
        public void MobDefinition_CanBeGivenAutoReviveByValuesOnly()
        {
            var revivingMob = new UnitDefinition(TestDefinitions.MaxHealth, removeOnDowned: false, autoRevive: true, autoReviveDelay: 3f,
                revivePoint: RevivePointRule.DownedPosition, reviveHealthRatio: 1f);
            var mob = registry.Create(revivingMob, Team.Enemy, new Vector3(2, 0, 2));
            Kill(mob);

            registry.Tick(3f);

            Assert.IsFalse(mob.IsRemoved);
            Assert.AreEqual(UnitLifeState.Alive, mob.State);
            Assert.AreEqual(new Vector3(2, 0, 2), mob.Position);
        }

        [Test]
        public void RegistryEvents_ForwardDownedAndRevived()
        {
            var player = registry.Create(TestDefinitions.Player(), Team.Player, Vector3.Zero);
            var events = new List<string>();
            registry.UnitDowned += unit => events.Add("Downed" + unit.Id);
            registry.UnitRevived += unit => events.Add("Revived" + unit.Id);

            Kill(player);
            registry.Tick(TestDefinitions.PlayerReviveDelay);

            CollectionAssert.AreEqual(new[] { "Downed" + player.Id, "Revived" + player.Id }, events);
        }

        [Test]
        public void AllPlayersDown_IsDetectableFromDownedEvent()
        {
            var players = new[]
            {
                registry.Create(TestDefinitions.Player(), Team.Player, Vector3.Zero),
                registry.Create(TestDefinitions.Player(), Team.Player, Vector3.Zero),
                registry.Create(TestDefinitions.Player(), Team.Player, Vector3.Zero),
            };
            var wipedOut = false;
            registry.UnitDowned += unit =>
            {
                if (unit.Team == Team.Player && registry.CountAlive(Team.Player) == 0)
                {
                    wipedOut = true;
                }
            };

            Kill(players[0]);
            Kill(players[1]);
            Assert.IsFalse(wipedOut);

            // 먼저 쓰러진 둘이 부활한 뒤 나머지 하나가 쓰러지면 전멸이 아니다.
            registry.Tick(TestDefinitions.PlayerReviveDelay);
            Kill(players[2]);
            Assert.IsFalse(wipedOut);

            Kill(players[0]);
            Kill(players[1]);
            Assert.IsTrue(wipedOut);
        }

        [Test]
        public void Tick_SurvivesUnitsCreatedAndRemovedInReviveHandler()
        {
            var p1 = registry.Create(TestDefinitions.Player(), Team.Player, Vector3.Zero);
            var p2 = registry.Create(TestDefinitions.Player(), Team.Player, Vector3.Zero);
            Kill(p1);
            Kill(p2);
            registry.UnitRevived += unit =>
            {
                if (unit == p1)
                {
                    p2.Remove();
                    registry.Create(TestDefinitions.Mob(), Team.Enemy, Vector3.Zero);
                }
            };

            Assert.DoesNotThrow(() => registry.Tick(TestDefinitions.PlayerReviveDelay));

            Assert.AreEqual(UnitLifeState.Alive, p1.State);
            Assert.AreEqual(UnitLifeState.Downed, p2.State);
            Assert.IsTrue(p2.IsRemoved);
            Assert.AreEqual(1, registry.CountAlive(Team.Enemy));
        }
    }
}
