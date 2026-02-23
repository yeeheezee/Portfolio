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
        [Header("커서 모드")]
        [Tooltip("전투 기본 모드에서 커서를 잠그고 숨김.")]
        [SerializeField] private bool _lockCursorInCombat = true;

        [Tooltip("ALT를 누르고 있는 동안 커서를 표시하고 잠금을 해제함.")]
        [SerializeField] private bool _holdAltToShowCursor = true;

        private PlayerInput _playerInput;
        private PlayerMovement _playerMovement;
        private PlayerJump _playerJump;
        private PlayerAttackCaster _playerAttackCaster;
        private MagicParry _manaParry;
        private bool _isCursorUiMode;

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
            _playerInput.actions["Move"].performed += context => _playerMovement.SetMoveInput(context.ReadValue<Vector2>());
            _playerInput.actions["Move"].canceled += context => _playerMovement.SetMoveInput(Vector2.zero);
            _playerInput.actions["Jump"].performed += context => _playerJump.PerformJump();
            _playerInput.actions["Fire"].performed += context => _playerAttackCaster.PerformTargetConfirm();
            _playerInput.actions["Parry"].performed += OnParryPerformed;
            RegisterOptionalCastAction("CastQ", OnCastQPerformed);
            RegisterOptionalCastAction("CastE", OnCastEPerformed);
            RegisterOptionalCastAction("CastR", OnCastRPerformed);

            ApplyCombatCursorMode();
        }

        private void Update()
        {
            if (!_holdAltToShowCursor)
            {
                return;
            }
            SyncCursorModeFromCurrentInput();
        }

        private void OnEnable()
        {
            // 재활성화 시 ALT 입력 상태를 기준으로 커서 모드를 복구함.
            SyncCursorModeFromCurrentInput();
        }

        private void OnDisable()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            _isCursorUiMode = true;
        }

        private void OnParryPerformed(InputAction.CallbackContext context)
        {
            if (_playerAttackCaster != null && _playerAttackCaster.IsTargeting)
            {
                Debug.Log("[Input] blocked: action=Parry reason=targeting");
                return;
            }

            if (_playerAttackCaster != null && _playerAttackCaster.IsCastingLocked)
            {
                Debug.Log("[Input] blocked: action=Parry reason=cast_locked");
                return;
            }

            _manaParry?.AttemptParry();
        }

        private void OnCastQPerformed(InputAction.CallbackContext context)
        {
            if (IsTargetingBlockedAction("Q"))
            {
                return;
            }

            _playerAttackCaster?.PerformCastQ();
        }

        private void OnCastEPerformed(InputAction.CallbackContext context)
        {
            if (IsTargetingBlockedAction("E"))
            {
                return;
            }

            _playerAttackCaster?.PerformCastE();
        }

        private void OnCastRPerformed(InputAction.CallbackContext context)
        {
            if (IsTargetingBlockedAction("R"))
            {
                return;
            }

            _playerAttackCaster?.PerformCastR();
        }

        private void ApplyCombatCursorMode()
        {
            if (_lockCursorInCombat)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            _isCursorUiMode = false;
        }

        private static void ApplyUiCursorMode()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void SyncCursorModeFromCurrentInput()
        {
            if (!_holdAltToShowCursor)
            {
                ApplyCombatCursorMode();
                return;
            }

            bool isAltPressed = Keyboard.current != null
                && (Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed);

            if (isAltPressed == _isCursorUiMode)
            {
                return;
            }

            if (isAltPressed)
            {
                ApplyUiCursorMode();
                _isCursorUiMode = true;
            }
            else
            {
                ApplyCombatCursorMode();
            }
        }

        private void RegisterOptionalCastAction(string actionName, System.Action<InputAction.CallbackContext> callback)
        {
            InputAction action = _playerInput.actions[actionName];
            if (action == null)
            {
                Debug.LogWarning($"[Input] missing action: {actionName}");
                return;
            }

            action.performed += callback;
        }

        private bool IsTargetingBlockedAction(string actionName)
        {
            if (_playerAttackCaster != null && _playerAttackCaster.IsTargeting)
            {
                Debug.Log($"[Input] blocked: action={actionName} reason=targeting");
                return true;
            }

            if (_playerAttackCaster != null && _playerAttackCaster.IsCastingLocked)
            {
                Debug.Log($"[Input] blocked: action={actionName} reason=cast_locked");
                return true;
            }

            return false;
        }
    }
}
