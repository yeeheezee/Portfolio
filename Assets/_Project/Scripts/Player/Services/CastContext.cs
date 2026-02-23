using UnityEngine;
using WizardBrawl.Core;
using WizardBrawl.Magic.Data;
using WizardBrawl.Magic.Data.SpellRecipe;

namespace WizardBrawl.Player.Services
{
    /// <summary>
    /// 단일 캐스트 실행 컨텍스트.
    /// </summary>
    public readonly struct CastContext
    {
        public CastContext(
            GameObject caster,
            CastSlotType slot,
            SpellImpactType impactType,
            MagicData selectedMagic,
            ElementType injectedElement,
            bool hadInjectedElement,
            ElementCombinationType combo,
            Vector3 targetPoint,
            float effectRadius,
            int effectLayerMask,
            ChainStage chainStage,
            ElementType chainFromElement,
            ElementType chainToElement)
        {
            Caster = caster;
            Slot = slot;
            ImpactType = impactType;
            SelectedMagic = selectedMagic;
            InjectedElement = injectedElement;
            HadInjectedElement = hadInjectedElement;
            Combo = combo;
            TargetPoint = targetPoint;
            EffectRadius = effectRadius;
            EffectLayerMask = effectLayerMask;
            ChainStage = chainStage;
            ChainFromElement = chainFromElement;
            ChainToElement = chainToElement;
        }

        public GameObject Caster { get; }
        public CastSlotType Slot { get; }
        public SpellImpactType ImpactType { get; }
        public MagicData SelectedMagic { get; }
        public ElementType InjectedElement { get; }
        public bool HadInjectedElement { get; }
        public ElementCombinationType Combo { get; }
        public Vector3 TargetPoint { get; }
        public float EffectRadius { get; }
        public int EffectLayerMask { get; }
        public ChainStage ChainStage { get; }
        public ElementType ChainFromElement { get; }
        public ElementType ChainToElement { get; }
    }
}
