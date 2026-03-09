using UnityEngine;
using WizardBrawl.Core;
using WizardBrawl.Magic.Effects;

namespace WizardBrawl.Magic.Data
{
    /// <summary>
    /// 직선 빔(즉시 판정) 마법 데이터.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBeamMagic", menuName = "Magic/Beam Magic Data")]
    public class BeamMagicData : MagicData
    {
        [Header("빔 설정")]
        [Tooltip("빔 최대 사거리.")]
        [SerializeField] private float _range = 18f;

        [Tooltip("빔 두께(캡슐 반경).")]
        [SerializeField] private float _radius = 0.35f;

        [Tooltip("빔 피해량.")]
        [SerializeField] private float _damage = 18f;

        [Tooltip("효과 적용 대상 레이어.")]
        [SerializeField] private LayerMask _targetLayers = ~0;

        [Tooltip("궁극기 체인 판정용 빔 여부.")]
        [SerializeField] private bool _isUltimateHit;

        [Header("비주얼")]
        [Tooltip("빔 시각 효과 프리팹(선택).")]
        [SerializeField] private GameObject _beamVfxPrefab;

        [Tooltip("빔 시각 효과 유지 시간(초).")]
        [SerializeField] private float _beamVfxLifetime = 0.35f;

        public float Range => _range;
        public float Radius => _radius;
        public float Damage => _damage;
        public LayerMask TargetLayers => _targetLayers;
        public bool IsUltimateHit => _isUltimateHit;
        public GameObject BeamVfxPrefab => _beamVfxPrefab;
        public float BeamVfxLifetime => _beamVfxLifetime;

        public override IMagicEffect CreateEffect()
        {
            return new BeamEffect(this);
        }
    }
}
