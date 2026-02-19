using UnityEngine;

namespace WizardBrawl.Core
{
    /// <summary>
    /// 상태 수신 단일 진입점에서 사용하는 공통 이벤트 데이터.
    /// </summary>
    public readonly struct StatusEvent
    {
        public StatusEventKind Kind { get; }
        public DebuffType? DebuffType { get; }
        public CrowdControlType? CrowdControlType { get; }
        public float Duration { get; }
        public float Magnitude { get; }
        public float Damage { get; }
        public bool IsUltimate { get; }
        public GameObject Source { get; }
        public float Timestamp { get; }

        private StatusEvent(
            StatusEventKind kind,
            DebuffType? debuffType,
            CrowdControlType? crowdControlType,
            float duration,
            float magnitude,
            float damage,
            bool isUltimate,
            GameObject source,
            float timestamp)
        {
            Kind = kind;
            DebuffType = debuffType;
            CrowdControlType = crowdControlType;
            Duration = duration;
            Magnitude = magnitude;
            Damage = damage;
            IsUltimate = isUltimate;
            Source = source;
            Timestamp = timestamp;
        }

        /// <summary>
        /// 디버프 이벤트를 현재 시각 기준으로 생성함.
        /// </summary>
        public static StatusEvent CreateDebuff(DebuffType debuffType, float duration, float magnitude, GameObject source)
        {
            return new StatusEvent(StatusEventKind.Debuff, debuffType, null, duration, magnitude, 0f, false, source, Time.time);
        }

        /// <summary>
        /// 군중제어 이벤트를 현재 시각 기준으로 생성함.
        /// </summary>
        public static StatusEvent CreateCrowdControl(CrowdControlType crowdControlType, float duration, float magnitude, GameObject source)
        {
            return new StatusEvent(StatusEventKind.CrowdControl, null, crowdControlType, duration, magnitude, 0f, false, source, Time.time);
        }

        /// <summary>
        /// 피해 이벤트를 현재 시각 기준으로 생성함.
        /// </summary>
        public static StatusEvent CreateDamage(float damage, bool isUltimate, GameObject source)
        {
            return new StatusEvent(StatusEventKind.Damage, null, null, 0f, 0f, damage, isUltimate, source, Time.time);
        }
    }
}
