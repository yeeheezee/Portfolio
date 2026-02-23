using WizardBrawl.Magic.Data.SpellRecipe;

namespace WizardBrawl.Player.Services
{
    /// <summary>
    /// 주입 소효과 조회 결과.
    /// </summary>
    public readonly struct InjectionEffectResolution
    {
        private InjectionEffectResolution(bool hasEffect, InjectionEffectEntry entry)
        {
            HasEffect = hasEffect;
            Entry = entry;
        }

        public bool HasEffect { get; }
        public InjectionEffectEntry Entry { get; }

        public static InjectionEffectResolution None()
        {
            return new InjectionEffectResolution(false, null);
        }

        public static InjectionEffectResolution Hit(InjectionEffectEntry entry)
        {
            return new InjectionEffectResolution(entry != null, entry);
        }
    }
}
