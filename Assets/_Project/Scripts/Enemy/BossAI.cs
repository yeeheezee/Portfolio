using UnityEngine;
using System.Collections;

namespace WizardBrawl.Enemy
{
    /// <summary>
    /// FSM을 기반으로 작동하며 플레이어에게 다양한 공격 패턴을 제시하는 역할.
    /// </summary>
    public class BossAI : MonoBehaviour
    {
        [Header("보스 스탯 데이터")]
        [Tooltip("보스의 모든 행동 수치를 정의")]
        [SerializeField] private BossStats _stats;

        // 보스가 가질 수 있는 상태를 정의
        private enum State { Idle, MaintainingDistance, Attacking }
        private State _currentState;

        // --- 내부 참조 변수 ---
        private Transform _playerTransform;
        private BossAttackCaster _attackCaster; // 실제 공격을 수행하는 컴포넌트
        private Coroutine _attackCoroutine; // 공격 패턴 코루틴을 제어하기 위한 변수

        // 공격 패턴 제어를 위한 변수
        private int _standardAttackCount = 0; // 일반 공격 횟수 카운트

        private void Awake()
        {
            _attackCaster = GetComponent<BossAttackCaster>();
        }

        private void Start()
        {
            // 필수 데이터인 _stats가 할당되지 않았으면 비활성화함.
            if (_stats == null)
            {
                Debug.LogError("BossStats ScriptableObject가 할당되지 않았습니다! AI가 작동할 수 없습니다.", this);
                TransitionToState(State.Idle);
                return;
            }

            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
                TransitionToState(State.MaintainingDistance); // 첫 상태는 거리 유지로 시작
            }
            else
            {
                Debug.LogError("씬에 'Player' 태그를 가진 오브젝트가 없습니다!", this);
                TransitionToState(State.Idle);
            }
        }

        private void Update()
        {
            // 플레이어가 없거나, 유휴 상태일 때는 아무것도 처리하지 않음.
            if (_playerTransform == null || _currentState == State.Idle) return;

            // 현재 상태와 관계없이 항상 플레이어를 바라봄.
            LookAtPlayer();

            // 현재 상태에 따라 적절한 행동을 매 프레임 실행함.
            switch (_currentState)
            {
                case State.MaintainingDistance:
                    MaintainOptimalDistance();
                    // 최적 거리에 도달하면 공격 상태로 전환.
                    if (IsAtOptimalDistance())
                    {
                        TransitionToState(State.Attacking);
                    }
                    break;

                case State.Attacking:
                    // 공격 중 거리가 멀어지면 다시 거리 유지 상태로 전환.
                    if (!IsAtOptimalDistance())
                    {
                        TransitionToState(State.MaintainingDistance);
                    }
                    break;
            }
        }

        /// <summary>
        /// AI의 상태를 새로운 상태로 안전하게 전환.
        /// </summary>
        /// <param name="newState">전환할 새로운 상태</param>
        private void TransitionToState(State newState)
        {
            if (_currentState == newState) return;

            _currentState = newState;

            // 상태를 나갈 때, 이전에 실행되던 공격 코루틴이 있다면 반드시 중지.
            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
                _attackCoroutine = null;
            }

            // 새로운 상태에 따른 진입 로직을 처리.
            switch (_currentState)
            {
                case State.Attacking:
                    // 공격 상태에 진입하면 공격 패턴을 결정하는 코루틴을 시작.
                    _attackCoroutine = StartCoroutine(AttackDecisionCoroutine());
                    break;
            }
        }

        /// <summary>
        /// 플레이어와의 거리를 _stats에 정의된 최적 거리로 유지하기 위해 움직임을 처리함.
        /// </summary>
        private void MaintainOptimalDistance()
        {
            float distance = Vector3.Distance(transform.position, _playerTransform.position);

            // 최적 거리보다 멀면 플레이어에게 다가감.
            if (distance > _stats.OptimalDistance + _stats.DistanceTolerance)
            {
                transform.position = Vector3.MoveTowards(transform.position, _playerTransform.position, _stats.MoveSpeed * Time.deltaTime);
            }
            // 최적 거리보다 가까우면 플레이어에게서 멀어짐.
            else if (distance < _stats.OptimalDistance - _stats.DistanceTolerance)
            {
                Vector3 directionAwayFromPlayer = (transform.position - _playerTransform.position).normalized;
                transform.position = Vector3.MoveTowards(transform.position, transform.position + directionAwayFromPlayer, _stats.MoveSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// 현재 플레이어와의 거리가 최적 거리 내에 있는지 판별함.
        /// </summary>
        /// <returns>최적 거리 내에 있으면 true, 아니면 false를 반환함.</returns>
        private bool IsAtOptimalDistance()
        {
            float distance = Vector3.Distance(transform.position, _playerTransform.position);
            return Mathf.Abs(distance - _stats.OptimalDistance) <= _stats.DistanceTolerance;
        }

        /// <summary>
        /// 플레이어를 향해 부드럽게 회전하도록 처리함.
        /// </summary>
        private void LookAtPlayer()
        {
            var targetDirection = _playerTransform.position - transform.position;
            targetDirection.y = 0; // Y축 회전 고정.
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _stats.RotationSpeed * Time.deltaTime);
        }

        /// <summary>
        /// 공격 상태인 동안 계속 실행되는 코루틴 분기 함수.
        /// </summary>
        private IEnumerator AttackDecisionCoroutine()
        {
            while (_currentState == State.Attacking)
            {

                // 일반 공격 2번 후 패링 불가 공격 준비 완료 시
                if (_standardAttackCount >= 2 && _attackCaster.IsUnparryableAttackReady)
                {
                    _attackCaster.PerformUnparryableAttack();
                    _standardAttackCount = 0; // 카운트 리셋
                    yield return new WaitForSeconds(_stats.UnparryableAttackCooldown);
                }
                // 강공격 준비 완료 시
                else if (_attackCaster.IsHeavyAttackReady)
                {
                    _attackCaster.PerformHeavyAttack();
                    yield return new WaitForSeconds(_stats.HeavyAttackCooldown);
                }
                // 일반 공격 준비 완료 시
                else if (_attackCaster.IsStandardAttackReady)
                {
                    _attackCaster.PerformStandardAttack();
                    _standardAttackCount++; // 일반 공격 카운트 증가
                    yield return new WaitForSeconds(_stats.StandardAttackCooldown);
                }
                // 모든 공격이 쿨타임일 경우
                else
                {
                    // 아무것도 하지 않고 다음 프레임에 다시 결정 로직을 실행.
                    yield return null;
                }

                // 어떤 공격이든 끝난 후에 추가적인 휴식 시간을 부여하여 너무 기계적으로 공격하지 않도록 함
                yield return new WaitForSeconds(_stats.RestBetweenAttacks);
            }
        }
    }
}