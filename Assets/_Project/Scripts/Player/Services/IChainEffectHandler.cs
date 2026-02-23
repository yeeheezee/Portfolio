using WizardBrawl.Magic.Data.SpellRecipe;

namespace WizardBrawl.Player.Services
{
    /// <summary>
    /// 체인 대효과 단일 타입 실행 계약.
    /// </summary>
    public interface IChainEffectHandler
    {
        SpellRecipeEffectType EffectType { get; }

        bool TryApply(CastContext context, ChainEffectEntry entry);
    }
}
