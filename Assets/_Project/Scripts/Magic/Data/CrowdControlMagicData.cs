using UnityEngine;
using WizardBrawl.Core;
using WizardBrawl.Magic.Effects;

namespace WizardBrawl.Magic.Data
{
    /// <summary>
    /// CC 적용형 마법 데이터.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCrowdControlMagic", menuName = "Magic/Crowd Control Magic Data")]
    public class CrowdControlMagicData : MagicData
    {
        [Header("CC 설정")]
        [Tooltip("적용할 CC 타입.")]
        [SerializeField] private CrowdControlType _controlType = CrowdControlType.Stun;

        [Tooltip("CC 지속 시간(초).")]
        [SerializeField] private float _duration = 1.5f;

        [Tooltip("CC 강도(정규화 값).")]
        [SerializeField] private float _strength = 1f;

        [Tooltip("적용 범위 반경.")]
        [SerializeField] private float _radius = 2.5f;

        [Tooltip("효과 적용 대상 레이어.")]
        [SerializeField] private LayerMask _targetLayers = ~0;

        public CrowdControlType ControlType => _controlType;
        public float Duration => _duration;
        public float Strength => _strength;
        public float Radius => _radius;
        public LayerMask TargetLayers => _targetLayers;

        public override IMagicEffect CreateEffect()
        {
            return new CrowdControlEffect(this);
        }
    }
}
