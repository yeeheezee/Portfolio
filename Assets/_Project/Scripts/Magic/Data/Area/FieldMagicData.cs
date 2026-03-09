using UnityEngine;
using WizardBrawl.Core;
using WizardBrawl.Magic.Effects;

namespace WizardBrawl.Magic.Data
{
    /// <summary>
    /// 장판(원형 범위) 마법 데이터.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFieldMagic", menuName = "Magic/Field Magic Data")]
    public class FieldMagicData : MagicData
    {
        [Header("장판 설정")]
        [Tooltip("장판 반경.")]
        [SerializeField] private float _radius = 3.5f;

        [Tooltip("틱당 장판 피해량.")]
        [SerializeField] private float _damagePerTick = 12f;

        [Tooltip("장판 지속 시간(초).")]
        [SerializeField] private float _duration = 3f;

        [Tooltip("장판 틱 간격(초).")]
        [SerializeField] private float _tickInterval = 0.5f;

        [Tooltip("효과 적용 대상 레이어.")]
        [SerializeField] private LayerMask _targetLayers = ~0;

        [Tooltip("궁극기 체인 판정용 장판 여부.")]
        [SerializeField] private bool _isUltimateHit;

        [Header("비주얼")]
        [Tooltip("장판 시각 효과 프리팹(선택).")]
        [SerializeField] private GameObject _fieldVfxPrefab;

        public float Radius => _radius;
        public float DamagePerTick => _damagePerTick;
        public float Duration => _duration;
        public float TickInterval => _tickInterval;
        public LayerMask TargetLayers => _targetLayers;
        public bool IsUltimateHit => _isUltimateHit;
        public GameObject FieldVfxPrefab => _fieldVfxPrefab;

        public override IMagicEffect CreateEffect()
        {
            return new FieldEffect(this);
        }
    }
}
