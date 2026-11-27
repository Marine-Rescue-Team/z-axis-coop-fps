using System;
using System.Numerics;

namespace Game.Core
{
    /// <summary>
    /// 게임 규칙상의 유닛 하나. 플레이어, 일반 적, 보스가 모두 이 클래스를 쓴다.
    /// UnitRegistry.Create로만 만든다.
    /// </summary>
    public sealed class Unit : IDamageable
    {
        public int Id { get; }
        public Team Team { get; }
        public UnitDefinition Definition { get; }
        public int MaxHealth { get; }
        public int CurrentHealth { get; private set; }
        public Vector3 Position { get; set; }
        public UnitLifeState State { get; private set; }

        /// <summary>마지막으로 쓰러진 위치. 한 번도 쓰러지지 않았으면 의미 없는 값.</summary>
        public Vector3 DownedPosition { get; private set; }

        /// <summary>게임에서 완전히 빠졌는지. 한 번 빠지면 되돌릴 수 없다.</summary>
        public bool IsRemoved { get; private set; }

        public bool CanBeDamaged => State == UnitLifeState.Alive && !IsRemoved;

        /// <summary>
        /// 피해를 받아 체력이 깎였다. 쓰러뜨린 피해에도 Downed보다 먼저 불리며, 그때 유닛은 이미 쓰러진 상태다.
        /// </summary>
        public event Action<Unit, DamageInfo> Damaged;
        public event Action<Unit> Downed;
        public event Action<Unit> Revived;
        public event Action<Unit> Removed;

        private int downedCount;

        internal Unit(int id, Team team, UnitDefinition definition, Vector3 position)
        {
            Id = id;
            Team = team;
            Definition = definition;
            MaxHealth = definition.MaxHealth;
            CurrentHealth = MaxHealth;
            Position = position;
            State = UnitLifeState.Alive;
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!CanBeDamaged || damage.Amount <= 0)
            {
                return;
            }

            // 상태를 모두 바꾼 뒤에 이벤트를 올린다. 듣는 쪽은 항상 일관된 상태를 본다.
            CurrentHealth = Math.Max(0, CurrentHealth - damage.Amount);
            var downed = CurrentHealth == 0;
            if (downed)
            {
                State = UnitLifeState.Downed;
                DownedPosition = Position;
                downedCount++;
            }

            var downedCountAtDamage = downedCount;
            try
            {
                Damaged?.Invoke(this, damage);
            }
            finally
            {
                // Damaged를 듣는 쪽이 예외를 던져도 쓰러짐 처리는 끝낸다. 예외는 그대로 호출자에게 간다.
                if (downed)
                {
                    FinishDowned(downedCountAtDamage);
                }
            }
        }

        private void FinishDowned(int downedCountAtDamage)
        {
            // Damaged를 듣는 쪽이 유닛을 제거하거나 되살렸다면 이 쓰러짐의 Downed는 올리지 않는다.
            // 되살린 뒤 다시 쓰러뜨렸다면 그 쓰러짐의 Downed는 안쪽 호출이 이미 올렸다.
            if (IsRemoved || State != UnitLifeState.Downed || downedCount != downedCountAtDamage)
            {
                return;
            }

            Downed?.Invoke(this);

            if (Definition.RemoveOnDowned && State == UnitLifeState.Downed)
            {
                Remove();
            }
        }

        /// <summary>
        /// 쓰러진 유닛을 되살린다. 쓰러진 상태가 아니거나 제거된 유닛이면 아무 일도 하지 않고 false.
        /// 언제 어디서 되살릴지는 호출하는 쪽이 정한다. 체력은 1과 최대 체력 사이로 맞춘다.
        /// </summary>
        public bool Revive(Vector3 position, int health)
        {
            if (State != UnitLifeState.Downed || IsRemoved)
            {
                return false;
            }

            State = UnitLifeState.Alive;
            Position = position;
            CurrentHealth = Math.Clamp(health, 1, MaxHealth);
            Revived?.Invoke(this);
            return true;
        }

        /// <summary>게임에서 완전히 뺀다. 살아 있든 쓰러져 있든 가능하다.</summary>
        public void Remove()
        {
            if (IsRemoved)
            {
                return;
            }

            IsRemoved = true;
            Removed?.Invoke(this);
        }
    }
}
