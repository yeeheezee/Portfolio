using UnityEngine;
using WizardBrawl.Magic;

namespace WizardBrawl.Enemy
{
    /// <summary>
    /// BossAI의 공격 명령을 받아 실제 마법을 시전함.
    /// </summary>
    [RequireComponent(typeof(BossAI))]
    public class BossAttackCaster : BaseCaster
    {
        /// <summary>
        /// 엔트리의 스킬이 시전 가능한 상태인지 여부.
        /// </summary>
        public bool IsEntryReady(BossSpellEntry entry)
        {
            return entry != null && CanUseSkillNow(entry.Spell);
        }

        /// <summary>
        /// 선택된 엔트리를 시전함.
        /// </summary>
        public bool TryCast(BossSpellEntry entry)
        {
            if (entry == null)
            {
                Debug.LogError("[BossCast] CRIT_NULL_REF: entry is null", this);
                return false;
            }

            if (entry.Spell == null)
            {
                Debug.LogError("[BossCast] CRIT_NULL_REF: entry.spell is null", this);
                return false;
            }

            bool casted = TryUseSkill(entry.Spell, transform.forward);
            if (!casted)
            {
                Debug.Log($"[BossCast] blocked spell={entry.Spell.MagicName}");
                return false;
            }

            Debug.Log($"[BossCast] fire spell={entry.Spell.MagicName} tier={entry.Tier} parry={entry.ParryRule} phaseGate={entry.PhaseGate}");
            return true;
        }
    }
}
