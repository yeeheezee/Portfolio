using UnityEngine;
using WizardBrawl.Core;
using System;

namespace WizardBrawl.Enemy.Status
{
    /// <summary>
    /// 디버프 상태와 체인 시간창을 관리함.
    /// </summary>
    public sealed class DebuffStatusHandler
    {
        private float _debuffWindowUntilTime;
        private float _damageAmpUntilTime;
        private float _weakenUntilTime;
        private float _incomingDamageMultiplier = 1f;
        private float _attackDelayMultiplier = 1f;

        /// <summary>
        /// 현재 디버프 기준 피격 배율을 반환함.
        /// </summary>
        public float IncomingDamageMultiplier => _incomingDamageMultiplier;

        /// <summary>
        /// 현재 디버프 기준 공격 지연 배율을 반환함.
        /// </summary>
        public float AttackDelayMultiplier => _attackDelayMultiplier;

        /// <summary>
        /// 현재 시각 기준 디버프 체인 시간창 활성 여부를 반환함.
        /// </summary>
        public bool IsDebuffWindowActive(float now)
        {
            return now < _debuffWindowUntilTime;
        }

        /// <summary>
        /// 디버프 이벤트를 적용하고 체인 시간창 및 배율 상태를 갱신함.
        /// </summary>
        public string Apply(StatusEvent statusEvent)
        {
            if (!statusEvent.DebuffType.HasValue)
            {
                return "blocked kind=Debuff reason=missing_debuff_type";
            }

            float now = statusEvent.Timestamp;
            float duration = Mathf.Max(0f, statusEvent.Duration);
            float magnitude = Mathf.Max(0f, statusEvent.Magnitude);
            DebuffType debuffType = statusEvent.DebuffType.Value;

            _debuffWindowUntilTime = Mathf.Max(_debuffWindowUntilTime, now + duration);

            switch (debuffType)
            {
                case DebuffType.DefenseDown:
                case DebuffType.Vulnerability:
                    _incomingDamageMultiplier = Mathf.Max(_incomingDamageMultiplier, 1f + magnitude);
                    _damageAmpUntilTime = Mathf.Max(_damageAmpUntilTime, now + duration);
                    break;
                case DebuffType.Weaken:
                    _attackDelayMultiplier = Mathf.Max(_attackDelayMultiplier, 1f + magnitude);
                    _weakenUntilTime = Mathf.Max(_weakenUntilTime, now + duration);
                    break;
            }

            return $"enter type=Debuff:{debuffType} duration={duration:F2} magnitude={magnitude:F2}";
        }

        /// <summary>
        /// 디버프 시간창/배율 만료를 처리하고 만료 전이 로그를 기록함.
        /// </summary>
        public void Tick(float now, Action<string> emitTransition)
        {
            if (emitTransition == null)
            {
                return;
            }

            if (now >= _debuffWindowUntilTime && _debuffWindowUntilTime > 0f)
            {
                _debuffWindowUntilTime = 0f;
                emitTransition("expire type=DebuffWindow");
            }

            if (now >= _damageAmpUntilTime && _incomingDamageMultiplier > 1f)
            {
                _incomingDamageMultiplier = 1f;
                _damageAmpUntilTime = 0f;
                emitTransition("expire type=Debuff:DamageAmp");
            }

            if (now >= _weakenUntilTime && _attackDelayMultiplier > 1f)
            {
                _attackDelayMultiplier = 1f;
                _weakenUntilTime = 0f;
                emitTransition("expire type=Debuff:Weaken");
            }
        }
    }
}
