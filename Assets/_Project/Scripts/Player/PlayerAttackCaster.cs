using UnityEngine;
using UnityEngine.InputSystem;
using WizardBrawl.Core;
using WizardBrawl.Magic;
using WizardBrawl.Magic.Data;

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
        [Header("메인 카메라")]
        [SerializeField]
        [Tooltip("마법을 시전할 방향을 정할 메인카메라")]
        private Camera _mainCamera;
        private void Awake() // Start 대신 Awake 사용 권장
        {
            // BaseCaster의 Awake도 호출될 수 있으니, 필요한 초기화는 여기서
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
        }

        /// <summary>
        /// 주 공격 마법 시전을 시도함.
        /// 이 메서드는 Player Input 컴포넌트의 Unity Event에 의해 호출됨.
        /// </summary>
        public void PerformAttack()
        {
            if (_mainCamera == null)
            {
                Debug.LogError("메인 카메라가 없습니다!");
                return;
            }
            Vector3 fireDirection = _mainCamera.transform.forward;
            UseSkill(_primaryAttackMagic, fireDirection);
        }
    }
}