namespace WizardBrawl.Player.Services
{
    /// <summary>
    /// 기본 캐스트 이후 소효과/대효과/연출 결정을 순차 처리하는 파이프라인.
    /// </summary>
    public sealed class CastPipeline
    {
        private readonly IInjectionEffectResolver _injectionResolver;
        private readonly IChainEffectResolver _chainResolver;
        private readonly ICastPresentationSink _presentationSink;

        public CastPipeline(
            IInjectionEffectResolver injectionResolver,
            IChainEffectResolver chainResolver,
            ICastPresentationSink presentationSink)
        {
            _injectionResolver = injectionResolver;
            _chainResolver = chainResolver;
            _presentationSink = presentationSink;
        }

        /// <summary>
        /// 기본 캐스트 성공 후 후속 레이어를 해석함.
        /// </summary>
        public void ProcessOnHit(CastContext context)
        {
            InjectionEffectResolution injection = _injectionResolver == null
                ? InjectionEffectResolution.None()
                : _injectionResolver.Resolve(context);

            ChainResolution chain = _chainResolver == null
                ? ChainResolution.None()
                : _chainResolver.Resolve(context);

            _presentationSink?.OnCastEffectResolved(context, injection, chain);
        }
    }
}
