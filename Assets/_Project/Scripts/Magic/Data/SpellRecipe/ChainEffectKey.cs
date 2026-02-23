using System;
using WizardBrawl.Core;

namespace WizardBrawl.Magic.Data.SpellRecipe
{
    /// <summary>
    /// 체인 대효과 조회 키(Stage + From + To).
    /// </summary>
    [Serializable]
    public struct ChainEffectKey : IEquatable<ChainEffectKey>
    {
        public ChainEffectKey(ChainStage stage, ElementType fromElement, ElementType toElement)
        {
            Stage = stage;
            FromElement = fromElement;
            ToElement = toElement;
        }

        public ChainStage Stage { get; }
        public ElementType FromElement { get; }
        public ElementType ToElement { get; }

        public bool Equals(ChainEffectKey other)
        {
            return Stage == other.Stage
                && FromElement == other.FromElement
                && ToElement == other.ToElement;
        }

        public override bool Equals(object obj)
        {
            return obj is ChainEffectKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Stage;
                hash = (hash * 397) ^ (int)FromElement;
                hash = (hash * 397) ^ (int)ToElement;
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{Stage}:{FromElement}->{ToElement}";
        }
    }
}
