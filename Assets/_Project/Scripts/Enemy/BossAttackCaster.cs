using UnityEngine;
using WizardBrawl.Magic;
using WizardBrawl.Magic.Data;

namespace WizardBrawl.Enemy
{
    /// <summary>
    /// BossAI의 공격 명령을 받아 실제 마법을 시전함.
    /// </summary>
    [RequireComponent(typeof(BossAI))]
    public class BossAttackCaster : BaseCaster
    {
        [Header("보스 마법 슬롯")]
        [SerializeField] private MagicData _standardAttack;
        [SerializeField] private MagicData _heavyAttack;
        [SerializeField] private MagicData _unparryableAttack;

        /// <summary>
        /// 일반 공격이 사용 가능한 상태인지 여부.
        /// </summary>
        public bool IsStandardAttackReady => IsSkillReady(_standardAttack);

        /// <summary>
        /// 강한 공격이 사용 가능한 상태인지 여부.
        /// </summary>
        public bool IsHeavyAttackReady => IsSkillReady(_heavyAttack);

        /// <summary>
        /// 패링 불가 공격이 사용 가능한 상태인지 여부.
        /// </summary>
        public bool IsUnparryableAttackReady => IsSkillReady(_unparryableAttack);

        /// <summary>
        /// 일반 공격을 수행함.
        /// </summary>
        public void PerformStandardAttack() => UseSkill(_standardAttack, transform.forward);

        /// <summary>
        /// 강한 공격을 수행함.
        /// </summary>
        public void PerformHeavyAttack() => UseSkill(_heavyAttack, transform.forward);

        /// <summary>
        /// 패링 불가 공격을 수행함.
        /// </summary>
        public void PerformUnparryableAttack() => UseSkill(_unparryableAttack, transform.forward);
    }
}