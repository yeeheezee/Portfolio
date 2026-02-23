using UnityEngine;
using WizardBrawl.Core;
using WizardBrawl.Magic.Data.SpellRecipe;

namespace WizardBrawl.Player.Services
{
    /// <summary>
    /// InjectionEffectTable 기반 소효과 리졸버.
    /// </summary>
    public sealed class TableInjectionEffectResolver : IInjectionEffectResolver
    {
        private readonly InjectionEffectTable _table;

        public TableInjectionEffectResolver(InjectionEffectTable table)
        {
            _table = table;
        }

        public InjectionEffectResolution Resolve(CastContext context)
        {
            if (!context.HadInjectedElement || context.InjectedElement == ElementType.None)
            {
                return InjectionEffectResolution.None();
            }

            if (_table == null)
            {
                Debug.Log("[InjectEffect] table missing");
                return InjectionEffectResolution.None();
            }

            InjectionEffectKey key = new InjectionEffectKey(context.ImpactType, context.InjectedElement);
            if (_table.TryGetEffect(key, out InjectionEffectEntry entry))
            {
                Debug.Log($"[InjectEffect] hit key={key} type={entry.EffectType} duration={entry.EffectDuration:F2}");
                return InjectionEffectResolution.Hit(entry);
            }

            Debug.Log($"[InjectEffect] missing key={key}");
            return InjectionEffectResolution.None();
        }
    }
}
