using System;
using System.Collections.Generic;
using System.Numerics;

namespace Game.Core
{
    /// <summary>
    /// 게임 한 판의 유닛 목록. 유닛을 만들고 찾고, 자동 부활 타이머를 돌린다.
    /// 제거된 유닛은 목록에서 빠진다.
    /// </summary>
    public sealed class UnitRegistry
    {
        private readonly List<Unit> units = new List<Unit>();
        private readonly Dictionary<int, Unit> unitsById = new Dictionary<int, Unit>();
        private readonly Dictionary<Unit, float> autoReviveRemaining = new Dictionary<Unit, float>();
        private readonly Dictionary<Team, Vector3> revivePoints = new Dictionary<Team, Vector3>();
        private readonly List<Unit> dueForRevive = new List<Unit>();
        private int nextId = 1;

        /// <summary>등록된 유닛 누구든 쓰러지면 불린다. 즉시 제거되는 유닛도 UnitRemoved보다 먼저 불린다.</summary>
        public event Action<Unit> UnitDowned;
        public event Action<Unit> UnitRevived;
        public event Action<Unit> UnitRemoved;

        public Unit Create(UnitDefinition definition, Team team, Vector3 position)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            var unit = new Unit(nextId++, team, definition, position);
            units.Add(unit);
            unitsById.Add(unit.Id, unit);
            unit.Downed += OnUnitDowned;
            unit.Revived += OnUnitRevived;
            unit.Removed += OnUnitRemoved;
            return unit;
        }

        public bool TryGet(int id, out Unit unit)
        {
            return unitsById.TryGetValue(id, out unit);
        }

        public int CountAlive(Team team)
        {
            return Count(team, UnitLifeState.Alive);
        }

        public int CountDowned(Team team)
        {
            return Count(team, UnitLifeState.Downed);
        }

        /// <summary>해당 팀, 해당 상태의 유닛을 results에 담는다(먼저 비운다). 만들어진 순서대로 담긴다.</summary>
        public void GetUnits(Team team, UnitLifeState state, List<Unit> results)
        {
            results.Clear();
            foreach (var unit in units)
            {
                if (unit.Team == team && unit.State == state)
                {
                    results.Add(unit);
                }
            }
        }

        public void SetRevivePoint(Team team, Vector3 position)
        {
            revivePoints[team] = position;
        }

        /// <summary>
        /// 쓰러진 유닛을 정의에 적힌 위치 규칙과 체력으로 되살린다. 자동 부활도 이 경로를 쓴다.
        /// 다른 위치나 체력으로 되살리려면 Unit.Revive를 직접 부른다.
        /// </summary>
        public bool Revive(Unit unit)
        {
            var position = unit.DownedPosition;
            if (unit.Definition.RevivePoint == RevivePointRule.TeamRevivePoint
                && revivePoints.TryGetValue(unit.Team, out var teamPoint))
            {
                position = teamPoint;
            }

            return unit.Revive(position, unit.Definition.ReviveHealth);
        }

        /// <summary>시간을 흘려 자동 부활 타이머를 처리한다.</summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || autoReviveRemaining.Count == 0)
            {
                return;
            }

            // 부활 이벤트를 듣는 쪽이 목록을 바꿀 수 있으므로, 대상을 먼저 모은 뒤 되살린다.
            dueForRevive.Clear();
            foreach (var unit in units)
            {
                if (!autoReviveRemaining.TryGetValue(unit, out var remaining))
                {
                    continue;
                }

                remaining -= deltaTime;
                autoReviveRemaining[unit] = remaining;
                if (remaining <= 0f)
                {
                    dueForRevive.Add(unit);
                }
            }

            foreach (var unit in dueForRevive)
            {
                // 앞선 부활의 이벤트를 듣는 쪽이 이 유닛의 타이머를 없애거나 다시 시작했을 수 있다.
                if (autoReviveRemaining.TryGetValue(unit, out var remaining) && remaining <= 0f)
                {
                    Revive(unit);
                }
            }

            dueForRevive.Clear();
        }

        private int Count(Team team, UnitLifeState state)
        {
            var count = 0;
            foreach (var unit in units)
            {
                if (unit.Team == team && unit.State == state)
                {
                    count++;
                }
            }

            return count;
        }

        private void OnUnitDowned(Unit unit)
        {
            if (unit.Definition.AutoRevive)
            {
                autoReviveRemaining[unit] = unit.Definition.AutoReviveDelay;
            }

            UnitDowned?.Invoke(unit);
        }

        private void OnUnitRevived(Unit unit)
        {
            autoReviveRemaining.Remove(unit);
            UnitRevived?.Invoke(unit);
        }

        private void OnUnitRemoved(Unit unit)
        {
            autoReviveRemaining.Remove(unit);
            units.Remove(unit);
            unitsById.Remove(unit.Id);
            unit.Downed -= OnUnitDowned;
            unit.Revived -= OnUnitRevived;
            unit.Removed -= OnUnitRemoved;
            UnitRemoved?.Invoke(unit);
        }
    }
}
