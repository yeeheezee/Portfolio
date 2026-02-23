using WizardBrawl.Magic.Data.SpellRecipe;

namespace WizardBrawl.Player.Services
{
    /// <summary>
    /// 체인 대효과 런타임 적용(EffectType 기반 핸들러 분기).
    /// </summary>
    public sealed class ChainRuntimeApplierSink : ICastPresentationSink
    {
        private readonly ChainEffectHandlerRegistry _registry;

        public ChainRuntimeApplierSink()
            : this(ChainEffectHandlerRegistry.CreateDefault())
        {
        }

        public ChainRuntimeApplierSink(ChainEffectHandlerRegistry registry)
        {
            _registry = registry;
        }

        public void OnCastEffectResolved(CastContext context, InjectionEffectResolution injection, ChainResolution chain)
        {
            if (!chain.IsSuccess || chain.Entry == null)
            {
                return;
            }

            if (_registry == null)
            {
                return;
            }

            if (!_registry.TryGetHandler(chain.Entry.EffectType, out IChainEffectHandler handler) || handler == null)
            {
                UnityEngine.Debug.Log($"[ChainRuntime] no_handler effectType={chain.Entry.EffectType}");
                return;
            }

            handler.TryApply(context, chain.Entry);
        }
    }
}
