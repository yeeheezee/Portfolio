using System;
using UnityEngine;
using WizardBrawl.Core;

namespace WizardBrawl.Magic.Data.SpellRecipe
{
    /// <summary>
    /// 체인 키에 대응되는 마법 오버라이드/전달 분류 데이터.
    /// </summary>
    [Serializable]
    public class SpellRecipeEntry
    {
        [Header("키")]
        [SerializeField] private ChainStage _stage = ChainStage.None;
        [SerializeField] private ElementType _fromElement = ElementType.None;
        [SerializeField] private ElementType _toElement = ElementType.None;

        [Header("마법 선택")]
        [Tooltip("해당 키에서 시전 마법을 교체해야 할 때만 지정.")]
        [SerializeField] private MagicData _magicOverride;

        [Header("전달/명중 분류")]
        [Tooltip("전달 방식 분류. Auto면 기본 분류를 사용.")]
        [SerializeField] private SpellDeliveryType _deliveryType = SpellDeliveryType.Auto;

        [Tooltip("명중 효과 분류. Auto면 기본 분류를 사용.")]
        [SerializeField] private SpellImpactType _impactType = SpellImpactType.Auto;

        public SpellRecipeKey Key => new SpellRecipeKey(_stage, _fromElement, _toElement);
        public MagicData MagicOverride => _magicOverride;
        public SpellDeliveryType DeliveryType => _deliveryType;
        public SpellImpactType ImpactType => _impactType;
    }
}
