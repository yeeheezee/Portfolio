using UnityEngine;
using WizardBrawl.Core;

namespace WizardBrawl.Enemy.Status
{
    /// <summary>
    /// 피해 적용과 궁 체인 증폭 판정을 관리함.
    /// </summary>
    public sealed class DamageStatusHandler
    {
        private const float UltimateChainBonus = 1.5f;

        /// <summary>
        /// 피해 이벤트를 적용하고 궁 체인 증폭 및 시간창 소비 여부를 반환함.
        /// </summary>
        public bool Apply(
            StatusEvent statusEvent,
            Health health,
            float incomingDamageMultiplier,
            bool hasCrowdControlWindow,
            out string transition)
        {
            transition = "blocked kind=Damage reason=missing_health";
            if (health == null)
            {
                return false;
            }

            float baseDamage = Mathf.Max(0f, statusEvent.Damage);
            float normalizedIncomingMultiplier = Mathf.Max(1f, incomingDamageMultiplier);
            bool isEnhancedUltimate = statusEvent.IsUltimate && hasCrowdControlWindow;
            float chainMultiplier = isEnhancedUltimate ? UltimateChainBonus : 1f;
            float finalDamage = baseDamage * normalizedIncomingMultiplier * chainMultiplier;

            health.TakeDamage(finalDamage);

            transition = $"enter type=Damage amount={finalDamage:F2} base={baseDamage:F2} incomingMul={normalizedIncomingMultiplier:F2} chainMul={chainMultiplier:F2} isUltimate={statusEvent.IsUltimate}";
            return isEnhancedUltimate;
        }
    }
}
