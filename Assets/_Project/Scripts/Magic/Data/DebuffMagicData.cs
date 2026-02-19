using UnityEngine;
using WizardBrawl.Core;
using WizardBrawl.Magic.Effects;

namespace WizardBrawl.Magic.Data
{
    /// <summary>
    /// 디버프 적용형 마법 데이터.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDebuffMagic", menuName = "Magic/Debuff Magic Data")]
    public class DebuffMagicData : MagicData
    {
        [Header("디버프 설정")]
        [Tooltip("적용할 디버프 타입.")]
        [SerializeField] private DebuffType _debuffType = DebuffType.DefenseDown;

        [Tooltip("디버프 지속 시간(초).")]
        [SerializeField] private float _duration = 3f;

        [Tooltip("디버프 크기(정규화 값).")]
        [SerializeField] private float _magnitude = 0.2f;

        [Tooltip("디버프 전달용 투사체 프리팹.")]
        [SerializeField] private GameObject _projectilePrefab;

        [Tooltip("투사체 속도.")]
        [SerializeField] private float _projectileSpeed = 16f;

        [Tooltip("투사체 수명(초).")]
        [SerializeField] private float _projectileLifetime = 3f;

        [Tooltip("효과 적용 대상 레이어.")]
        [SerializeField] private LayerMask _targetLayers = ~0;

        [Header("폭발 슬로우 설정")]
        [Tooltip("투사체 폭발 반경.")]
        [SerializeField] private float _burstRadius = 2.8f;

        [Tooltip("폭발 슬로우 지속 시간(초).")]
        [SerializeField] private float _burstSlowDuration = 1.5f;

        [Tooltip("폭발 슬로우 강도(정규화 값).")]
        [SerializeField] private float _burstSlowStrength = 0.3f;

        public DebuffType DebuffType => _debuffType;
        public float Duration => _duration;
        public float Magnitude => _magnitude;
        public GameObject ProjectilePrefab => _projectilePrefab;
        public float ProjectileSpeed => _projectileSpeed;
        public float ProjectileLifetime => _projectileLifetime;
        public LayerMask TargetLayers => _targetLayers;
        public float BurstRadius => _burstRadius;
        public float BurstSlowDuration => _burstSlowDuration;
        public float BurstSlowStrength => _burstSlowStrength;

        /// <summary>
        /// 데이터에 대응하는 디버프 실행 효과를 생성함.
        /// </summary>
        public override IMagicEffect CreateEffect()
        {
            return new DebuffEffect(this);
        }
    }
}
