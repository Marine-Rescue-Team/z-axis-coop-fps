namespace Game.Core
{
    /// <summary>한 번의 피해. 가해자는 없을 수 있다(환경 피해 등).</summary>
    public readonly struct DamageInfo
    {
        public readonly int Amount;
        public readonly Unit Attacker;

        public DamageInfo(int amount, Unit attacker = null)
        {
            Amount = amount;
            Attacker = attacker;
        }
    }
}
