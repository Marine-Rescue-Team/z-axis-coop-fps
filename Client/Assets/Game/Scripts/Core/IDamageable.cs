namespace Game.Core
{
    /// <summary>피해를 받을 수 있는 대상. 유닛과 성문이 함께 구현한다.</summary>
    public interface IDamageable
    {
        Team Team { get; }

        /// <summary>지금 피해를 받을 수 있는지. false이면 TakeDamage는 아무 일도 하지 않는다.</summary>
        bool CanBeDamaged { get; }

        void TakeDamage(DamageInfo damage);
    }
}
