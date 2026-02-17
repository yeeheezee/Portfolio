using UnityEngine;
using WizardBrawl.Core;
using System;

namespace WizardBrawl.Enemy.Status
{
    /// <summary>
    /// CC 상태, 우선순위, 체인 시간창을 관리함.
    /// </summary>
    public sealed class CrowdControlStatusHandler
    {
        private float _stunUntilTime;
        private float _weakStaggerUntilTime;
        private float _slowUntilTime;
        private float _crowdControlWindowUntilTime;
        private float _slowMoveMultiplier = 1f;

        public bool IsStunned(float now) => now < _stunUntilTime;
        public bool IsWeakStaggered(float now) => now < _weakStaggerUntilTime;
        public bool IsCrowdControlWindowActive(float now) => now < _crowdControlWindowUntilTime;

        public float GetMoveMultiplier(float now)
        {
            if (IsStunned(now) || IsWeakStaggered(now))
            {
                return 0f;
            }

            return now < _slowUntilTime ? _slowMoveMultiplier : 1f;
        }

        public void ConsumeCrowdControlWindow()
        {
            _crowdControlWindowUntilTime = 0f;
        }

        public string Apply(StatusEvent statusEvent, bool hasDebuffWindow)
        {
            if (!statusEvent.CrowdControlType.HasValue)
            {
                return "blocked kind=CrowdControl reason=missing_control_type";
            }

            float now = statusEvent.Timestamp;
            float duration = Mathf.Max(0f, statusEvent.Duration);
            float magnitude = Mathf.Clamp01(statusEvent.Magnitude);
            CrowdControlType controlType = statusEvent.CrowdControlType.Value;

            if (controlType == CrowdControlType.Slow)
            {
                _slowUntilTime = Mathf.Max(_slowUntilTime, now + duration);
                _slowMoveMultiplier = Mathf.Min(_slowMoveMultiplier, Mathf.Clamp(1f - magnitude, 0.1f, 1f));
                return $"enter type=Slow duration={duration:F2} magnitude={magnitude:F2}";
            }

            if (hasDebuffWindow)
            {
                _stunUntilTime = Mathf.Max(_stunUntilTime, now + duration);
                _crowdControlWindowUntilTime = Mathf.Max(_crowdControlWindowUntilTime, now + duration);
                return $"enter type=Stun reason=debuff_window duration={duration:F2}";
            }

            if (IsStunned(now))
            {
                return "blocked type=WeakStagger reason=stun_priority";
            }

            _weakStaggerUntilTime = Mathf.Max(_weakStaggerUntilTime, now + duration);
            _crowdControlWindowUntilTime = Mathf.Max(_crowdControlWindowUntilTime, now + duration);
            return $"enter type=WeakStagger reason=no_debuff_window duration={duration:F2}";
        }

        public void Tick(float now, Action<string> emitTransition)
        {
            if (emitTransition == null)
            {
                return;
            }

            if (now >= _stunUntilTime && _stunUntilTime > 0f)
            {
                _stunUntilTime = 0f;
                emitTransition("expire type=Stun");
            }

            if (now >= _weakStaggerUntilTime && _weakStaggerUntilTime > 0f)
            {
                _weakStaggerUntilTime = 0f;
                emitTransition("expire type=WeakStagger");
            }

            if (now >= _slowUntilTime && _slowMoveMultiplier < 1f)
            {
                _slowMoveMultiplier = 1f;
                _slowUntilTime = 0f;
                emitTransition("expire type=Slow");
            }

            if (now >= _crowdControlWindowUntilTime && _crowdControlWindowUntilTime > 0f)
            {
                _crowdControlWindowUntilTime = 0f;
                emitTransition("expire type=CrowdControlWindow");
            }
        }
    }
}
