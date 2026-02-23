using System;
using UnityEngine;
using WizardBrawl.Core;

namespace WizardBrawl.Magic.Data.SpellRecipe
{
    /// <summary>
    /// 체인 대효과 엔트리.
    /// </summary>
    [Serializable]
    public class ChainEffectEntry
    {
        [Header("키")]
        [SerializeField] private ChainStage _stage = ChainStage.None;
        [SerializeField] private ElementType _fromElement = ElementType.None;
        [SerializeField] private ElementType _toElement = ElementType.None;

        [Header("대효과")]
        [SerializeField] private SpellRecipeEffectType _effectType = SpellRecipeEffectType.None;

        [Min(0f)]
        [SerializeField] private float _effectDuration;

        [Min(0f)]
        [SerializeField] private float _effectRadius;

        [SerializeField] private LayerMask _effectLayers = ~0;

        [SerializeField] private float _stunDurationBonus;

        [SerializeField] private float _ultimateDamageMultiplier = 1f;

        [SerializeField] private bool _allowChainUltimate;

        public ChainEffectKey Key => new ChainEffectKey(_stage, _fromElement, _toElement);
        public ChainStage Stage => _stage;
        public ElementType FromElement => _fromElement;
        public ElementType ToElement => _toElement;
        public SpellRecipeEffectType EffectType => _effectType;
        public float EffectDuration => _effectDuration;
        public float EffectRadius => _effectRadius;
        public LayerMask EffectLayers => _effectLayers;
        public float StunDurationBonus => _stunDurationBonus;
        public float UltimateDamageMultiplier => _ultimateDamageMultiplier;
        public bool AllowChainUltimate => _allowChainUltimate;
    }
}
