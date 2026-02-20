using UnityEngine;
using UnityEngine.InputSystem;
using WizardBrawl.Core;
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

        [Header("조합 마법 슬롯")]
        [Tooltip("RR 조합 성공 시 사용할 마법 데이터.")]
        [SerializeField] private MagicData _rrMagic;

        [Tooltip("BB 조합 성공 시 사용할 마법 데이터.")]
        [SerializeField] private MagicData _bbMagic;

        [Tooltip("YY 조합 성공 시 사용할 마법 데이터.")]
        [SerializeField] private MagicData _yyMagic;

        [Tooltip("RB 조합 성공 시 사용할 마법 데이터.")]
        [SerializeField] private MagicData _rbMagic;

        [Tooltip("RY 조합 성공 시 사용할 마법 데이터.")]
        [SerializeField] private MagicData _ryMagic;

        [Tooltip("BY 조합 성공 시 사용할 마법 데이터.")]
        [SerializeField] private MagicData _byMagic;

        [Header("궁 폴백")]
        [Tooltip("강화 궁 사용 불가 시 폴백할 기본 궁 마법 데이터.")]
        [SerializeField] private MagicData _baseUltimateMagic;

        [Header("메인 카메라")]
        [Tooltip("마법을 시전할 방향을 정할 메인카메라")]
        [SerializeField] private Camera _mainCamera;

        [Header("타겟팅 규칙")]
        [Tooltip("타겟팅 Raycast가 유효한 지면 레이어.")]
        [SerializeField] private LayerMask _targetGroundLayers = ~0;

        [Tooltip("타겟팅 최대 거리.")]
        [SerializeField] private float _targetingMaxDistance = 30f;

        [Header("타겟팅 인디케이터")]
        [Tooltip("타겟팅 위치를 표시할 인디케이터 Transform.")]
        [SerializeField] private Transform _targetingIndicator;

        [Tooltip("인디케이터 색상을 변경할 Renderer. 미지정 시 색상 변경은 건너뜀.")]
        [SerializeField] private Renderer _targetingIndicatorRenderer;

        [Tooltip("유효 지면을 가리킬 때 인디케이터 색상.")]
        [SerializeField] private Color _targetValidColor = Color.green;

        [Tooltip("무효 지점을 가리킬 때 인디케이터 색상.")]
        [SerializeField] private Color _targetInvalidColor = Color.red;

        [Tooltip("인디케이터가 바닥에 묻히지 않도록 올릴 높이 오프셋.")]
        [SerializeField] private float _targetingIndicatorHeightOffset = 0.05f;

        [Header("타겟팅 카메라 입력 잠금")]
        [Tooltip("타겟팅 중 비활성화할 카메라 입력 컴포넌트(예: Cinemachine Input Axis Controller).")]
        [SerializeField] private Behaviour _cameraLookInputController;

        [Header("타겟팅 이동 잠금")]
        [Tooltip("타겟팅 중 이동을 잠글 PlayerMovement. 미지정 시 같은 오브젝트에서 자동 탐색함.")]
        [SerializeField] private PlayerMovement _playerMovement;

        [Header("타겟팅 종료 후 카메라 복원")]
        [Tooltip("타겟팅 종료 직후 카메라 홱회전을 줄이기 위해 입력 복원을 지연함(초).")]
        [SerializeField] private float _cameraLookRestoreDelay = 0.08f;

        private readonly ElementCombinationResolver _combinationResolver = new ElementCombinationResolver();
        private PlayerElementSlot _elementSlot;
        private bool _isTargeting;
        private MagicData _pendingMagic;
        private ElementCombinationType _pendingCombination = ElementCombinationType.None;
        private Vector3 _previewTargetPoint;
        private bool _hasPreviewTargetPoint;
        private bool _isCameraLookRestorePending;
        private float _cameraLookRestoreAtTime;
        private ChainState _chainState = ChainState.None;

        private enum ChainState
        {
            None,
            DebuffReady,
            CrowdControlReady
        }

        protected override void Awake()
        {
            base.Awake();
            _elementSlot = GetComponent<PlayerElementSlot>();
            if (_playerMovement == null)
            {
                _playerMovement = GetComponent<PlayerMovement>();
            }

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
            SetTargetingIndicatorVisible(false);
        }

        private void OnDisable()
        {
            // 비활성화 시 카메라 입력이 잠긴 채 남지 않도록 복원함.
            SetCameraLookInputEnabled(true);
            SetPlayerMovementEnabled(true);
        }

        private void Update()
        {
            if (!_isTargeting)
            {
                TryRestoreCameraLookInput();
                return;
            }

            UpdateTargetingPreview();

            bool isCancelPressed = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
            bool isEscPressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            if (isCancelPressed || isEscPressed)
            {
                CancelTargeting("cancel_input");
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

            if (_isTargeting)
            {
                ConfirmTargetingCast();
                return;
            }

            MagicData selectedMagic = ResolveSelectedMagic(out ElementCombinationType combo);
            if (RequiresTargeting(selectedMagic, combo))
            {
                EnterTargeting(selectedMagic, combo);
                return;
            }

            TryCastSelectedMagic(selectedMagic, combo, _mainCamera.transform.position + (_mainCamera.transform.forward * _targetingMaxDistance));
        }

        private MagicData ResolveSelectedMagic(out ElementCombinationType combo)
        {
            combo = ElementCombinationType.None;
            MagicData selectedMagic = _primaryAttackMagic;

            if (_elementSlot != null && _combinationResolver.TryResolve(_elementSlot.CurrentState, out combo))
            {
                selectedMagic = SelectMagicByCombination(combo);
                Debug.Log($"[ElementCombo] success: {combo}");
            }
            else
            {
                Debug.Log("[ElementCombo] fail: fallback to primary");
            }

            return selectedMagic == null ? _primaryAttackMagic : selectedMagic;
        }

        private bool RequiresTargeting(MagicData selectedMagic, ElementCombinationType combo)
        {
            if (selectedMagic is CrowdControlMagicData)
            {
                return true;
            }

            return IsUltimateCombination(combo);
        }

        private static bool IsUltimateCombination(ElementCombinationType combo)
        {
            return combo == ElementCombinationType.YY
                || combo == ElementCombinationType.RY
                || combo == ElementCombinationType.BY;
        }

        private void EnterTargeting(MagicData selectedMagic, ElementCombinationType combo)
        {
            _isTargeting = true;
            _pendingMagic = selectedMagic;
            _pendingCombination = combo;
            _hasPreviewTargetPoint = false;
            SetTargetingIndicatorVisible(true);
            _isCameraLookRestorePending = false;
            SetCameraLookInputEnabled(false);
            SetPlayerMovementEnabled(false);
            UpdateTargetingPreview();
            Debug.Log($"[Targeting] enter: magic={selectedMagic?.MagicName ?? "None"}, combo={combo}");
        }

        private void ConfirmTargetingCast()
        {
            if (!_hasPreviewTargetPoint)
            {
                Debug.Log("[Targeting] blocked: invalid ground point");
                return;
            }

            Vector3 cursorPoint = _previewTargetPoint;

            if (TryCastSelectedMagic(_pendingMagic, _pendingCombination, cursorPoint))
            {
                bool hasCurrentCursorPoint = TryGetTargetPoint(out Vector3 currentCursorPoint);
                float targetingDelta = hasCurrentCursorPoint
                    ? Vector3.Distance(currentCursorPoint, cursorPoint)
                    : 0f;
                Debug.Log($"[Targeting] delta: cursorNow={(hasCurrentCursorPoint ? currentCursorPoint.ToString() : "N/A")} cast={cursorPoint} value={targetingDelta:F3}");
                Debug.Log("[Targeting] confirm: cast success");
                _isTargeting = false;
                _pendingMagic = null;
                _pendingCombination = ElementCombinationType.None;
                _hasPreviewTargetPoint = false;
                SetTargetingIndicatorVisible(false);
                ScheduleCameraLookRestore();
                SetPlayerMovementEnabled(true);
            }
            else
            {
                Debug.Log("[Targeting] confirm: cast failed");
                CancelTargeting("cast_failed");
            }
        }

        private void CancelTargeting(string reason)
        {
            Debug.Log($"[Targeting] cancel: reason={reason}");
            _isTargeting = false;
            _pendingMagic = null;
            _pendingCombination = ElementCombinationType.None;
            _hasPreviewTargetPoint = false;
            SetTargetingIndicatorVisible(false);
            ScheduleCameraLookRestore();
            SetPlayerMovementEnabled(true);
        }

        private bool TryCastSelectedMagic(MagicData selectedMagic, ElementCombinationType combo, Vector3 targetPoint)
        {
            Vector3 fireDirection = BuildFireDirection(targetPoint);
            if (IsUltimateCombination(combo))
            {
                return TryCastUltimate(selectedMagic, fireDirection);
            }

            bool castSuccess = TryUseSkill(selectedMagic, fireDirection);
            if (castSuccess)
            {
                UpdatePatternStateOnCast(selectedMagic);
            }

            return castSuccess;
        }

        private bool TryCastUltimate(MagicData enhancedUltimate, Vector3 fireDirection)
        {
            bool hasEnhancedState = _chainState == ChainState.CrowdControlReady;
            if (hasEnhancedState && enhancedUltimate != null && CanUseSkillNow(enhancedUltimate))
            {
                if (TryUseSkill(enhancedUltimate, fireDirection))
                {
                    Debug.Log("[ElementCombo] pattern=CC->Ultimate stage=Success");
                    Debug.Log("[CastResult] CC->Ultimate enhanced cast");
                    _chainState = ChainState.None;
                    return true;
                }
            }

            MagicData fallbackUltimate = _baseUltimateMagic != null ? _baseUltimateMagic : _primaryAttackMagic;
            if (fallbackUltimate == null)
            {
                Debug.LogWarning("[CastResult] ultimate fallback failed: no base ultimate assigned");
                _chainState = ChainState.None;
                return false;
            }

            string fallbackReason = hasEnhancedState ? "enhanced_unavailable_resource" : "enhanced_unavailable_state";
            if (!CanUseSkillNow(fallbackUltimate))
            {
                Debug.Log($"[CastResult] ultimate fallback blocked: reason={fallbackReason}");
                _chainState = ChainState.None;
                return false;
            }
            
            bool fallbackSuccess = TryUseSkill(fallbackUltimate, fireDirection);
            if (fallbackSuccess)
            {
                Debug.Log("[ElementCombo] pattern=CC->Ultimate stage=Fallback");
                Debug.Log($"[CastResult] ultimate fallback to base: reason={fallbackReason}");
            }

            _chainState = ChainState.None;
            return fallbackSuccess;
        }

        private void UpdatePatternStateOnCast(MagicData castedMagic)
        {
            if (castedMagic is DebuffMagicData)
            {
                _chainState = ChainState.DebuffReady;
                Debug.Log("[ElementCombo] pattern=Debuff->CC stage=DebuffArmed");
                return;
            }

            if (castedMagic is CrowdControlMagicData)
            {
                if (_chainState == ChainState.DebuffReady)
                {
                    Debug.Log("[ElementCombo] pattern=Debuff->CC stage=Success");
                }
                else
                {
                    Debug.Log("[ElementCombo] pattern=Debuff->CC stage=Miss");
                }

                _chainState = ChainState.CrowdControlReady;
                Debug.Log("[ElementCombo] pattern=CC->Ultimate stage=CcArmed");
            }
        }

        private bool TryGetTargetPoint(out Vector3 targetPoint)
        {
            targetPoint = Vector3.zero;
            if (_mainCamera == null)
            {
                return false;
            }

            if (Mouse.current == null)
            {
                return false;
            }

            Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, _targetingMaxDistance, _targetGroundLayers))
            {
                targetPoint = hit.point;
                return true;
            }

            return false;
        }

        private void UpdateTargetingPreview()
        {
            if (TryGetTargetPoint(out Vector3 targetPoint))
            {
                _previewTargetPoint = targetPoint;
                _hasPreviewTargetPoint = true;
                SetTargetingIndicatorState(targetPoint, true);
                return;
            }

            _hasPreviewTargetPoint = false;
            SetTargetingIndicatorState(_previewTargetPoint, false);
        }

        private void SetTargetingIndicatorVisible(bool visible)
        {
            if (_targetingIndicator != null)
            {
                _targetingIndicator.gameObject.SetActive(visible);
            }
        }

        private void SetTargetingIndicatorState(Vector3 point, bool isValid)
        {
            if (_targetingIndicator != null)
            {
                _targetingIndicator.position = point + Vector3.up * _targetingIndicatorHeightOffset;
            }

            if (_targetingIndicatorRenderer != null
                && _targetingIndicatorRenderer.material != null
                && _targetingIndicatorRenderer.material.HasProperty("_Color"))
            {
                _targetingIndicatorRenderer.material.color = isValid ? _targetValidColor : _targetInvalidColor;
            }
        }

        private void SetCameraLookInputEnabled(bool enabled)
        {
            if (_cameraLookInputController != null)
            {
                _cameraLookInputController.enabled = enabled;
            }
        }

        private void SetPlayerMovementEnabled(bool enabled)
        {
            if (_playerMovement != null)
            {
                _playerMovement.CanMove = enabled;
            }
        }

        private void ScheduleCameraLookRestore()
        {
            _isCameraLookRestorePending = true;
            _cameraLookRestoreAtTime = Time.time + Mathf.Max(0f, _cameraLookRestoreDelay);
            SetCameraLookInputEnabled(false);
        }

        private void TryRestoreCameraLookInput()
        {
            if (!_isCameraLookRestorePending)
            {
                return;
            }

            if (Time.time < _cameraLookRestoreAtTime)
            {
                return;
            }

            _isCameraLookRestorePending = false;
            SetCameraLookInputEnabled(true);
        }

        private Vector3 BuildFireDirection(Vector3 targetPoint)
        {
            Vector3 origin = MagicSpawnPoint != null ? MagicSpawnPoint.position : transform.position;
            Vector3 fireDirection = targetPoint - origin;
            if (fireDirection.sqrMagnitude < 0.0001f)
            {
                return _mainCamera.transform.forward;
            }

            return fireDirection.normalized;
        }

        private MagicData SelectMagicByCombination(ElementCombinationType combinationType)
        {
            MagicData magic = combinationType switch
            {
                ElementCombinationType.RR => _rrMagic,
                ElementCombinationType.BB => _bbMagic,
                ElementCombinationType.YY => _yyMagic,
                ElementCombinationType.RB => _rbMagic,
                ElementCombinationType.RY => _ryMagic,
                ElementCombinationType.BY => _byMagic,
                _ => _primaryAttackMagic
            };

            return magic == null ? _primaryAttackMagic : magic;
        }
    }
}
