using System;
using WizardBrawl.Core;

namespace WizardBrawl.Magic.Data.SpellRecipe
{
    /// <summary>
    /// 주입 소효과 조회 키(Impact + InjectedElement).
    /// </summary>
    [Serializable]
    public struct InjectionEffectKey : IEquatable<InjectionEffectKey>
    {
        public InjectionEffectKey(SpellImpactType impactType, ElementType injectedElement)
        {
            ImpactType = impactType;
            InjectedElement = injectedElement;
        }

        public SpellImpactType ImpactType { get; }
        public ElementType InjectedElement { get; }

        public bool Equals(InjectionEffectKey other)
        {
            return ImpactType == other.ImpactType
                && InjectedElement == other.InjectedElement;
        }

        public override bool Equals(object obj)
        {
            return obj is InjectionEffectKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)ImpactType * 397) ^ (int)InjectedElement;
            }
        }

        public override string ToString()
        {
            return $"{ImpactType}:{InjectedElement}";
        }
    }
}
