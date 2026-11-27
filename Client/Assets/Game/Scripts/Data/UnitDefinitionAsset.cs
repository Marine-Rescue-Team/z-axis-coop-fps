using Game.Core;
using UnityEngine;

namespace Game.Data
{
    /// <summary>
    /// 인스펙터에서 편집하는 유닛 데이터. 규칙 계층은 이 에셋이 아니라 ToDefinition()이 만든 UnitDefinition을 쓴다.
    /// </summary>
    [CreateAssetMenu(fileName = "UnitDefinition", menuName = "Game/Unit Definition")]
    public class UnitDefinitionAsset : ScriptableObject
    {
        [SerializeField, Min(1)] private int maxHealth = 100;

        [Header("쓰러짐")]
        [Tooltip("켜면 쓰러지는 즉시 게임에서 제거된다. 끄면 쓰러진 채 남는다.")]
        [SerializeField] private bool removeOnDowned;

        [Header("부활")]
        [Tooltip("켜면 쓰러지고 Auto Revive Delay초 뒤 자동으로 부활한다. 끄면 바깥에서 부활시킬 때까지 쓰러져 있다.")]
        [SerializeField] private bool autoRevive;

        [SerializeField, Min(0f)] private float autoReviveDelay = 10f;

        [Tooltip("자동 부활이나 UnitRegistry.Revive로 되살릴 때의 위치.")]
        [SerializeField] private RevivePointRule revivePoint = RevivePointRule.DownedPosition;

        [Tooltip("부활 시 체력. 최대 체력에 대한 비율.")]
        [SerializeField, Range(0.01f, 1f)] private float reviveHealthRatio = 1f;

        /// <summary>지금 값으로 UnitDefinition을 새로 만든다. 값이 범위를 벗어나 있으면 예외가 난다.</summary>
        public UnitDefinition ToDefinition()
        {
            return new UnitDefinition(maxHealth, removeOnDowned, autoRevive, autoReviveDelay, revivePoint,
                reviveHealthRatio);
        }
    }
}
