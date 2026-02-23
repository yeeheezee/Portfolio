namespace WizardBrawl.Player.Services
{
    /// <summary>
    /// 주입 소효과 조회 계약.
    /// </summary>
    public interface IInjectionEffectResolver
    {
        InjectionEffectResolution Resolve(CastContext context);
    }

    /// <summary>
    /// 체인 대효과 조회 계약.
    /// </summary>
    public interface IChainEffectResolver
    {
        ChainResolution Resolve(CastContext context);
    }

    /// <summary>
    /// 효과 적용 결과를 프레젠테이션 계층으로 전달하는 계약.
    /// </summary>
    public interface ICastPresentationSink
    {
        void OnCastEffectResolved(CastContext context, InjectionEffectResolution injection, ChainResolution chain);
    }

    /// <summary>
    /// 주입 효과를 사용하지 않는 기본 리졸버.
    /// </summary>
    public sealed class NullInjectionEffectResolver : IInjectionEffectResolver
    {
        public InjectionEffectResolution Resolve(CastContext context)
        {
            return InjectionEffectResolution.None();
        }
    }

    /// <summary>
    /// 체인 효과를 사용하지 않는 기본 리졸버.
    /// </summary>
    public sealed class NullChainEffectResolver : IChainEffectResolver
    {
        public ChainResolution Resolve(CastContext context)
        {
            return ChainResolution.None();
        }
    }

    /// <summary>
    /// 프레젠테이션 전달을 사용하지 않는 기본 싱크.
    /// </summary>
    public sealed class NullCastPresentationSink : ICastPresentationSink
    {
        public void OnCastEffectResolved(CastContext context, InjectionEffectResolution injection, ChainResolution chain)
        {
        }
    }
}
