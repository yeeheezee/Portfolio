using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;
using WizardBrawl.Core;
using WizardBrawl.Enemy.Status;
using WizardBrawl.Magic.Data;

namespace WizardBrawl.Enemy
{
    /// <summary>
    /// FSM 기반의 보스 AI. 행동을 결정하는 역할을 함.
    /// </summary>
    public class BossAI : MonoBehaviour, IStatusReceiver
    {
        [Header("보스 스탯 데이터")]
        [Tooltip("보스의 모든 행동 수치를 정의.")]
        [SerializeField] private BossStats _stats;

        [Header("보스 스킬 풀")]
        [Tooltip("보스가 선택할 스킬 엔트리 테이블.")]
        [SerializeField] private BossSpellPoolTable _spellPoolTable;

        [Header("치명 감시 임계값")]
        [Tooltip("공격 선택 후 이 시간 안에 시전 성공이 없으면 CRIT_NO_FIRE로 판정.")]
        [SerializeField] private float _maxNoFireWaitSeconds = 1.5f;
        [Tooltip("Attacking 상태에서 진행 신호가 이 시간 이상 없으면 CRIT_STUCK_WAIT로 판정.")]
        [SerializeField] private float _maxStuckWaitSeconds = 3.0f;
        [Header("거리 유지 보정")]
        [Tooltip("false면 근접 시 뒤로 물러나지 않아 플레이어가 거리를 좁힐 수 있음.")]
        [SerializeField] private bool _allowBackstepWhenTooClose = false;
        [Tooltip("후퇴 버스트 중 후퇴 속도(유닛/초).")]
        [FormerlySerializedAs("_backstepStepDistance")]
        [SerializeField] private float _backstepSpeed = 1.2f;
        [Tooltip("근접 시 연속 후퇴를 허용하는 버스트 시간(초).")]
        [SerializeField] private float _backstepBurstDuration = 0.25f;
        [Tooltip("다음 후퇴를 허용하기 전 대기 시간(초).")]
        [SerializeField] private float _backstepCooldown = 1.8f;
        [Header("행동 윈도우")]
        [Tooltip("접근 상태를 유지하는 최소 시간(초).")]
        [SerializeField] private float _approachWindowDuration = 0.55f;
        [Tooltip("좌우 이동 상태를 유지하는 시간(초).")]
        [SerializeField] private float _strafeWindowDuration = 1.1f;
        [Tooltip("공격 상태를 유지하는 시간(초).")]
        [SerializeField] private float _attackWindowDuration = 1.5f;
        [Tooltip("공격 후 숨 고르기 상태를 유지하는 시간(초).")]
        [SerializeField] private float _recoveryWindowDuration = 0.8f;
        [Tooltip("공격 윈도우 1회에서 허용할 최대 시전 횟수.")]
        [SerializeField] private int _maxCastsPerAttackWindow = 1;
        [Tooltip("좌우 이동 속도 배율.")]
        [SerializeField] private float _strafeSpeedMultiplier = 0.75f;

        private enum State { Idle, Approach, Strafe, Attacking, Recovery }
        private State _currentState;
        private BossCombatPhase _currentPhase = BossCombatPhase.Phase1;

        private Transform _playerTransform;
        private BossAttackCaster _attackCaster;
        private Health _health;
        private BossMovementController _movementController;
        private CombatStatusController _statusController;
        private Coroutine _attackCoroutine;
        private bool _candidateCacheDirty = true;
        private readonly List<BossSpellEntry> _phaseCandidateCache = new List<BossSpellEntry>();
        private readonly List<BossSpellEntry> _selectionBuffer = new List<BossSpellEntry>();
        private bool _waitingForFire;
        private float _fireWaitStartTime;
        private float _attackProgressHeartbeatTime;
        private float _attackWaitUntilTime;
        private float _backstepBurstEndTime;
        private float _nextBackstepAllowedTime;
        private float _stateEnteredAt;
        private int _castsInCurrentAttackWindow;
        private string _pendingSpellName = "none";
        private int _strafeDirection = 1;

