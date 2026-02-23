using UnityEngine;
using WizardBrawl.Core;
using WizardBrawl.Magic.Data.SpellRecipe;

namespace WizardBrawl.Player.Services
{
    public sealed class SlowChainEffectHandler : IChainEffectHandler
    {
        public SpellRecipeEffectType EffectType => SpellRecipeEffectType.Slow;

        public bool TryApply(CastContext context, ChainEffectEntry entry)
        {
            float duration = Mathf.Max(0f, entry.EffectDuration);
            if (duration <= 0f)
            {
                return false;
            }

            Collider[] hits = ChainEffectTargetingUtility.CollectTargets(context, entry);
            int applied = 0;
            for (int i = 0; i < hits.Length; i++)
            {
                GameObject target = hits[i].gameObject;
                if (target == context.Caster)
                {
                    continue;
                }

                if (target.TryGetComponent<IStatusReceiver>(out IStatusReceiver receiver))
                {
                    receiver.ApplyStatus(StatusEvent.CreateCrowdControl(CrowdControlType.Slow, duration, 0.5f, context.Caster));
                    applied++;
                }
            }

            if (applied > 0)
            {
                Debug.Log($"[ChainRuntime] effect=Slow applied={applied} duration={duration:F2}");
            }

            return applied > 0;
        }
    }
}
