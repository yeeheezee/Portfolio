// _Project/Scripts/Player/PlayerInitializer.cs
using UnityEngine;
using UnityEngine.InputSystem;


namespace WizardBrawl.Player
{
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerJump))]
    [RequireComponent(typeof(PlayerAttackCaster))]
    [RequireComponent(typeof(PlayerElementSlot))]
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
        private InputAction _moveAction;
        private InputAction _fireAction;
        private InputAction _jumpAction;
        private InputAction _parryAction;
        private InputAction _castDebuffAction;
        private InputAction _castCrowdControlAction;
        private InputAction _castUltimateAction;
        private InputAction _sprintAction;
        private InputAction _armInjectAction;

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
            _moveAction = FindAction("Move");
            _fireAction = FindAction("Fire");
            _jumpAction = FindAction("Jump");
            _parryAction = FindAction("Parry");
            _castDebuffAction = FindAction("CastDebuff");
            _castCrowdControlAction = FindAction("CastCrowdControl");
            _castUltimateAction = FindAction("CastUltimate");
            _sprintAction = FindAction("Sprint");
            _armInjectAction = FindAction("ArmInject");

            if (_manaParry == null)
            {
                Debug.LogError("PlayerInitializer에서 MagicParry를 찾을 수 없습니다.", this);
            }
        }

        private InputAction FindAction(string actionName)
        {
            InputAction action = _playerInput.actions.FindAction(actionName, throwIfNotFound: false);
            if (action == null)
            {
                Debug.LogWarning($"PlayerInitializer 입력 액션 누락: {actionName}", this);
            }

            return action;
        }

        private void OnEnable()
        {
            if (_moveAction != null)
            {
                _moveAction.performed += OnMovePerformed;
                _moveAction.canceled += OnMoveCanceled;
            }

            if (_fireAction != null)
            {
                _fireAction.performed += OnFirePerformed;
            }

            if (_jumpAction != null)
            {
                _jumpAction.performed += OnJumpPerformed;
            }

            if (_parryAction != null)
            {
                _parryAction.performed += OnParryPerformed;
            }

            if (_castDebuffAction != null)
            {
                _castDebuffAction.performed += OnCastDebuffPerformed;
            }

            if (_castCrowdControlAction != null)
            {
                _castCrowdControlAction.performed += OnCastCrowdControlPerformed;
            }

            if (_castUltimateAction != null)
            {
                _castUltimateAction.performed += OnCastUltimatePerformed;
            }

            if (_sprintAction != null)
            {
                _sprintAction.performed += OnSprintPerformed;
                _sprintAction.canceled += OnSprintCanceled;
            }

            if (_armInjectAction != null)
            {
                _armInjectAction.performed += OnArmInjectPerformed;
            }
        }

        private void OnDisable()
        {
            if (_moveAction != null)
            {
                _moveAction.performed -= OnMovePerformed;
                _moveAction.canceled -= OnMoveCanceled;
            }

            if (_fireAction != null)
            {
                _fireAction.performed -= OnFirePerformed;
            }

            if (_jumpAction != null)
            {
                _jumpAction.performed -= OnJumpPerformed;
            }

            if (_parryAction != null)
            {
                _parryAction.performed -= OnParryPerformed;
            }

            if (_castDebuffAction != null)
            {
                _castDebuffAction.performed -= OnCastDebuffPerformed;
            }

            if (_castCrowdControlAction != null)
            {
                _castCrowdControlAction.performed -= OnCastCrowdControlPerformed;
            }

            if (_castUltimateAction != null)
            {
                _castUltimateAction.performed -= OnCastUltimatePerformed;
            }

            if (_sprintAction != null)
            {
                _sprintAction.performed -= OnSprintPerformed;
                _sprintAction.canceled -= OnSprintCanceled;
            }

            if (_armInjectAction != null)
            {
                _armInjectAction.performed -= OnArmInjectPerformed;
            }
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            _playerMovement.SetMoveInput(context.ReadValue<Vector2>());
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            _playerMovement.SetMoveInput(Vector2.zero);
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            _playerJump.PerformJump();
        }

        private void OnFirePerformed(InputAction.CallbackContext context)
        {
            _playerAttackCaster.PerformAttack();
        }

        private void OnParryPerformed(InputAction.CallbackContext context)
        {
            if (_manaParry == null)
            {
                return;
            }

            if (_playerAttackCaster != null && _playerAttackCaster.IsTargeting)
            {
                Debug.Log("[Parry] blocked: targeting_in_progress");
                return;
            }

            _manaParry.AttemptParry();
        }

        private void OnCastDebuffPerformed(InputAction.CallbackContext context)
        {
            _playerAttackCaster.PerformDebuffCast();
        }

        private void OnCastCrowdControlPerformed(InputAction.CallbackContext context)
        {
            _playerAttackCaster.PerformCrowdControlCast();
        }

        private void OnCastUltimatePerformed(InputAction.CallbackContext context)
        {
            _playerAttackCaster.PerformUltimateCast();
        }

        private void OnSprintPerformed(InputAction.CallbackContext context)
        {
            _playerMovement.SetSprintPressed(true);
        }

        private void OnSprintCanceled(InputAction.CallbackContext context)
        {
            _playerMovement.SetSprintPressed(false);
        }

        private void OnArmInjectPerformed(InputAction.CallbackContext context)
        {
            _playerAttackCaster.ToggleInjectArm();
        }
    }
}