        public BossCombatPhase CurrentPhase => _currentPhase;
        public bool IsPhase2Active => _currentPhase == BossCombatPhase.Phase2;
        public float Phase2ThresholdHealthNormalized => 0.5f;
        public event Action<BossCombatPhase> OnPhaseChanged;
        public event Action<float, float> OnBossHealthChanged;

        /// <summary>
        /// 디버프 적용으로 증가한 피격 계수(추후 데미지 파이프라인 연동용).
        /// </summary>
        public float IncomingDamageMultiplier => _statusController == null ? 1f : _statusController.IncomingDamageMultiplier;

        private void Awake()
        {
            _attackCaster = GetComponent<BossAttackCaster>();
            _health = GetComponent<Health>();
            _movementController = new BossMovementController(transform, GetComponent<Rigidbody>());
            _statusController = new CombatStatusController(_health);
        }

        private void Start()
        {
            InitializeTarget();
            _attackProgressHeartbeatTime = Time.time;
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.OnHealthChanged += HandleBossHealthChanged;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.OnHealthChanged -= HandleBossHealthChanged;
            }
        }

        private void Update()
        {
            _statusController.Tick(Time.time);
            UpdateCombatPhase();

            if (IsStunned())
            {
                CompleteNoFireWatch();
                MarkAttackProgress();
                StopAttackCoroutineIfRunning();
                return;
            }

            CheckCriticalGuards();

            if (_currentState == State.Attacking && _attackCoroutine == null)
            {
                _attackCoroutine = StartCoroutine(AttackDecisionCoroutine());
            }

            if (!CanAct()) return;

            LookAtPlayer();
            UpdateState();
        }

        private void FixedUpdate()
        {
            _movementController?.FlushPendingMove();
        }

        /// <summary>
        /// AI의 행동 가능 여부를 확인함.
        /// </summary>
        /// <returns>행동 가능하면 true, 아니면 false.</returns>
        private bool CanAct()
        {
            return _playerTransform != null
                && _currentState != State.Idle
                && _stats != null
                && _attackCaster != null
                && _spellPoolTable != null;
        }

        /// <summary>
        /// CC 이벤트를 생성해 상태 수신 진입점으로 전달함.
        /// </summary>
        public void ApplyCrowdControl(CrowdControlType controlType, float duration, float strength)
        {
            ApplyStatus(StatusEvent.CreateCrowdControl(controlType, duration, strength, gameObject));
        }

        /// <summary>
        /// 디버프 이벤트를 생성해 상태 수신 진입점으로 전달함.
        /// </summary>
        public void ApplyDebuff(DebuffType debuffType, float duration, float magnitude)
        {
            ApplyStatus(StatusEvent.CreateDebuff(debuffType, duration, magnitude, gameObject));
        }

        /// <summary>
        /// 상태 이벤트를 상태 컨트롤러에 위임해 적용함.
        /// </summary>
        public void ApplyStatus(StatusEvent statusEvent)
        {
            if (statusEvent.Kind == StatusEventKind.CrowdControl)
            {
                Debug.Log(
                    $"[BossStatus] receive_cc type={statusEvent.CrowdControlType} duration={statusEvent.Duration:0.00} " +
                    $"magnitude={statusEvent.Magnitude:0.00} source={(statusEvent.Source != null ? statusEvent.Source.name : "None")}",
                    this);
            }

            _statusController.Apply(statusEvent);
        }

        /// <summary>
        /// 현재 시각 기준 궁 체인 가능 여부를 반환함.
        /// </summary>
        public bool IsUltimateChainReady()
        {
            return _statusController.IsUltimateChainReady(Time.time);
        }

