using System;
using UnityEngine;
using WizardBrawl.Magic.Data;

namespace WizardBrawl.Enemy
{
    /// <summary>
    /// 보스 스킬 풀 엔트리 데이터.
    /// </summary>
    [Serializable]
    public class BossSpellEntry
    {
        [Header("마법 참조")]
        [SerializeField] private MagicData _spell;

        [Header("축 분류")]
        [SerializeField] private BossSpellTier _tier = BossSpellTier.Standard;
        [SerializeField] private BossParryRule _parryRule = BossParryRule.Parryable;
        [SerializeField] private BossPhaseGate _phaseGate = BossPhaseGate.AllPhases;

        public MagicData Spell => _spell;
        public BossSpellTier Tier => _tier;
        public BossParryRule ParryRule => _parryRule;
        public BossPhaseGate PhaseGate => _phaseGate;
    }
}
