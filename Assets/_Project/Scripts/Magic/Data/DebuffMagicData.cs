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

        [Tooltip("적용 범위 반경.")]
        [SerializeField] private float _radius = 2.5f;

        [Tooltip("효과 적용 대상 레이어.")]
        [SerializeField] private LayerMask _targetLayers = ~0;

        public DebuffType DebuffType => _debuffType;
        public float Duration => _duration;
        public float Magnitude => _magnitude;
        public float Radius => _radius;
        public LayerMask TargetLayers => _targetLayers;

        public override IMagicEffect CreateEffect()
        {
            return new DebuffEffect(this);
        }
    }
}
