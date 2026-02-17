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

        public CombatStatusController(Health health)
        {
            _health = health;
        }

        public float IncomingDamageMultiplier => _debuffHandler.IncomingDamageMultiplier;
        public float AttackDelayMultiplier => _debuffHandler.AttackDelayMultiplier;

        public bool IsStunned(float now) => _crowdControlHandler.IsStunned(now);
        public bool IsWeakStaggered(float now) => _crowdControlHandler.IsWeakStaggered(now);
        public float GetMoveMultiplier(float now) => _crowdControlHandler.GetMoveMultiplier(now);
        public bool IsUltimateChainReady(float now) => _crowdControlHandler.IsCrowdControlWindowActive(now);

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
