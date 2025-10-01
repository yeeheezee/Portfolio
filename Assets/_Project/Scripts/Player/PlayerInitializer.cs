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

        private void Awake()
        {
            // 필요한 모든 컴포넌트를 가져옵니다.
            _playerInput = GetComponent<PlayerInput>();
            _playerMovement = GetComponent<PlayerMovement>();
            _playerJump = GetComponent<PlayerJump>();
            _playerAttackCaster = GetComponent<PlayerAttackCaster>();

            //각 액션 발생 시 함수 호출되도록 구독
            _playerInput.actions["Move"].performed += context => _playerMovement.SetMoveInput(context.ReadValue<Vector2>());
            _playerInput.actions["Move"].canceled += context => _playerMovement.SetMoveInput(Vector2.zero);

            _playerInput.actions["Jump"].performed += context => _playerJump.PerformJump();

            _playerInput.actions["Fire"].performed += context => _playerAttackCaster.PerformAttack();
        }
    }
}
