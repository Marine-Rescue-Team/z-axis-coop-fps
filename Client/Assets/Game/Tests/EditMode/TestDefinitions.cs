namespace Game.Core.Tests
{
    /// <summary>테스트에서 쓰는 세 가지 유닛 설정.</summary>
    internal static class TestDefinitions
    {
        public const int MaxHealth = 100;
        public const float PlayerReviveDelay = 10f;

        /// <summary>플레이어: 10초 뒤 팀 부활 지점에서 절반 체력으로 자동 부활.</summary>
        public static UnitDefinition Player()
        {
            return new UnitDefinition(MaxHealth, removeOnDowned: false, autoRevive: true,
                autoReviveDelay: PlayerReviveDelay, revivePoint: RevivePointRule.TeamRevivePoint,
                reviveHealthRatio: 0.5f);
        }

        /// <summary>보스: 쓰러진 채 남고, 바깥에서 부활시키면 쓰러진 자리에서 최대 체력으로 부활.</summary>
        public static UnitDefinition Boss()
        {
            return new UnitDefinition(MaxHealth, removeOnDowned: false, autoRevive: false,
                autoReviveDelay: 0f, revivePoint: RevivePointRule.DownedPosition, reviveHealthRatio: 1f);
        }

        /// <summary>일반 몹: 쓰러지면 즉시 제거.</summary>
        public static UnitDefinition Mob()
        {
            return new UnitDefinition(MaxHealth, removeOnDowned: true, autoRevive: false,
                autoReviveDelay: 0f, revivePoint: RevivePointRule.DownedPosition, reviveHealthRatio: 1f);
        }
    }
}
