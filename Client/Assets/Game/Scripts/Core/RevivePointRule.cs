namespace Game.Core
{
    /// <summary>정의에 따라 부활시킬 때 어디서 되살릴지.</summary>
    public enum RevivePointRule
    {
        /// <summary>쓰러진 자리.</summary>
        DownedPosition,

        /// <summary>팀의 지정 부활 지점. 지정되지 않았으면 쓰러진 자리.</summary>
        TeamRevivePoint,
    }
}
