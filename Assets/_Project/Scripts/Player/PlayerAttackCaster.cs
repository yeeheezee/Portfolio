using UnityEngine;
using UnityEngine.InputSystem;
using WizardBrawl.Core;
using WizardBrawl.Magic;
using WizardBrawl.Magic.Data;
using WizardBrawl.Magic.Data.SpellRecipe;
using WizardBrawl.Magic.SpellRecipe;
using WizardBrawl.Player.Services;

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

        [Tooltip("체인 대효과 테이블. (Stage + From + To) 키 기반")]
        [SerializeField] private ChainEffectTable _chainEffectTable;

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
            SetSlotMutationLocked(false);
            _injectionStateService.ResetState();
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
            Debug.Log("[Attack] blocked: use Q/E/R for casting");
        }

        public void PerformDebuffCast()
        {
            PerformSlotCast(CastSlotType.Q);
        }

        public void PerformCrowdControlCast()
        {
            PerformSlotCast(CastSlotType.E);
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
                bool ultimateCastSuccess = TryCastUltimate(selectedMagic, fireDirection, targetPoint);
                if (ultimateCastSuccess)
                {
                    NotifyCastPipeline(slot, impactType, selectedMagic, combo, targetPoint, hadInjectedElement, injectedElement);
                }

                return ultimateCastSuccess;
            }

            bool castSuccess = TryUseSkill(selectedMagic, fireDirection, targetPoint);
            if (castSuccess)
            {
                UpdatePatternStateOnCast(impactType, selectedMagic, hadInjectedElement);
                NotifyCastPipeline(slot, impactType, selectedMagic, combo, targetPoint, hadInjectedElement, injectedElement);
            }

            return castSuccess;
        }

        private bool TryCastUltimate(MagicData enhancedUltimate, Vector3 fireDirection, Vector3 targetPoint)
        {
            bool allowChainUltimate = ResolveChainUltimateAvailability();
            bool hasEnhancedState = _chainState == ChainState.CrowdControlReady && allowChainUltimate;
            if (hasEnhancedState && enhancedUltimate != null && CanUseSkillNow(enhancedUltimate))
            {
                if (TryUseSkill(enhancedUltimate, fireDirection, targetPoint))
                {
                    Debug.Log("[ElementCombo] pattern=CC->Ultimate stage=Success");
                    Debug.Log("[CastResult] CC->Ultimate enhanced cast");
                    _chainState = ChainState.None;
                    return true;
                }
            }

            MagicData fallbackUltimate = _baseUltimateMagic;
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
            
            bool fallbackSuccess = TryUseSkill(fallbackUltimate, fireDirection, targetPoint);
            if (fallbackSuccess)
            {
                Debug.Log("[ElementCombo] pattern=CC->Ultimate stage=Fallback");
                Debug.Log($"[CastResult] ultimate fallback to base: reason={fallbackReason}");
            }

            _chainState = ChainState.None;
            return fallbackSuccess;
        }

        private void UpdatePatternStateOnCast(SpellImpactType impactType, MagicData castedMagic, bool hadInjectedElement)
        {
            if (!hadInjectedElement)
            {
                _chainState = ChainState.None;
                Debug.Log("[ElementCombo] reset: no_injected_element");
                return;
            }

            if (impactType == SpellImpactType.Debuff || castedMagic is DebuffMagicData)
            {
                _chainState = ChainState.DebuffReady;
                Debug.Log("[ElementCombo] pattern=Debuff->CC stage=DebuffArmed");
                return;
            }

            if (impactType == SpellImpactType.CrowdControl || castedMagic is CrowdControlMagicData)
            {
                bool isDebuffToCcSuccess = _chainState == ChainState.DebuffReady;
                if (_chainState == ChainState.DebuffReady)
                {
                    Debug.Log("[ElementCombo] pattern=Debuff->CC stage=Success");
                }
                else
                {
                    Debug.Log("[ElementCombo] pattern=Debuff->CC stage=Miss");
                }

                if (isDebuffToCcSuccess && ShouldArmCrowdControlToUltimate())
                {
                    _chainState = ChainState.CrowdControlReady;
                    Debug.Log("[ElementCombo] pattern=CC->Ultimate stage=CcArmed");
                    Debug.Log("[ChainDecision] result=arm_cc_to_ultimate");
                }
                else
                {
                    if (isDebuffToCcSuccess)
                    {
                        _chainState = ChainState.None;
                        Debug.Log("[ChainDecision] result=consume_on_debuff_to_cc");
                    }
                    else
                    {
                        _chainState = ChainState.CrowdControlReady;
                        Debug.Log("[ElementCombo] pattern=CC->Ultimate stage=CcArmed");
                        Debug.Log("[ChainDecision] result=arm_cc_to_ultimate_by_cc_only");
                    }
                }
            }
        }

        private void HandleDebugElementInjectInput()
        {
            if (!_enableDebugElementInjectKeys || _elementSlot == null || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                _elementSlot.SaveFromParry(ElementType.R);
                Debug.Log("[DebugSlot] inject=R key=1");
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                _elementSlot.SaveFromParry(ElementType.B);
                Debug.Log("[DebugSlot] inject=B key=2");
            }

            if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                _elementSlot.SaveFromParry(ElementType.Y);
                Debug.Log("[DebugSlot] inject=Y key=3");
            }
        }

        private void PerformSlotCast(CastSlotType slot)
        {
            if (_isTargeting)
            {
                return;
            }

            bool hasInjectedElement = _injectionStateService.TryPrepareInject(_elementSlot, out ElementType injectedElement, out string blockedReason);
            if (!hasInjectedElement)
            {
                injectedElement = ElementType.None;
                Debug.Log($"[Inject] optional: {blockedReason} slot={slot}");
            }

            if (_mainCamera == null)
            {
                return;
            }

            if (!TrySelectSlotMagic(slot, out MagicData selectedMagic))
            {
                Debug.Log($"[DirectCast] blocked: reason=missing_magic slot={slot}");
                return;
            }

            SpellImpactType requestedImpactType = ResolveCastImpactTypeForMagic(slot, selectedMagic);

            ElementCombinationType combo = ElementCombinationType.None;
            if (hasInjectedElement)
            {
                selectedMagic = ResolveDirectCastMagicWithRecipe(requestedImpactType, injectedElement, selectedMagic, out combo);
            }
            else
            {
                _hasLastSpellRecipeResolution = false;
                _hasLastChainEffectEntry = false;
                _hasLastResolvedChainKey = false;
            }

            ResolveExecutionKinds(combo, selectedMagic);
            SpellImpactType resolvedImpactType = _resolvedImpactType;

            if (RequiresTargeting())
            {
                if (hasInjectedElement)
                {
                    _directChainStateService.BeginPending(resolvedImpactType, injectedElement);
                    _injectionStateService.MarkQueued(resolvedImpactType, injectedElement);
                }

                EnterTargeting(slot, selectedMagic, combo, _resolvedDeliveryType, _resolvedImpactType, hasInjectedElement, injectedElement);
                Debug.Log($"[DirectCast] queued slot={slot} impact={resolvedImpactType} combo={combo} injected={hasInjectedElement}");
                return;
            }

            Vector3 fallbackPoint = _mainCamera.transform.position + (_mainCamera.transform.forward * _targetingMaxDistance);
            bool success = TryCastSelectedMagic(slot, selectedMagic, combo, resolvedImpactType, fallbackPoint, hasInjectedElement, injectedElement);
            if (success)
            {
                if (hasInjectedElement)
                {
                    _injectionStateService.ConsumeImmediate(_elementSlot, resolvedImpactType, injectedElement);
                    _directChainStateService.RecordSuccess(resolvedImpactType, injectedElement);
                }
            }
            Debug.Log($"[DirectCast] cast slot={slot} impact={resolvedImpactType} combo={combo} injected={hasInjectedElement} success={success}");
        }

        private MagicData ResolveDirectCastMagicWithRecipe(
            SpellImpactType impactType,
            ElementType injectedElement,
            MagicData defaultMagic,
            out ElementCombinationType combo)
        {
            combo = ElementCombinationType.None;
            _hasLastSpellRecipeResolution = false;
            _hasLastChainEffectEntry = false;
            _hasLastResolvedChainKey = false;

            if (!_directChainStateService.TryBuildRecipeKeyForDirectCast(impactType, injectedElement, out SpellRecipeKey key, out combo))
            {
                return defaultMagic;
            }

            _lastResolvedChainKey = key;
            _hasLastResolvedChainKey = true;
            TryResolveChainEffect(key);

            if (_spellRecipeResolver == null)
            {
                return defaultMagic;
            }

            SpellRecipeResolution resolution = _spellRecipeResolver.Resolve(key, defaultMagic);
            _lastSpellRecipeResolution = resolution;
            _hasLastSpellRecipeResolution = true;
            return resolution.SelectedMagic == null ? defaultMagic : resolution.SelectedMagic;
        }

        private void TryResolveChainEffect(SpellRecipeKey key)
        {
            if (_chainEffectTable == null)
            {
                _hasLastChainEffectEntry = false;
                return;
            }

            ChainEffectKey chainKey = new ChainEffectKey(key.Stage, key.FromElement, key.ToElement);
            if (_chainEffectTable.TryGetEffect(chainKey, out ChainEffectEntry entry))
            {
                _lastChainEffectEntry = entry;
                _hasLastChainEffectEntry = true;
                Debug.Log($"[ChainEffect] hit: key={chainKey}");
            }
            else
            {
                _hasLastChainEffectEntry = false;
                Debug.Log($"[ChainEffect] missing: key={chainKey}");
            }
        }

        private bool TrySelectSlotMagic(CastSlotType slot, out MagicData selectedMagic)
        {
            selectedMagic = null;
            switch (slot)
            {
                case CastSlotType.Q:
                    selectedMagic = _baseDebuffMagic;
                    return selectedMagic != null;
                case CastSlotType.E:
                    selectedMagic = _baseCrowdControlMagic;
                    return selectedMagic != null;
                case CastSlotType.R:
                    selectedMagic = _baseUltimateMagic;
                    return selectedMagic != null;
                default:
                    return false;
            }
        }

        private static SpellImpactType ResolveCastImpactTypeForMagic(CastSlotType slot, MagicData selectedMagic)
        {
            SpellImpactType inferred = ResolveFallbackImpactType(ElementCombinationType.None, selectedMagic);
            if (inferred != SpellImpactType.None)
            {
                return inferred;
            }

            // Backward-compatible fallback for assets that do not define DefaultImpactType yet.
            switch (slot)
            {
                case CastSlotType.Q:
                    return SpellImpactType.Debuff;
                case CastSlotType.E:
                    return SpellImpactType.CrowdControl;
                case CastSlotType.R:
                    return SpellImpactType.Ultimate;
                default:
                    return SpellImpactType.None;
            }
        }

        private bool ResolveChainUltimateAvailability()
        {
            if (!_hasLastChainEffectEntry || _lastChainEffectEntry == null)
            {
                return false;
            }

            bool allowByChainEffect = _lastChainEffectEntry.AllowChainUltimate;
            Debug.Log($"[ChainDecision] allowChainUltimate={allowByChainEffect} source=chain_effect");
            return allowByChainEffect;
        }

        private bool ShouldArmCrowdControlToUltimate()
        {
            if (!_hasLastChainEffectEntry || _lastChainEffectEntry == null)
            {
                return false;
            }

            bool allowChainUltimate = _lastChainEffectEntry.AllowChainUltimate;
            Debug.Log($"[ChainDecision] allowChainUltimate={allowChainUltimate} source=chain_effect_d2c");
            return allowChainUltimate;
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
