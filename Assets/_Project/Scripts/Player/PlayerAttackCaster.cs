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
        [Header("기본 스킬 슬롯(Q/E/R)")]
        [Tooltip("Q 키 슬롯 기본 마법 데이터.")]
        [SerializeField] private MagicData _baseDebuffMagic;

        [Tooltip("E 키 슬롯 기본 마법 데이터.")]
        [SerializeField] private MagicData _baseCrowdControlMagic;

        [Tooltip("R 키 슬롯 기본 마법 데이터.")]
        [SerializeField] private MagicData _baseUltimateMagic;

        [Header("Spell Recipe")]
        [Tooltip("체인 레시피 매핑 테이블. 지정 시 조합별 레시피를 우선 적용함.")]
        [SerializeField] private SpellRecipeTable _spellRecipeTable;

        [Tooltip("주입 소효과 테이블. (ImpactType + Element) 키 기반")]
        [SerializeField] private InjectionEffectTable _injectionEffectTable;

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

        [Header("타겟팅 카메라 입력 잠금")]
        [Tooltip("타겟팅 중 비활성화할 카메라 입력 컴포넌트(예: Cinemachine Input Axis Controller).")]
        [SerializeField] private Behaviour _cameraLookInputController;

        [Header("타겟팅 이동 잠금")]
        [Tooltip("타겟팅 중 이동을 잠글 PlayerMovement. 미지정 시 같은 오브젝트에서 자동 탐색함.")]
        [SerializeField] private PlayerMovement _playerMovement;

        [Header("타겟팅 종료 후 카메라 복원")]
        [Tooltip("타겟팅 종료 직후 카메라 홱회전을 줄이기 위해 입력 복원을 지연함(초).")]
        [SerializeField] private float _cameraLookRestoreDelay = 0.08f;

        [Header("Debug")]
        [Tooltip("true면 1/2/3 키로 슬롯에 R/B/Y를 임시 주입함.")]
        [SerializeField] private bool _enableDebugElementInjectKeys = true;

        private PlayerElementSlot _elementSlot;
        private bool _isTargeting;
        private PendingCastState _pendingCast;
        private Vector3 _previewTargetPoint;
        private bool _hasPreviewTargetPoint;
        private bool _isCameraLookRestorePending;
        private float _cameraLookRestoreAtTime;
        private ChainState _chainState = ChainState.None;
        private SpellRecipeResolver _spellRecipeResolver;
        private SpellRecipeResolution _lastSpellRecipeResolution;
        private bool _hasLastSpellRecipeResolution;
        private ChainEffectEntry _lastChainEffectEntry;
        private bool _hasLastChainEffectEntry;
        private SpellRecipeKey _lastResolvedChainKey;
        private bool _hasLastResolvedChainKey;
        private SpellDeliveryType _resolvedDeliveryType = SpellDeliveryType.Auto;
        private SpellImpactType _resolvedImpactType = SpellImpactType.Auto;
        private readonly InjectionStateService _injectionStateService = new InjectionStateService();
        private readonly DirectChainStateService _directChainStateService = new DirectChainStateService();
        private CastPipeline _castPipeline;

        public bool IsTargeting => _isTargeting;

        private enum ChainState
        {
            None,
            DebuffReady,
            CrowdControlReady
        }

        private struct PendingCastState
        {
            public CastSlotType Slot;
            public MagicData Magic;
            public ElementCombinationType Combination;
            public SpellDeliveryType DeliveryType;
            public SpellImpactType ImpactType;
            public bool HadInjectedElement;
            public ElementType InjectedElement;

            public static PendingCastState Empty => new PendingCastState
            {
                Slot = CastSlotType.None,
                Magic = null,
                Combination = ElementCombinationType.None,
                DeliveryType = SpellDeliveryType.Auto,
                ImpactType = SpellImpactType.Auto,
                HadInjectedElement = false,
                InjectedElement = ElementType.None
            };
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

            _spellRecipeResolver = _spellRecipeTable == null
                ? null
                : new SpellRecipeResolver(_spellRecipeTable);
            _castPipeline = new CastPipeline(
                new TableInjectionEffectResolver(_injectionEffectTable),
                new TableChainEffectResolver(_chainEffectTable),
                new CompositeCastPresentationSink(
                    new ChainRuntimeApplierSink(),
                    new ChainEffectEventPublisherSink(),
                    new DebugCastPresentationSink()));

            SetTargetingIndicatorVisible(false);
        }

        private void OnDisable()
        {
            // 비활성화 시 카메라 입력이 잠긴 채 남지 않도록 복원함.
            _isTargeting = false;
            _pendingCast = PendingCastState.Empty;
            _hasPreviewTargetPoint = false;
            _isCameraLookRestorePending = false;
            _cameraLookRestoreAtTime = 0f;
            _directChainStateService.Reset();
            _hasLastSpellRecipeResolution = false;
            _hasLastChainEffectEntry = false;
            _hasLastResolvedChainKey = false;
            SetTargetingIndicatorVisible(false);
            SetCameraLookInputEnabled(true);
            SetPlayerMovementEnabled(true);
            SetSlotMutationLocked(false);
            _injectionStateService.ResetState();
        }

        private void Update()
        {
            HandleDebugElementInjectInput();

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

        public void PerformUltimateCast()
        {
            PerformSlotCast(CastSlotType.R);
        }

        public void ToggleInjectArm()
        {
            if (_isTargeting)
            {
                Debug.Log("[Inject] blocked: targeting_in_progress");
                return;
            }

            bool isArmed = _injectionStateService.ToggleArm();
            Debug.Log($"[Inject] armed={isArmed}");
        }

        private void ResolveExecutionKinds(ElementCombinationType combo, MagicData selectedMagic)
        {
            SpellDeliveryType fallbackDelivery = ResolveFallbackDeliveryType(combo, selectedMagic);
            SpellImpactType fallbackImpact = ResolveFallbackImpactType(combo, selectedMagic);

            if (_hasLastSpellRecipeResolution && _lastSpellRecipeResolution.HasRecipe && _lastSpellRecipeResolution.Entry != null)
            {
                SpellRecipeEntry entry = _lastSpellRecipeResolution.Entry;
                _resolvedDeliveryType = entry.DeliveryType == SpellDeliveryType.Auto ? fallbackDelivery : entry.DeliveryType;
                _resolvedImpactType = entry.ImpactType == SpellImpactType.Auto ? fallbackImpact : entry.ImpactType;
                Debug.Log($"[SpellRecipe] execution delivery={_resolvedDeliveryType} impact={_resolvedImpactType} source=recipe");
                return;
            }

            _resolvedDeliveryType = fallbackDelivery;
            _resolvedImpactType = fallbackImpact;
            Debug.Log($"[SpellRecipe] execution delivery={_resolvedDeliveryType} impact={_resolvedImpactType} source=fallback");
        }

        private static SpellDeliveryType ResolveFallbackDeliveryType(ElementCombinationType combo, MagicData selectedMagic)
        {
            if (selectedMagic != null && selectedMagic.DefaultDeliveryType != SpellDeliveryType.Auto)
            {
                return selectedMagic.DefaultDeliveryType;
            }

            if (selectedMagic is DebuffMagicData || selectedMagic is ProjectileMagicData)
            {
                return SpellDeliveryType.Projectile;
            }

            if (selectedMagic is CrowdControlMagicData)
            {
                return SpellDeliveryType.Area;
            }

            return SpellDeliveryType.Projectile;
        }

        private static SpellImpactType ResolveFallbackImpactType(ElementCombinationType combo, MagicData selectedMagic)
        {
            if (selectedMagic != null && selectedMagic.DefaultImpactType != SpellImpactType.Auto)
            {
                return selectedMagic.DefaultImpactType;
            }

            if (selectedMagic is DebuffMagicData)
            {
                return SpellImpactType.Debuff;
            }

            if (selectedMagic is CrowdControlMagicData)
            {
                return SpellImpactType.CrowdControl;
            }

            return SpellImpactType.None;
        }

        private bool RequiresTargeting()
        {
            return _resolvedDeliveryType == SpellDeliveryType.Area
                || _resolvedDeliveryType == SpellDeliveryType.Meteor;
        }

        private void EnterTargeting(
            CastSlotType slot,
            MagicData selectedMagic,
            ElementCombinationType combo,
            SpellDeliveryType deliveryType,
            SpellImpactType impactType,
            bool hadInjectedElement,
            ElementType injectedElement)
        {
            _isTargeting = true;
            _pendingCast = new PendingCastState
            {
                Slot = slot,
                Magic = selectedMagic,
                Combination = combo,
                DeliveryType = deliveryType,
                ImpactType = impactType,
                HadInjectedElement = hadInjectedElement,
                InjectedElement = injectedElement
            };
            _hasPreviewTargetPoint = false;
            SetTargetingIndicatorVisible(true);
            _isCameraLookRestorePending = false;
            SetCameraLookInputEnabled(false);
            SetPlayerMovementEnabled(false);
            SetSlotMutationLocked(true);
            UpdateTargetingPreview();
            Debug.Log($"[Targeting] enter: magic={selectedMagic?.MagicName ?? "None"}, combo={combo}, delivery={deliveryType}, impact={impactType}");
        }

        private void ConfirmTargetingCast()
        {
            if (!_hasPreviewTargetPoint)
            {
                Debug.Log("[Targeting] blocked: invalid ground point");
                return;
            }

            Vector3 cursorPoint = _previewTargetPoint;

            if (TryCastSelectedMagic(
                _pendingCast.Slot,
                _pendingCast.Magic,
                _pendingCast.Combination,
                _pendingCast.ImpactType,
                cursorPoint,
                _pendingCast.HadInjectedElement,
                _pendingCast.InjectedElement))
            {
                bool hasCurrentCursorPoint = TryGetTargetPoint(out Vector3 currentCursorPoint);
                float targetingDelta = hasCurrentCursorPoint
                    ? Vector3.Distance(currentCursorPoint, cursorPoint)
                    : 0f;
                Debug.Log($"[Targeting] delta: cursorNow={(hasCurrentCursorPoint ? currentCursorPoint.ToString() : "N/A")} cast={cursorPoint} value={targetingDelta:F3}");
                Debug.Log("[Targeting] confirm: cast success");
                _isTargeting = false;
                _pendingCast = PendingCastState.Empty;
                _injectionStateService.ConfirmQueued(_elementSlot);
                _directChainStateService.ConfirmPendingAndRecordSuccess();
                _hasPreviewTargetPoint = false;
                SetTargetingIndicatorVisible(false);
                ScheduleCameraLookRestore();
                SetPlayerMovementEnabled(true);
                SetSlotMutationLocked(false);
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
            _pendingCast = PendingCastState.Empty;
            if (_injectionStateService.CancelQueued())
            {
                Debug.Log("[Inject] rearmed: targeting cancelled before consume");
            }
            _directChainStateService.ClearPending();
            _hasPreviewTargetPoint = false;
            SetTargetingIndicatorVisible(false);
            ScheduleCameraLookRestore();
            SetPlayerMovementEnabled(true);
            SetSlotMutationLocked(false);
        }

        private bool TryCastSelectedMagic(
            CastSlotType slot,
            MagicData selectedMagic,
            ElementCombinationType combo,
            SpellImpactType impactType,
            Vector3 targetPoint,
            bool hadInjectedElement,
            ElementType injectedElement)
        {
            Vector3 fireDirection = BuildFireDirection(targetPoint);
            if (impactType == SpellImpactType.Ultimate)
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

        private void SetSlotMutationLocked(bool isLocked)
        {
            if (_elementSlot != null)
            {
                _elementSlot.SetMutationLocked(isLocked);
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

        private static void ResolveEffectTargeting(MagicData selectedMagic, out float radius, out int layerMask)
        {
            radius = 0f;
            layerMask = 0;

            if (selectedMagic is CrowdControlMagicData ccData)
            {
                radius = ccData.Radius;
                layerMask = ccData.TargetLayers.value;
                return;
            }

            if (selectedMagic is DebuffMagicData debuffData)
            {
                radius = debuffData.BurstRadius;
                layerMask = debuffData.TargetLayers.value;
            }
        }

        private void NotifyCastPipeline(
            CastSlotType slot,
            SpellImpactType impactType,
            MagicData selectedMagic,
            ElementCombinationType combo,
            Vector3 targetPoint,
            bool hadInjectedElement,
            ElementType injectedElement)
        {
            if (_castPipeline == null)
            {
                return;
            }

            ResolveEffectTargeting(selectedMagic, out float effectRadius, out int effectLayerMask);

            CastContext context = new CastContext(
                gameObject,
                slot,
                impactType,
                selectedMagic,
                injectedElement,
                hadInjectedElement,
                combo,
                targetPoint,
                effectRadius,
                effectLayerMask,
                _hasLastResolvedChainKey ? _lastResolvedChainKey.Stage : ChainStage.None,
                _hasLastResolvedChainKey ? _lastResolvedChainKey.FromElement : ElementType.None,
                _hasLastResolvedChainKey ? _lastResolvedChainKey.ToElement : ElementType.None);
            _castPipeline.ProcessOnHit(context);
        }

    }
}
