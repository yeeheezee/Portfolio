namespace WizardBrawl.Magic.Data.SpellRecipe
{
    /// <summary>
    /// 체인 레시피가 적용되는 단계.
    /// </summary>
    public enum ChainStage
    {
        None = 0,
        DebuffToCrowdControl = 1,
        CrowdControlToUltimate = 2
    }
}
