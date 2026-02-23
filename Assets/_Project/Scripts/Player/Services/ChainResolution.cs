using WizardBrawl.Magic.Data.SpellRecipe;

namespace WizardBrawl.Player.Services
{
    /// <summary>
    /// 체인 대효과 조회 결과.
    /// </summary>
    public readonly struct ChainResolution
    {
        private ChainResolution(bool isSuccess, ChainEffectEntry entry)
        {
            IsSuccess = isSuccess;
            Entry = entry;
        }

        public bool IsSuccess { get; }
        public ChainEffectEntry Entry { get; }

        public static ChainResolution None()
        {
            return new ChainResolution(false, null);
        }

        public static ChainResolution Success(ChainEffectEntry entry)
        {
            return new ChainResolution(entry != null, entry);
        }
    }
}
