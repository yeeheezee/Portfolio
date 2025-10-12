using UnityEngine;
using WizardBrawl.Magic;
using WizardBrawl.Magic.Data;

namespace WizardBrawl.Enemy
{
    [RequireComponent(typeof(BossAI))]
    public class BossAttackCaster : BaseCaster
    {
        [Header("보스 마법 슬롯")]
        [SerializeField] private MagicData _standardAttack;
        [SerializeField] private MagicData _heavyAttack;
        [SerializeField] private MagicData _unparryableAttack;
        // TODO: 연속 공격(콤보)을 위한 데이터 리스트 추가

        // AI는 MagicData의 존재를 알 필요 없이, 이 프로퍼티만 확인하면 되도록 변경.
        public bool IsStandardAttackReady => IsSkillReady(_standardAttack);
        public bool IsHeavyAttackReady => IsSkillReady(_heavyAttack);
        public bool IsUnparryableAttackReady => IsSkillReady(_unparryableAttack);


        // AI가 호출할 메서드들
        public void PerformStandardAttack()
        {
            UseSkill(_standardAttack, transform.forward);
        }

        public void PerformHeavyAttack()
        {
            UseSkill(_heavyAttack, transform.forward);
        }

        public void PerformUnparryableAttack()
        {
            UseSkill(_unparryableAttack, transform.forward);
        }
    }
}