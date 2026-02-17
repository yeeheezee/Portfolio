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

        private readonly ElementCombinationResolver _combinationResolver = new ElementCombinationResolver();
        private PlayerElementSlot _elementSlot;
        private bool _isTargeting;
        private MagicData _pendingMagic;
        private ElementCombinationType _pendingCombination = ElementCombinationType.None;
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

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (!_isTargeting)
            {
                return;
            }

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
            Debug.Log($"[Targeting] enter: magic={selectedMagic?.MagicName ?? "None"}, combo={combo}");
        }

        private void ConfirmTargetingCast()
        {
            if (!TryGetTargetPoint(out Vector3 targetPoint))
            {
                Debug.Log("[Targeting] blocked: invalid ground point");
                return;
            }

            if (TryCastSelectedMagic(_pendingMagic, _pendingCombination, targetPoint))
            {
                Debug.Log("[Targeting] confirm: cast success");
                _isTargeting = false;
                _pendingMagic = null;
                _pendingCombination = ElementCombinationType.None;
            }
            else
            {
                Debug.Log("[Targeting] confirm: cast failed");
            }
        }

        private void CancelTargeting(string reason)
        {
            Debug.Log($"[Targeting] cancel: reason={reason}");
            _isTargeting = false;
            _pendingMagic = null;
            _pendingCombination = ElementCombinationType.None;
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
