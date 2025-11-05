// _Project/Scripts/Player/PlayerInitializer.cs
using UnityEngine;
using UnityEngine.InputSystem;


namespace WizardBrawl.Player
{
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerJump))]
    [RequireComponent(typeof(PlayerAttackCaster))]
    /// <summary>
    /// Move 액션에서 동적 매개변수 함수에 대해 읽어오지 못하는 오류가 있어 코드 레벨에서 직접 연결할 수 있도록 스크립트 생성
    /// </summary>
    public class PlayerInitializer : MonoBehaviour
    {
        private PlayerInput _playerInput;
        private PlayerMovement _playerMovement;
        private PlayerJump _playerJump;
        private PlayerAttackCaster _playerAttackCaster;
        private MagicParry _manaParry;

        /// <summary>
        /// 컴포넌트들을 캐싱하고 Input System의 액션과 각 컴포넌트의 메서드를 연결(바인딩)함.
        /// </summary>
        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            _playerMovement = GetComponent<PlayerMovement>();
            _playerJump = GetComponent<PlayerJump>();
            _playerAttackCaster = GetComponent<PlayerAttackCaster>();
            _manaParry = GetComponentInChildren<MagicParry>();

            _playerInput.actions["Move"].performed += context => _playerMovement.SetMoveInput(context.ReadValue<Vector2>());
            _playerInput.actions["Move"].canceled += context => _playerMovement.SetMoveInput(Vector2.zero);
            _playerInput.actions["Jump"].performed += context => _playerJump.PerformJump();
            _playerInput.actions["Fire"].performed += context => _playerAttackCaster.PerformAttack();
            _playerInput.actions["Parry"].performed += context => _manaParry.AttemptParry();
        }
    }
}
