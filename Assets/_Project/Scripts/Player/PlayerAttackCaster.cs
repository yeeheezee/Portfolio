using UnityEngine;
using UnityEngine.InputSystem;
using WizardBrawl.Core;
using WizardBrawl.Data;

namespace WizardBrawl.Player
{
    /// <summary>
    /// 플레이어의 공격 입력을 받아, BaseCaster의 기능을 사용하여 공격 마법을 시전함.
    /// </summary>
    public class PlayerAttackCaster : BaseCaster
    {
        [Header("마법 슬롯")]
        [SerializeField]
        [Tooltip("주 공격으로 사용할 마법의 데이터 에셋.")]
        private MagicData _primaryAttackMagic;

        /// <summary>
        /// 주 공격 마법 시전을 시도함.
        /// 이 메서드는 Player Input 컴포넌트의 Unity Event에 의해 호출됨.
        /// </summary>
        public void PerformAttack()
        {
            UseSkill(_primaryAttackMagic);
        }
    }
}