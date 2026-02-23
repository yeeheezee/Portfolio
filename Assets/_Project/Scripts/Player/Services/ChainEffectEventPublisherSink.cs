namespace WizardBrawl.Player.Services
{
    /// <summary>
    /// 체인 대효과 성공 시 이벤트를 발행하는 싱크.
    /// </summary>
    public sealed class ChainEffectEventPublisherSink : ICastPresentationSink
    {
        public void OnCastEffectResolved(CastContext context, InjectionEffectResolution injection, ChainResolution chain)
        {
            if (!chain.IsSuccess || chain.Entry == null)
            {
                return;
            }

            ChainEffectEventBus.Publish(new ChainEffectAppliedEvent(context, chain.Entry));
        }
    }
}
