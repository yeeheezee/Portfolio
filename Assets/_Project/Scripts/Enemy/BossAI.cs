using UnityEngine;
using System.Collections; // 코루틴 사용을 위해 추가

namespace WizardBrawl.Enemy
{
    /// <summary>
    /// '마법 패링' 시스템에 맞춰 재설계된 보스 AI.
    /// 플레이어에게 다양한 공격 패턴을 제시하는 역할을 함.
    /// </summary>
    public class BossAI : MonoBehaviour
    {
        // Cooldown 상태를 제거. 스킬별 쿨타임으로 관리하므로 전역 쿨타임은 불필요.
        private enum State { Idle, MaintainingDistance, Attacking }
        private State _currentState;

        [Header("AI 행동 설정")] 
        // 유지하려는 최적 거리
        [SerializeField] private float _optimalDistance = 15f;
        // 최적 거리에서 허용 오차
        [SerializeField] private float _distanceTolerance = 2f;
        //이동 속도
        [SerializeField] private float _moveSpeed = 5f;

        // --- 내부 참조 변수 ---
        private Transform _playerTransform;
        private BossAttackCaster _attackCaster;
        private Coroutine _attackCoroutine; // 공격 코루틴을 제어하기 위한 변수

        // 공격 패턴 제어를 위한 변수
        private int _standardAttackCount = 0; // 일반 공격 횟수 카운트

        private void Awake()
        {
            _attackCaster = GetComponent<BossAttackCaster>();
        }

        private void Start()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
                TransitionToState(State.MaintainingDistance);
            }
            else
            {
                Debug.LogError("씬에 'Player' 태그를 가진 오브젝트가 없습니다!", this);
                TransitionToState(State.Idle);
            }
        }

        private void Update()
        {
            if (_playerTransform == null) return;

            switch (_currentState)
            {
                case State.Idle:
                    break;

                case State.MaintainingDistance:
                    // 추격 상태에서는 플레이어와의 거리를 확인하고 최적의 거리로 이동
                    LookAtPlayer();
                    MaintainOptimalDistance();
                    if (IsAtOptimalDistance())
                    {
                        TransitionToState(State.Attacking);
                    }
                    break;

                case State.Attacking:
                    // 공격 상태에서는 플레이어를 계속 주시하고, 거리가 멀어지면 추격 상태로 돌아감.
                    LookAtPlayer();
                    if (!IsAtOptimalDistance())
                    {
                        TransitionToState(State.MaintainingDistance);
                    }
                    break;
            }
        }

        private void TransitionToState(State newState)
        {
            if (_currentState == newState) return;

            _currentState = newState;

            // 상태 전환 시 코루틴을 명확하게 제어
            // 기존 공격 코루틴이 있다면 중지
            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
                _attackCoroutine = null;
            }

            switch (_currentState)
            {
                case State.Attacking:
                    // 새로운 공격 코루틴 시작
                    _attackCoroutine = StartCoroutine(AttackDecisionCoroutine());
                    break;

                default:
                    // 다른 상태에서는 특별히 할 일 없음
                    break;
            }
        }

        // --- 상태별 행동 메서드 ---

        private void MaintainOptimalDistance()
        {
            float distance = Vector3.Distance(transform.position, _playerTransform.position);

            // 최적 거리보다 멀면 다가가기
            if (distance > _optimalDistance + _distanceTolerance)
            {
                transform.position = Vector3.MoveTowards(transform.position, _playerTransform.position, _moveSpeed * Time.deltaTime);
            }
            // 최적 거리보다 가까우면 멀어지기
            else if (distance < _optimalDistance - _distanceTolerance)
            {
                //현재 위치에서 플레이어 거리를 뺀 값을 정규화하여 방향 구하기
                Vector3 directionAwayFromPlayer = (transform.position - _playerTransform.position).normalized;
                transform.position = Vector3.MoveTowards(transform.position, transform.position + directionAwayFromPlayer, _moveSpeed * Time.deltaTime);
            }
        }

        private bool IsAtOptimalDistance()
        {
            float distance = Vector3.Distance(transform.position, _playerTransform.position);
            // 최적 거리 계산
            return Mathf.Abs(distance - _optimalDistance) <= _distanceTolerance;
        }

        private void LookAtPlayer()
        {
            var targetDirection = _playerTransform.position - transform.position;
            targetDirection.y = 0;
            //타겟 쪽으로 회전
            transform.rotation = Quaternion.LookRotation(targetDirection);
        }

        // --- 코루틴 기반 패턴 관리 ---

        /// <summary>
        /// 공격 상태(Attacking)인 동안 계속 실행되며,
        /// 스킬 쿨타임을 확인하여 최적의 공격을 결정하는 지능적인 코루틴.
        /// </summary>
        private IEnumerator AttackDecisionCoroutine()
        {
            LookAtPlayer();
            yield return null;
            // 공격 상태인 동안 이 루프는 계속 반복됨.
            while (_currentState == State.Attacking)
            {
                // --- 공격 결정 로직 (우선순위 기반) ---

                // 우선순위 1: "일반 공격 2번 후, 패링 불가 공격이 준비되었는가?"
                if (_standardAttackCount >= 2 && _attackCaster.IsUnparryableAttackReady)
                {
                    _attackCaster.PerformUnparryableAttack();
                    _standardAttackCount = 0; // 카운트 리셋
                    yield return new WaitForSeconds(2.0f); // 패링불가 공격의 후딜레이
                }
                // 우선순위 2: "강력한 공격(Heavy)이 준비되었는가?"
                else if (_attackCaster.IsHeavyAttackReady)
                {
                    _attackCaster.PerformHeavyAttack();
                    yield return new WaitForSeconds(1.5f); // 강력한 공격의 후딜레이
                }
                // 우선순위 3: "일반 공격이 준비되었는가?"
                else if (_attackCaster.IsStandardAttackReady)
                {
                    _attackCaster.PerformStandardAttack();
                    _standardAttackCount++; // 일반 공격 카운트 증가
                    yield return new WaitForSeconds(1.0f); // 일반 공격의 후딜레이
                }
                // 모든 공격이 쿨타임일 경우:
                else
                {
                    // 아무것도 하지 않고 다음 프레임에 다시 결정 로직을 실행.
                    yield return null;
                }
            }
        }
    }
}