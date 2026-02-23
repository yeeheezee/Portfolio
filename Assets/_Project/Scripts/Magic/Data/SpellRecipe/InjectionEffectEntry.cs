using System;
using UnityEngine;
using WizardBrawl.Core;

namespace WizardBrawl.Magic.Data.SpellRecipe
{
    /// <summary>
    /// 주입 소효과 엔트리.
    /// </summary>
    [Serializable]
    public class InjectionEffectEntry
    {
        [Header("키")]
        [SerializeField] private SpellImpactType _impactType = SpellImpactType.None;
        [SerializeField] private ElementType _injectedElement = ElementType.None;

        [Header("소효과")]
        [SerializeField] private SpellRecipeEffectType _effectType = SpellRecipeEffectType.None;

        [Min(0f)]
        [SerializeField] private float _effectDuration;

        [SerializeField] private float _damageBonusFlat;

        [SerializeField] private float _damageMultiplier = 1f;

        [Min(0f)]
        [SerializeField] private float _shieldAmount;

        public InjectionEffectKey Key => new InjectionEffectKey(_impactType, _injectedElement);
        public SpellImpactType ImpactType => _impactType;
        public ElementType InjectedElement => _injectedElement;
        public SpellRecipeEffectType EffectType => _effectType;
        public float EffectDuration => _effectDuration;
        public float DamageBonusFlat => _damageBonusFlat;
        public float DamageMultiplier => _damageMultiplier;
        public float ShieldAmount => _shieldAmount;
    }
}
