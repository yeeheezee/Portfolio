using UnityEngine;
using System.Collections;

namespace WizardBrawl.Enemy
{
    /// <summary>
    /// FSM 기반의 보스 AI. 행동을 결정하는 역할을 함.
    /// </summary>
    public class BossAI : MonoBehaviour
    {
        [Header("보스 스탯 데이터")]
        [Tooltip("보스의 모든 행동 수치를 정의.")]
        [SerializeField] private BossStats _stats;

        private enum State { Idle, MaintainingDistance, Attacking }
        private State _currentState;

        private Transform _playerTransform;
        private BossAttackCaster _attackCaster;
        private Coroutine _attackCoroutine;
        private int _standardAttackCount = 0;

        private void Awake()
        {
            _attackCaster = GetComponent<BossAttackCaster>();
        }

        private void Start()
        {
            InitializeTarget();
        }

        private void Update()
        {
            if (!CanAct()) return;

            LookAtPlayer();
            UpdateState();
        }

        /// <summary>
        /// AI의 행동 가능 여부를 확인함.
        /// </summary>
        /// <returns>행동 가능하면 true, 아니면 false.</returns>
        private bool CanAct()
        {
            return _playerTransform != null && _currentState != State.Idle && _stats != null;
        }

        /// <summary>
        /// AI의 대상을 초기화함.
        /// </summary>
        private void InitializeTarget()
        {
            if (_stats == null)
            {
                Debug.LogError("BossStats가 할당되지 않았습니다! AI를 비활성화합니다.", this);
                enabled = false;
                return;
            }

            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
                TransitionToState(State.MaintainingDistance);
            }
            else
            {
                Debug.LogError("Player 태그를 가진 오브젝트를 찾을 수 없습니다!", this);
                TransitionToState(State.Idle);
            }
        }

        private void Update()
        /// <summary>
        /// AI의 상태를 현재 조건에 맞게 갱신함.
        /// </summary>
        {
            if (_playerTransform == null || _currentState == State.Idle) return;
            LookAtPlayer();
            switch (_currentState)
            {
                case State.MaintainingDistance:
                    MaintainOptimalDistance();
                    if (IsAtOptimalDistance()) TransitionToState(State.Attacking);
                    break;
                case State.Attacking:
                    if (!IsAtOptimalDistance()) TransitionToState(State.MaintainingDistance);
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

            _currentState = newState;

            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
                _attackCoroutine = null;
            }
            _currentState = newState;
            if (_currentState == State.Attacking)
            {
                case State.Attacking:
                    _attackCoroutine = StartCoroutine(AttackDecisionCoroutine());
                    break;
            }
        }

        /// <summary>
        /// 플레이어와 최적의 거리를 유지하도록 위치를 조정함.
        /// </summary>
        private void MaintainOptimalDistance()
        {
            float distance = Vector3.Distance(transform.position, _playerTransform.position);
            if (distance > _stats.OptimalDistance + _stats.DistanceTolerance)
            {
                transform.position = Vector3.MoveTowards(transform.position, _playerTransform.position, _stats.MoveSpeed * Time.deltaTime);
            }
            else if (distance < _stats.OptimalDistance - _stats.DistanceTolerance)
            {
                Vector3 awayDir = (transform.position - _playerTransform.position).normalized;
                transform.position = Vector3.MoveTowards(transform.position, transform.position + awayDir, _stats.MoveSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// 현재 플레이어와의 거리가 공격하기에 적합한지 판별함.
        /// </summary>
        /// <returns>최적 거리 내에 있으면 true, 아니면 false.</returns>
        private bool IsAtOptimalDistance()
        {
            float distance = Vector3.Distance(transform.position, _playerTransform.position);
            return Mathf.Abs(distance - _stats.OptimalDistance) <= _stats.DistanceTolerance;
        }

        /// <summary>
        /// 플레이어를 향해 부드럽게 회전함.
        /// </summary>
        private void LookAtPlayer()
        {
            var targetDirection = _playerTransform.position - transform.position;
            targetDirection.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _stats.RotationSpeed * Time.deltaTime);
        }

        /// <summary>
        /// 공격 상태일 때, 어떤 공격을 사용할지 우선순위에 따라 결정하는 코루틴.
        /// </summary>
        private IEnumerator AttackDecisionCoroutine()
        {
            while (_currentState == State.Attacking)
            {
                if (_standardAttackCount >= 2 && _attackCaster.IsUnparryableAttackReady)
                {
                    _attackCaster.PerformUnparryableAttack();
                    _standardAttackCount = 0;
                    yield return new WaitForSeconds(_stats.UnparryableAttackCooldown);
                }
                else if (_attackCaster.IsHeavyAttackReady)
                {
                    _attackCaster.PerformHeavyAttack();
                    yield return new WaitForSeconds(_stats.HeavyAttackCooldown);
                }
                else if (_attackCaster.IsStandardAttackReady)
                {
                    _attackCaster.PerformStandardAttack();
                    _standardAttackCount++;
                    yield return new WaitForSeconds(_stats.StandardAttackCooldown);
                }
                else
                {
                    yield return null;
                }
                yield return new WaitForSeconds(_stats.RestBetweenAttacks);
            }
        }
    }
}