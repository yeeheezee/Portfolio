using UnityEngine;
using WizardBrawl.Magic.Data.SpellRecipe;

namespace WizardBrawl.Player.Services
{
    /// <summary>
    /// ChainEffectTable 기반 대효과 리졸버.
    /// </summary>
    public sealed class TableChainEffectResolver : IChainEffectResolver
    {
        private readonly ChainEffectTable _table;

        public TableChainEffectResolver(ChainEffectTable table)
        {
            _table = table;
        }

        public ChainResolution Resolve(CastContext context)
        {
            if (context.ChainStage == ChainStage.None)
            {
                return ChainResolution.None();
            }

            if (_table == null)
            {
                Debug.Log("[ChainEffect] table missing");
                return ChainResolution.None();
            }

            ChainEffectKey key = new ChainEffectKey(context.ChainStage, context.ChainFromElement, context.ChainToElement);
            if (_table.TryGetEffect(key, out ChainEffectEntry entry))
            {
                Debug.Log($"[ChainEffect] hit key={key} type={entry.EffectType} duration={entry.EffectDuration:F2}");
                return ChainResolution.Success(entry);
            }

            Debug.Log($"[ChainEffect] missing key={key}");
            return ChainResolution.None();
        }
    }
}
