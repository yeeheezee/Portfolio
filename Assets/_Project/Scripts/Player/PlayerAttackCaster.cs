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
        
        [Tooltip("Q 슬롯 입력 시 사용할 마법 데이터.")]
        [SerializeField] private MagicData _qSlotMagic;

        [Tooltip("E 슬롯 입력 시 사용할 마법 데이터.")]
        [SerializeField] private MagicData _eSlotMagic;

        [Tooltip("R 슬롯 입력 시 사용할 마법 데이터.")]
        [SerializeField] private MagicData _rSlotMagic;

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

        [Header("타겟팅 인디케이터 반경 동기화")]
        [Tooltip("인디케이터 원본 크기 대비 지름 배율.")]
        [SerializeField] private float _targetingIndicatorScaleMultiplier = 1f;

        [Tooltip("반경 정보를 읽지 못했을 때 사용할 기본 반경.")]
        [SerializeField] private float _defaultTargetingIndicatorRadius = 1.5f;

        [Tooltip("인디케이터 지름 최소값.")]
        [SerializeField] private float _targetingIndicatorMinDiameter = 1f;

        [Header("타겟팅 이동 참조")]
        [Tooltip("타겟팅 상태를 참조할 PlayerMovement. 미지정 시 같은 오브젝트에서 자동 탐색함.")]
        [SerializeField] private PlayerMovement _playerMovement;

        [Header("캐스팅 타이밍")]
        [Tooltip("마법 데이터에 캐스팅 시간이 없을 때 사용할 선딜 기본값(초).")]
        [SerializeField] private float _castWindupTime = 0.12f;

        [Tooltip("마법 데이터에 캐스팅 시간이 없을 때 사용할 후딜 기본값(초).")]
        [SerializeField] private float _castRecoveryTime = 0.18f;

        private bool _isTargeting;
        private MagicData _pendingMagic;
        private bool _pendingUsesUltimateFlow;
        private Vector3 _previewTargetPoint;
        private bool _hasPreviewTargetPoint;
        private Vector3 _targetingIndicatorBaseScale = Vector3.one;
        private ChainState _chainState = ChainState.None;
        private readonly CastTimingService _castTimingService = new CastTimingService();

        private enum ChainState
        {
            None,
            DebuffReady,
            CrowdControlReady
        }

        public bool IsTargeting => _isTargeting;
        public bool IsCastingLocked => _castTimingService.IsLocked;

        protected override void Awake()
        {
            base.Awake();
            if (_playerMovement == null)
            {
                _playerMovement = GetComponent<PlayerMovement>();
            }

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            if (_targetingIndicator != null)
            {
                _targetingIndicatorBaseScale = _targetingIndicator.localScale;
            }

            _castTimingService.Initialize(this);
            SetTargetingIndicatorVisible(false);
        }

        private void OnDisable()
        {
            _castTimingService.Cancel("disable");
            SetPlayerMovementEnabled(true);
        }

        private void Update()
        {
            if (!_isTargeting) return;

            UpdateTargetingPreview();

            bool isCancelPressed = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
            bool isEscPressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            if (isCancelPressed || isEscPressed)
            {
                CancelTargeting("cancel_input");
            }
        }

        public void PerformCastQ()
        {
            TryStartSlotCast(_qSlotMagic != null ? _qSlotMagic : _primaryAttackMagic, "Q");
        }

        public void PerformCastE()
        {
            TryStartSlotCast(_eSlotMagic != null ? _eSlotMagic : _primaryAttackMagic, "E");
        }

        public void PerformCastR()
        {
            TryStartSlotCast(_rSlotMagic != null ? _rSlotMagic : _primaryAttackMagic, "R");
        }

        public void PerformTargetConfirm()
        {
            if (_mainCamera == null)
            {
                Debug.LogError("메인 카메라가 할당되지 않았습니다!", this);
                return;
            }

            if (!_isTargeting)
            {
                return;
            }

            ConfirmTargetingCast();
        }

        [System.Obsolete("Use PerformCastQ/PerformCastE/PerformCastR and PerformTargetConfirm.")]
        public void PerformAttack()
        {
            PerformCastQ();
        }

        private void TryStartSlotCast(MagicData selectedMagic, string slotName)
        {
            if (_mainCamera == null)
            {
                Debug.LogError("메인 카메라가 할당되지 않았습니다!", this);
                return;
            }

            if (IsCastingLocked)
            {
                Debug.Log($"[CastState] blocked: slot={slotName}, state={_castTimingService.State}");
                return;
            }

            if (_isTargeting)
            {
                Debug.Log($"[Input] blocked: action={slotName} reason=targeting");
                return;
            }

            if (selectedMagic == null)
            {
                Debug.LogWarning($"[CastResult] blocked: slot={slotName} reason=magic_not_assigned");
                return;
            }

            if (RequiresTargeting(selectedMagic))
            {
                EnterTargeting(selectedMagic, slotName);
                return;
            }

            Vector3 fallbackPoint = _mainCamera.transform.position + (_mainCamera.transform.forward * _targetingMaxDistance);
            StartCastSequence(selectedMagic, selectedMagic.UseUltimateFlow, fallbackPoint, $"slot={slotName}");
        }

        private static bool RequiresTargeting(MagicData selectedMagic)
        {
            return selectedMagic != null && selectedMagic.CastMode == MagicCastMode.Targeted;
        }

        private void EnterTargeting(MagicData selectedMagic, string slotName)
        {
            _isTargeting = true;
            _pendingMagic = selectedMagic;
            _pendingUsesUltimateFlow = selectedMagic != null && selectedMagic.UseUltimateFlow;
            _hasPreviewTargetPoint = false;
            SyncTargetingIndicatorScale(selectedMagic);
            SetTargetingIndicatorVisible(true);
            UpdateTargetingPreview();
            Debug.Log($"[Targeting] enter: slot={slotName}, magic={selectedMagic?.MagicName ?? "None"}");
        }

        private void ConfirmTargetingCast()
        {
            if (!_hasPreviewTargetPoint)
            {
                Debug.Log("[Targeting] blocked: invalid ground point");
                return;
            }

            Vector3 cursorPoint = _previewTargetPoint;
            bool castStarted = StartCastSequence(_pendingMagic, _pendingUsesUltimateFlow, cursorPoint, "target_confirm");
            if (castStarted)
            {
                bool hasCurrentCursorPoint = TryGetTargetPoint(out Vector3 currentCursorPoint);
                float targetingDelta = hasCurrentCursorPoint
                    ? Vector3.Distance(currentCursorPoint, cursorPoint)
                    : 0f;
                Debug.Log($"[Targeting] delta: cursorNow={(hasCurrentCursorPoint ? currentCursorPoint.ToString() : "N/A")} cast={cursorPoint} value={targetingDelta:F3}");
                Debug.Log("[Targeting] confirm: cast queued");
                ExitTargetingState();
            }
            else
            {
                Debug.Log("[Targeting] confirm: cast queue failed");
                CancelTargeting("cast_failed");
            }
        }

        private void CancelTargeting(string reason)
        {
            Debug.Log($"[Targeting] cancel: reason={reason}");
            ExitTargetingState();
        }

        private void ExitTargetingState()
        {
            _isTargeting = false;
            _pendingMagic = null;
            _pendingUsesUltimateFlow = false;
            _hasPreviewTargetPoint = false;
            SetTargetingIndicatorVisible(false);
        }

        private bool StartCastSequence(MagicData selectedMagic, bool usesUltimateFlow, Vector3 targetPoint, string source)
        {
            if (selectedMagic == null)
            {
                Debug.LogWarning($"[CastState] blocked: source={source}, reason=null_magic");
                return false;
            }

            if (IsCastingLocked)
            {
                Debug.Log($"[CastState] blocked: source={source}, state={_castTimingService.State}");
                return false;
            }

            if (!CanUseSkillNow(selectedMagic))
            {
                Debug.Log($"[CastState] blocked: source={source}, magic={selectedMagic.MagicName}, reason=resource_or_cooldown");
                return false;
            }

            return _castTimingService.TryBegin(
                ResolveCastWindupTime(selectedMagic),
                ResolveCastRecoveryTime(selectedMagic),
                () => TryCastSelectedMagic(selectedMagic, usesUltimateFlow, targetPoint),
                source,
                selectedMagic.MagicName);
        }

        private bool TryCastSelectedMagic(MagicData selectedMagic, bool usesUltimateFlow, Vector3 targetPoint)
        {
            Vector3 fireDirection = BuildFireDirection(targetPoint);
            if (usesUltimateFlow)
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

            Ray ray = _mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
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

        private void SyncTargetingIndicatorScale(MagicData selectedMagic)
        {
            if (_targetingIndicator == null)
            {
                return;
            }

            float radius = ResolveTargetingRadius(selectedMagic);
            float diameter = Mathf.Max(_targetingIndicatorMinDiameter, radius * 2f * Mathf.Max(0.01f, _targetingIndicatorScaleMultiplier));
            _targetingIndicator.localScale = new Vector3(_targetingIndicatorBaseScale.x * diameter, _targetingIndicatorBaseScale.y, _targetingIndicatorBaseScale.z * diameter);
        }

        private float ResolveTargetingRadius(MagicData selectedMagic)
        {
            if (selectedMagic is CrowdControlMagicData crowdControlMagic)
            {
                return Mathf.Max(0.1f, crowdControlMagic.Radius);
            }

            return _defaultTargetingIndicatorRadius;
        }

        private float ResolveCastWindupTime(MagicData selectedMagic)
        {
            if (selectedMagic == null)
            {
                return Mathf.Max(0f, _castWindupTime);
            }

            return Mathf.Max(0f, selectedMagic.CastWindupTime);
        }

        private float ResolveCastRecoveryTime(MagicData selectedMagic)
        {
            if (selectedMagic == null)
            {
                return Mathf.Max(0f, _castRecoveryTime);
            }

            return Mathf.Max(0f, selectedMagic.CastRecoveryTime);
        }

        private void SetPlayerMovementEnabled(bool enabled)
        {
            if (_playerMovement != null)
            {
                _playerMovement.CanMove = enabled;
            }
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

    }
}
