using System;
using WizardBrawl.Core;

namespace WizardBrawl.Magic.Data.SpellRecipe
{
    /// <summary>
    /// 체인 레시피 조회 키(단계/속성 순서).
    /// </summary>
    [Serializable]
    public struct SpellRecipeKey : IEquatable<SpellRecipeKey>
    {
        public SpellRecipeKey(
            ChainStage stage,
            ElementType fromElement,
            ElementType toElement)
        {
            Stage = stage;
            FromElement = fromElement;
            ToElement = toElement;
        }

        public ChainStage Stage { get; }
        public ElementType FromElement { get; }
        public ElementType ToElement { get; }

        public bool Equals(SpellRecipeKey other)
        {
            return Stage == other.Stage
                && FromElement == other.FromElement
                && ToElement == other.ToElement;
        }

        public override bool Equals(object obj)
        {
            return obj is SpellRecipeKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Stage, FromElement, ToElement);
        }

        public override string ToString()
        {
            return $"{Stage}:{FromElement}->{ToElement}";
        }
    }
}
