using System;

namespace Game.Core
{
    /// <summary>
    /// 유닛 종류 하나의 데이터. 플레이어, 일반 적, 보스의 차이는 이 값으로만 낸다.
    /// 만든 뒤에는 바뀌지 않는다.
    /// </summary>
    public sealed class UnitDefinition
    {
        public int MaxHealth { get; }

        /// <summary>true이면 쓰러지는 즉시 게임에서 제거된다. false이면 쓰러진 채 남는다.</summary>
        public bool RemoveOnDowned { get; }

        /// <summary>true이면 쓰러지고 AutoReviveDelay초 뒤 자동으로 부활한다. false이면 바깥에서 부활시킬 때까지 쓰러져 있다.</summary>
        public bool AutoRevive { get; }

        public float AutoReviveDelay { get; }

        /// <summary>자동 부활이나 UnitRegistry.Revive로 되살릴 때의 위치.</summary>
        public RevivePointRule RevivePoint { get; }

        /// <summary>부활 시 체력. 최대 체력에 대한 비율(0 초과 1 이하).</summary>
        public float ReviveHealthRatio { get; }

        /// <summary>부활 시 체력. 최소 1.</summary>
        public int ReviveHealth => Math.Clamp((int)MathF.Round(MaxHealth * ReviveHealthRatio), 1, MaxHealth);

        public UnitDefinition(int maxHealth, bool removeOnDowned, bool autoRevive, float autoReviveDelay,
            RevivePointRule revivePoint, float reviveHealthRatio)
        {
            if (maxHealth < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            }

            // 조건을 뒤집어 써서 NaN도 걸러낸다.
            if (!(autoReviveDelay >= 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(autoReviveDelay));
            }

            if (!(reviveHealthRatio > 0f && reviveHealthRatio <= 1f))
            {
                throw new ArgumentOutOfRangeException(nameof(reviveHealthRatio));
            }

            MaxHealth = maxHealth;
            RemoveOnDowned = removeOnDowned;
            AutoRevive = autoRevive;
            AutoReviveDelay = autoReviveDelay;
            RevivePoint = revivePoint;
            ReviveHealthRatio = reviveHealthRatio;
        }
    }
}
