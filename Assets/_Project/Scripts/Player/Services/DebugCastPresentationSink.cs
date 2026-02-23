using UnityEngine;

namespace WizardBrawl.Player.Services
{
    /// <summary>
    /// 소효과/대효과 해석 결과를 로그로 출력하는 임시 프레젠테이션 싱크.
    /// </summary>
    public sealed class DebugCastPresentationSink : ICastPresentationSink
    {
        public void OnCastEffectResolved(CastContext context, InjectionEffectResolution injection, ChainResolution chain)
        {
            if (!injection.HasEffect && !chain.IsSuccess)
            {
                return;
            }

            string injectionText = injection.HasEffect
                ? $"{injection.Entry.EffectType}/{injection.Entry.EffectDuration:F2}s"
                : "none";
            string chainText = chain.IsSuccess && chain.Entry != null
                ? $"{chain.Entry.EffectType}/{chain.Entry.EffectDuration:F2}s"
                : "none";
            Debug.Log($"[Presentation] slot={context.Slot} inject={injectionText} chain={chainText}");
        }
    }
}