        /// <summary>
        /// AI의 대상을 초기화함.
        /// </summary>
        private void InitializeTarget()
        {
            if (_stats == null)
            {
                Debug.LogError("[BossAI] CRIT_NULL_REF: BossStats is null", this);
                enabled = false;
                return;
            }

            if (_attackCaster == null)
            {
                Debug.LogError("[BossAI] CRIT_NULL_REF: BossAttackCaster is null", this);
                enabled = false;
                return;
            }

            if (_spellPoolTable == null)
            {
                Debug.LogError("[BossAI] CRIT_NULL_REF: BossSpellPoolTable is null", this);
                enabled = false;
                return;
            }

            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
                TransitionToState(State.Approach);
            }
            else
            {
                Debug.LogError("Player 태그를 가진 오브젝트를 찾을 수 없습니다!", this);
                TransitionToState(State.Idle);
            }
        }

        /// <summary>
        /// AI의 상태를 현재 조건에 맞게 갱신함.
        /// </summary>
        private void UpdateState()
        {
            switch (_currentState)
            {
                case State.Approach:
                    UpdateApproachState();
                    break;
                case State.Strafe:
                    UpdateStrafeState();
                    break;
                case State.Attacking:
                    UpdateAttackingState();
                    break;
                case State.Recovery:
                    if (GetStateElapsed() >= Mathf.Max(0.1f, _recoveryWindowDuration))
                    {
                        TransitionToState(ShouldApproach() ? State.Approach : State.Strafe);
                    }
                    break;
            }
        }

        /// <summary>
        /// AI의 상태를 안전하게 전환하고, 상태 변경에 따른 정리/초기화 작업을 수행함.
        /// </summary>
        /// <param name="newState">전환할 새로운 상태.</param>
        private void TransitionToState(State newState)
        {
            if (_currentState == newState) return;
            StopAttackCoroutineIfRunning();
            if (newState != State.Attacking)
            {
                CompleteNoFireWatch();
            }

            _currentState = newState;
            _stateEnteredAt = Time.time;
            _movementController?.ClearPendingMove();
            if (_currentState == State.Attacking)
            {
                _castsInCurrentAttackWindow = 0;
                _attackCoroutine = StartCoroutine(AttackDecisionCoroutine());
            }
            else if (_currentState == State.Strafe)
            {
                _strafeDirection = UnityEngine.Random.value < 0.5f ? -1 : 1;
            }

            MarkAttackProgress();
        }

        private void UpdateApproachState()
        {
            float moveSpeed = GetCurrentMoveSpeed();
            if (moveSpeed <= 0f)
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, _playerTransform.position);

            if (distance > _stats.OptimalDistance + _stats.DistanceTolerance)
            {
                _movementController?.MoveTowards(_playerTransform.position, moveSpeed * Time.deltaTime);
            }
            else if (_allowBackstepWhenTooClose && distance < _stats.OptimalDistance - _stats.DistanceTolerance)
            {
                Backstep(moveSpeed);
            }

            if (!ShouldApproach() && GetStateElapsed() >= Mathf.Max(0.1f, _approachWindowDuration))
            {
                TransitionToState(State.Strafe);
            }
        }

        private void UpdateStrafeState()
        {
            float moveSpeed = GetCurrentMoveSpeed();
            if (moveSpeed <= 0f)
            {
                return;
            }

            if (ShouldApproach())
            {
                TransitionToState(State.Approach);
                return;
            }

            if (_allowBackstepWhenTooClose && IsTooClose())
            {
                Backstep(moveSpeed);
            }
            else
            {
                Vector3 toPlayer = (_playerTransform.position - transform.position);
                toPlayer.y = 0f;
                Vector3 lateral = Vector3.Cross(Vector3.up, toPlayer.normalized) * _strafeDirection;
                if (lateral.sqrMagnitude > 0.0001f)
                {
                    float strafeSpeed = moveSpeed * Mathf.Max(0.1f, _strafeSpeedMultiplier);
                    _movementController?.MoveLateral(toPlayer, _strafeDirection, strafeSpeed * Time.deltaTime);
                }
            }

            if (GetStateElapsed() >= Mathf.Max(0.2f, _strafeWindowDuration))
            {
                TransitionToState(State.Attacking);
            }
        }

        private void UpdateAttackingState()
        {
            if (_allowBackstepWhenTooClose && IsTooClose())
            {
                TransitionToState(State.Approach);
                return;
            }

            if (GetStateElapsed() >= Mathf.Max(0.1f, _attackWindowDuration))
            {
                TransitionToState(State.Recovery);
            }
        }

        private bool ShouldApproach()
        {
            float distance = Vector3.Distance(transform.position, _playerTransform.position);
            return distance > _stats.OptimalDistance + (_stats.DistanceTolerance * 0.5f);
        }

        private void Backstep(float moveSpeed)
        {
            bool inBurst = Time.time <= _backstepBurstEndTime;
            if (!inBurst && Time.time >= _nextBackstepAllowedTime)
            {
                _backstepBurstEndTime = Time.time + Mathf.Max(0.05f, _backstepBurstDuration);
                _nextBackstepAllowedTime = _backstepBurstEndTime + Mathf.Max(0.05f, _backstepCooldown);
                inBurst = true;
            }

            if (!inBurst)
            {
                return;
            }

            float backstepSpeed = Mathf.Max(0.1f, _backstepSpeed);
            _movementController?.MoveAwayFrom(_playerTransform.position, Mathf.Max(backstepSpeed, moveSpeed) * Time.deltaTime);
        }

        private float GetStateElapsed()
        {
            return Time.time - _stateEnteredAt;
        }

        private bool IsTooClose()
        {
            float distance = Vector3.Distance(transform.position, _playerTransform.position);
            return distance < (_stats.OptimalDistance - _stats.DistanceTolerance);
        }

        /// <summary>
        /// 플레이어를 향해 부드럽게 회전함.
        /// </summary>
        private void LookAtPlayer()
        {
            var targetDirection = _playerTransform.position - transform.position;
            targetDirection.y = 0;
            if (targetDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _stats.RotationSpeed * Time.deltaTime);
        }

        /// <summary>
        /// 공격 상태일 때, 후보군에서 공격을 선택해 시전하는 코루틴.
        /// </summary>
        private IEnumerator AttackDecisionCoroutine()
        {
            MarkAttackProgress();
            while (_currentState == State.Attacking)
            {
                if (_castsInCurrentAttackWindow >= Mathf.Max(1, _maxCastsPerAttackWindow))
                {
                    TransitionToState(State.Recovery);
                    yield break;
                }

                if (IsStunned())
                {
                    MarkAttackProgress();
                    yield return null;
                    continue;
                }

                if (TrySelectSpellEntry(out BossSpellEntry selectedEntry))
                {
                    BeginNoFireWatch(selectedEntry);
                    bool casted = _attackCaster.TryCast(selectedEntry);
                    if (casted)
                    {
                        _castsInCurrentAttackWindow++;
                        CompleteNoFireWatch();
                        MarkAttackProgress();
                        float attackCooldown = ResolveAttackCooldown(selectedEntry);
                        float attackWait = attackCooldown * _statusController.AttackDelayMultiplier;
                        SetExpectedWait(attackWait);
                        yield return new WaitForSeconds(attackWait);
                    }
                    else
                    {
                        MarkAttackProgress();
                        yield return null;
                    }
                }
                else
                {
                    MarkAttackProgress();
                    yield return null;
                }

                MarkAttackProgress();
                float restWait = _stats.RestBetweenAttacks * _statusController.AttackDelayMultiplier;
                SetExpectedWait(restWait);
                yield return new WaitForSeconds(restWait);
            }
        }

        private bool IsStunned()
        {
            return _statusController.IsStunned(Time.time);
        }

        private float GetCurrentMoveSpeed()
        {
            return _stats.MoveSpeed * _statusController.GetMoveMultiplier(Time.time);
        }

        private void StopAttackCoroutineIfRunning()
        {
            if (_attackCoroutine == null)
            {
                return;
            }

            StopCoroutine(_attackCoroutine);
            _attackCoroutine = null;
        }

        private bool TrySelectSpellEntry(out BossSpellEntry selectedEntry)
        {
            selectedEntry = null;

            RebuildPhaseCandidateCacheIfNeeded();
            if (_phaseCandidateCache.Count == 0)
            {
                return false;
            }

            _selectionBuffer.Clear();
            IReadOnlyList<BossSpellEntry> entries = _phaseCandidateCache;
            float totalWeight = 0f;
            for (int i = 0; i < entries.Count; i++)
            {
                BossSpellEntry entry = entries[i];
                if (entry == null || entry.Spell == null)
                {
                    continue;
                }

                if (!_attackCaster.IsEntryReady(entry))
                {
                    continue;
                }

                float weight = EvaluateSpellWeight(entry);
                if (weight <= 0f)
                {
                    continue;
                }

                _selectionBuffer.Add(entry);
                totalWeight += weight;
            }

            if (_selectionBuffer.Count == 0 || totalWeight <= 0f)
            {
                return false;
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulativeWeight = 0f;
            for (int i = 0; i < _selectionBuffer.Count; i++)
            {
                BossSpellEntry entry = _selectionBuffer[i];
                cumulativeWeight += EvaluateSpellWeight(entry);
                if (roll <= cumulativeWeight)
                {
                    selectedEntry = entry;
                    break;
                }
            }

            selectedEntry ??= _selectionBuffer[_selectionBuffer.Count - 1];
            Debug.Log($"[BossAI] select spell={selectedEntry.Spell.MagicName} tier={selectedEntry.Tier} parry={selectedEntry.ParryRule} phaseGate={selectedEntry.PhaseGate}");
            return true;
        }

        private float EvaluateSpellWeight(BossSpellEntry entry)
        {
            float weight = 1f;
            float distance = _playerTransform == null ? _stats.OptimalDistance : Vector3.Distance(transform.position, _playerTransform.position);
            bool isCloseRange = distance <= _stats.CloseRangeThreshold;
            bool isFarRange = distance >= _stats.FarRangeThreshold;
            bool isUltimateChainReady = IsUltimateChainReady();

            MagicData spell = entry.Spell;
            if (spell is ProjectileMagicData)
            {
                if (isFarRange)
                {
                    weight += _stats.ProjectileFarWeightBonus;
                }

                if (isCloseRange)
                {
                    weight *= Mathf.Max(0.05f, _stats.ProjectileCloseWeightPenalty);
                }
            }
            else if (spell is CrowdControlMagicData)
            {
                if (!isCloseRange && !isFarRange)
                {
                    weight += _stats.CrowdControlMidWeightBonus;
                }
            }
            else if (spell is FieldMagicData)
            {
                if (!isFarRange)
                {
                    weight += isCloseRange ? _stats.FieldCloseWeightBonus : _stats.FieldMidWeightBonus;
                }
            }

            if (spell.UseUltimateFlow && isUltimateChainReady)
            {
                weight += _stats.UltimateChainWeightBonus;
            }

            if (entry.Tier == BossSpellTier.Heavy && isCloseRange)
            {
                weight += 0.75f;
            }

            return Mathf.Max(0.05f, weight);
        }

        private void UpdateCombatPhase()
        {
            if (_health == null)
            {
                return;
            }

            if (_currentPhase == BossCombatPhase.Phase2)
            {
                return;
            }

            float phase2Threshold = _health.MaxHealth * 0.5f;
            if (_health.CurrentHealth > phase2Threshold)
            {
                return;
            }

            _currentPhase = BossCombatPhase.Phase2;
            _candidateCacheDirty = true;
            OnPhaseChanged?.Invoke(_currentPhase);
            Debug.Log($"[BossState] phase transition: Phase1 -> Phase2 (hp={_health.CurrentHealth:0.0}/{_health.MaxHealth:0.0})");
            MarkAttackProgress();
        }

        private void RebuildPhaseCandidateCacheIfNeeded()
        {
            if (!_candidateCacheDirty)
            {
                return;
            }

            _phaseCandidateCache.Clear();
            if (_spellPoolTable == null || _spellPoolTable.Entries == null)
            {
                return;
            }

            IReadOnlyList<BossSpellEntry> entries = _spellPoolTable.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                BossSpellEntry entry = entries[i];
                if (entry == null || entry.Spell == null)
                {
                    continue;
                }

                if (!IsPhaseAllowed(entry))
                {
                    continue;
                }

                _phaseCandidateCache.Add(entry);
            }

            _candidateCacheDirty = false;
            Debug.Log($"[BossState] candidate cache rebuilt: phase={_currentPhase}, count={_phaseCandidateCache.Count}");
            MarkAttackProgress();
        }

        private bool IsPhaseAllowed(BossSpellEntry entry)
        {
            switch (entry.PhaseGate)
            {
                case BossPhaseGate.AllPhases:
                    return true;
                case BossPhaseGate.Phase1Only:
                    return _currentPhase == BossCombatPhase.Phase1;
                case BossPhaseGate.Phase2Only:
                    return _currentPhase == BossCombatPhase.Phase2;
                default:
                    return false;
            }
        }

        private float ResolveAttackCooldown(BossSpellEntry entry)
        {
            if (entry == null)
            {
                return _stats.StandardAttackCooldown;
            }

            if (entry.ParryRule == BossParryRule.Unparryable)
            {
                return _stats.UnparryableAttackCooldown;
            }

            return entry.Tier == BossSpellTier.Heavy
                ? _stats.HeavyAttackCooldown
                : _stats.StandardAttackCooldown;
        }

        private void BeginNoFireWatch(BossSpellEntry entry)
        {
            _waitingForFire = true;
            _fireWaitStartTime = Time.time;
            _pendingSpellName = entry == null || entry.Spell == null ? "null" : entry.Spell.MagicName;
            MarkAttackProgress();
        }

        private void CompleteNoFireWatch()
        {
            _waitingForFire = false;
            _pendingSpellName = "none";
        }

        private void CheckCriticalGuards()
        {
            if (_currentState != State.Attacking)
            {
                return;
            }

            if (IsStunned())
            {
                return;
            }

            if (_waitingForFire)
            {
                float noFireElapsed = Time.time - _fireWaitStartTime;
                if (noFireElapsed >= _maxNoFireWaitSeconds)
                {
                    Debug.LogError($"[BossState] CRIT_NO_FIRE: spell={_pendingSpellName}, elapsed={noFireElapsed:0.00}s", this);
                    HandleCriticalLoopFailure("CRIT_NO_FIRE");
                    return;
                }
            }

            float stuckElapsed = Time.time - _attackProgressHeartbeatTime;
            if (Time.time < _attackWaitUntilTime)
            {
                return;
            }

            if (stuckElapsed >= _maxStuckWaitSeconds)
            {
                Debug.LogError($"[BossState] CRIT_STUCK_WAIT: state=Attacking, elapsed={stuckElapsed:0.00}s", this);
                HandleCriticalLoopFailure("CRIT_STUCK_WAIT");
            }
        }

        private void HandleCriticalLoopFailure(string reason)
        {
            CompleteNoFireWatch();
            StopAttackCoroutineIfRunning();
            TransitionToState(State.Approach);
            MarkAttackProgress();
            Debug.LogWarning($"[BossState] fail-closed reset applied: reason={reason}");
        }

        private void MarkAttackProgress()
        {
            _attackProgressHeartbeatTime = Time.time;
            _attackWaitUntilTime = 0f;
        }

        private void SetExpectedWait(float waitSeconds)
        {
            _attackWaitUntilTime = Time.time + Mathf.Max(0f, waitSeconds);
        }

        private void HandleBossHealthChanged(float currentHealth, float maxHealth)
        {
            OnBossHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
}
