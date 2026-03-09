using UnityEngine;
using WizardBrawl.Core;

namespace WizardBrawl.Enemy.Status
{
    /// <summary>
    /// 상태 이벤트를 핸들러로 라우팅하고 통합 상태를 제공함.
    /// </summary>
    public sealed class CombatStatusController
    {
        private readonly DebuffStatusHandler _debuffHandler = new DebuffStatusHandler();
        private readonly CrowdControlStatusHandler _crowdControlHandler = new CrowdControlStatusHandler();
        private readonly DamageStatusHandler _damageHandler = new DamageStatusHandler();

        private readonly Health _health;

        /// <summary>
        /// 상태 컨트롤러를 생성하고 체력 의존성을 주입함.
        /// </summary>
        public CombatStatusController(Health health)
        {
            _health = health;
        }

        /// <summary>
        /// 현재 디버프 기준 피격 배율을 반환함.
        /// </summary>
        public float IncomingDamageMultiplier => _debuffHandler.IncomingDamageMultiplier;

        /// <summary>
        /// 현재 디버프 기준 공격 지연 배율을 반환함.
        /// </summary>
        public float AttackDelayMultiplier => _debuffHandler.AttackDelayMultiplier;

        /// <summary>
        /// 현재 시각 기준 스턴 상태 여부를 반환함.
        /// </summary>
        public bool IsStunned(float now) => _crowdControlHandler.IsStunned(now);

        /// <summary>
        /// 현재 시각 기준 약경직 상태 여부를 반환함.
        /// </summary>
        public bool IsWeakStaggered(float now) => _crowdControlHandler.IsWeakStaggered(now);

        /// <summary>
        /// 현재 시각 기준 이동 배율을 반환함.
        /// </summary>
        public float GetMoveMultiplier(float now) => _crowdControlHandler.GetMoveMultiplier(now);

        /// <summary>
        /// 현재 시각 기준 궁 체인 가능 여부를 반환함.
        /// </summary>
        public bool IsUltimateChainReady(float now) => _crowdControlHandler.IsCrowdControlWindowActive(now);

        /// <summary>
        /// 상태 이벤트를 종류별 핸들러로 라우팅하고 전이 로그를 기록함.
        /// </summary>
        public void Apply(StatusEvent statusEvent)
        {
            string transition;
            switch (statusEvent.Kind)
            {
                case StatusEventKind.Debuff:
                    transition = _debuffHandler.Apply(statusEvent);
                    LogTransition(transition);
                    break;
                case StatusEventKind.CrowdControl:
                    bool hasDebuffWindow = _debuffHandler.IsDebuffWindowActive(statusEvent.Timestamp);
                    transition = _crowdControlHandler.Apply(statusEvent, hasDebuffWindow);
                    LogTransition(transition);
                    break;
                case StatusEventKind.Damage:
                    bool hasCrowdControlWindow = _crowdControlHandler.IsCrowdControlWindowActive(statusEvent.Timestamp);
                    bool consumed = _damageHandler.Apply(
                        statusEvent,
                        _health,
                        _debuffHandler.IncomingDamageMultiplier,
                        hasCrowdControlWindow,
                        out transition);
                    LogTransition(transition);
                    if (consumed)
                    {
                        _crowdControlHandler.ConsumeCrowdControlWindow();
                        LogTransition("consume type=CrowdControlWindow reason=ultimate_chain_success");
                }
                    break;
                default:
                    LogTransition($"blocked kind={statusEvent.Kind} reason=unsupported_kind");
                    break;
            }
        }

        /// <summary>
        /// 만료 시각을 기준으로 상태 갱신과 만료 로그를 처리함.
        /// </summary>
        public void Tick(float now)
        {
            _debuffHandler.Tick(now, LogTransition);
            _crowdControlHandler.Tick(now, LogTransition);
        }

        private void LogTransition(string transition)
        {
            Debug.Log($"[StateTransition] {transition}");
        }
    }
}
