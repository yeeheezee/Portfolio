using UnityEngine;

namespace WizardBrawl.Enemy
{
    /// <summary>
    /// 보스의 행동 패턴 및 스탯 데이터를 담는 ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_BossStats_", menuName = "Enemy/Boss Stats")]
    public class BossStats : ScriptableObject
    {
        [Header("움직임")]
        [Tooltip("보스의 기본 이동 속도")]
        public float MoveSpeed = 5f;
        [Tooltip("보스가 플레이어를 향해 회전하는 속도")]
        public float RotationSpeed = 5f;

        [Header("거리 유지")]
        [Tooltip("보스가 플레이어와 유지하려는 최적의 거리")]
        public float OptimalDistance = 15f;
        [Tooltip("최적 거리에서 벗어나도 행동을 바꾸지 않는 허용 오차 범위")]
        public float DistanceTolerance = 2f;

        [Header("공격 템포")]
        [Tooltip("일반 공격 후 다음 행동까지의 최소 대기 시간 (후딜레이)")]
        public float StandardAttackCooldown = 1.0f;
        [Tooltip("강한 공격 후 다음 행동까지의 최소 대기 시간 (후딜레이)")]
        public float HeavyAttackCooldown = 1.5f;
        [Tooltip("패링 불가 공격 후 다음 행동까지의 최소 대기 시간 (후딜레이)")]
        public float UnparryableAttackCooldown = 2.0f;
        [Tooltip("하나의 공격 사이클이 끝난 뒤, 다음 공격을 시작하기 전까지의 최소 휴식 시간")]
        public float RestBetweenAttacks = 0.8f;
    }
}