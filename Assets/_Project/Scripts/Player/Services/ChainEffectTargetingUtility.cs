using UnityEngine;

namespace WizardBrawl.Player.Services
{
    internal static class ChainEffectTargetingUtility
    {
        public static Collider[] CollectTargets(CastContext context, Magic.Data.SpellRecipe.ChainEffectEntry entry)
        {
            float radius = entry != null && entry.EffectRadius > 0f
                ? entry.EffectRadius
                : context.EffectRadius;
            int layerMask = entry != null && entry.EffectLayers.value != 0
                ? entry.EffectLayers.value
                : context.EffectLayerMask;

            if (radius <= 0f)
            {
                Debug.LogWarning("[ChainRuntime] blocked: invalid effect radius");
                return System.Array.Empty<Collider>();
            }

            if (layerMask == 0)
            {
                Debug.LogWarning("[ChainRuntime] blocked: invalid effect layer mask");
                return System.Array.Empty<Collider>();
            }

            return Physics.OverlapSphere(context.TargetPoint, radius, layerMask);
        }
    }
}
