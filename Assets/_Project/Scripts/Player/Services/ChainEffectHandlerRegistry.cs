using System.Collections.Generic;
using WizardBrawl.Magic.Data.SpellRecipe;

namespace WizardBrawl.Player.Services
{
    public sealed class ChainEffectHandlerRegistry
    {
        private readonly Dictionary<SpellRecipeEffectType, IChainEffectHandler> _handlers;

        public ChainEffectHandlerRegistry(IEnumerable<IChainEffectHandler> handlers)
        {
            _handlers = new Dictionary<SpellRecipeEffectType, IChainEffectHandler>();
            foreach (IChainEffectHandler handler in handlers)
            {
                if (handler == null)
                {
                    continue;
                }

                _handlers[handler.EffectType] = handler;
            }
        }

        public bool TryGetHandler(SpellRecipeEffectType effectType, out IChainEffectHandler handler)
        {
            return _handlers.TryGetValue(effectType, out handler);
        }

        public static ChainEffectHandlerRegistry CreateDefault()
        {
            return new ChainEffectHandlerRegistry(new IChainEffectHandler[]
            {
                new StunChainEffectHandler(),
                new RootChainEffectHandler(),
                new FreezeChainEffectHandler(),
                new SlowChainEffectHandler()
            });
        }
    }
}
