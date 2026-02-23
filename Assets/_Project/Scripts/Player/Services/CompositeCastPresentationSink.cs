namespace WizardBrawl.Player.Services
{
    /// <summary>
    /// 복수 싱크에 동일한 결과를 전달하는 합성 싱크.
    /// </summary>
    public sealed class CompositeCastPresentationSink : ICastPresentationSink
    {
        private readonly ICastPresentationSink[] _sinks;

        public CompositeCastPresentationSink(params ICastPresentationSink[] sinks)
        {
            _sinks = sinks;
        }

        public void OnCastEffectResolved(CastContext context, InjectionEffectResolution injection, ChainResolution chain)
        {
            if (_sinks == null)
            {
                return;
            }

            for (int i = 0; i < _sinks.Length; i++)
            {
                _sinks[i]?.OnCastEffectResolved(context, injection, chain);
            }
        }
    }
}
