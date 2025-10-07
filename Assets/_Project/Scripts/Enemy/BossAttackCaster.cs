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

        // AI가 마법의 쿨타임을 외부에서 확인할 수 있도록 public으로 열어줌
        public MagicData StandardAttack => _standardAttack;
        public MagicData HeavyAttack => _heavyAttack;
        public MagicData UnparryableAttack => _unparryableAttack;


        // AI가 호출할 메서드들
        public void PerformStandardAttack()
        {
            UseSkill(_standardAttack);
        }

        public void PerformHeavyAttack()
        {
            UseSkill(_heavyAttack);
        }

        public void PerformUnparryableAttack()
        {
            UseSkill(_unparryableAttack);
        }
    }
}