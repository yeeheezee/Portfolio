using System;
using WizardBrawl.Magic.Data.SpellRecipe;

namespace WizardBrawl.Player.Services
{
    /// <summary>
    /// 체인 대효과 적용 이벤트 버스.
    /// </summary>
    public static class ChainEffectEventBus
    {
        public static event Action<ChainEffectAppliedEvent> EffectApplied;

        public static void Publish(ChainEffectAppliedEvent payload)
        {
            EffectApplied?.Invoke(payload);
        }
    }

    public readonly struct ChainEffectAppliedEvent
    {
        public ChainEffectAppliedEvent(CastContext context, ChainEffectEntry entry)
        {
            Context = context;
            Entry = entry;
        }

        public CastContext Context { get; }
        public ChainEffectEntry Entry { get; }
    }
}
