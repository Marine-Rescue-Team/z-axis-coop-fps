using System;

namespace Game.Core
{
    /// <summary>성문. 유닛이 아니며 피해만 받는다. 파괴된 뒤에는 피해를 받지 않는다.</summary>
    public sealed class Gate : IDamageable
    {
        public Team Team { get; }
        public int MaxDurability { get; }
        public int CurrentDurability { get; private set; }
        public bool IsDestroyed => CurrentDurability <= 0;
        public bool CanBeDamaged => !IsDestroyed;

        /// <summary>
        /// 피해를 받아 내구도가 깎였다. 파괴시킨 피해에도 Destroyed보다 먼저 불리며, 그때 성문은 이미 파괴된 상태다.
        /// </summary>
        public event Action<Gate, DamageInfo> Damaged;
        public event Action<Gate> Destroyed;

        public Gate(Team team, int maxDurability)
        {
            if (maxDurability <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDurability));
            }

            Team = team;
            MaxDurability = maxDurability;
            CurrentDurability = maxDurability;
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!CanBeDamaged || damage.Amount <= 0)
            {
                return;
            }

            // 이 피해가 파괴시켰는지를 이벤트보다 먼저 정한다.
            // Damaged를 듣는 쪽이 다시 피해를 줘서 파괴해도 Destroyed는 그 호출에서 한 번만 불린다.
            CurrentDurability = Math.Max(0, CurrentDurability - damage.Amount);
            var destroyed = IsDestroyed;
            try
            {
                Damaged?.Invoke(this, damage);
            }
            finally
            {
                // Damaged를 듣는 쪽이 예외를 던져도 Destroyed는 올린다. 예외는 그대로 호출자에게 간다.
                if (destroyed)
                {
                    Destroyed?.Invoke(this);
                }
            }
        }
    }
}
