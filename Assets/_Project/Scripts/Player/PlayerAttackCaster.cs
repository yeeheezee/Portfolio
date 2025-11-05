using UnityEngine;
using WizardBrawl.Magic;
using WizardBrawl.Magic.Data;

namespace WizardBrawl.Player
{
    /// <summary>
    /// 플레이어의 공격 입력을 받아 마법을 시전함.
    /// </summary>
    public class PlayerAttackCaster : BaseCaster
    {
        [Header("마법 슬롯")]
        [Tooltip("주 공격으로 사용할 마법의 데이터 에셋.")]
        [SerializeField] private MagicData _primaryAttackMagic;

        [Header("메인 카메라")]
        [Tooltip("마법을 시전할 방향을 정할 메인카메라")]
        private Camera _mainCamera;
        private void Awake() // Start 대신 Awake 사용 권장
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
        }

        /// <summary>
        /// 주 공격 마법을 시전함.
        /// </summary>
        public void PerformAttack()
        {
            if (_mainCamera == null)
            {
                Debug.LogError("메인 카메라가 할당되지 않았습니다!", this);
                return;
            }
            Vector3 fireDirection = _mainCamera.transform.forward;
            UseSkill(_primaryAttackMagic, fireDirection);
        }
    }
}