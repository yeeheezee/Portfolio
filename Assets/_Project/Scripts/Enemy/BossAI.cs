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
        // 보스의 상태 정의
        private enum State { Idle, MaintainingDistance, Attacking, Cooldown }
        private State _currentState;

        [Header("AI 행동 설정")]
        [SerializeField] private float _optimalDistance = 15f; // 유지하려는 최적 거리
        [SerializeField] private float _distanceTolerance = 2f; // 최적 거리에서 허용 오차
        [SerializeField] private float _moveSpeed = 5f;

        [Header("공격 타이밍")]
        [SerializeField] private float _attackCooldown = 2.0f; // 공격 패턴이 끝난 후 최소 휴식 시간

        // --- 내부 참조 변수 ---
        private Transform _playerTransform;
        private BossAttackCaster _attackCaster;

        private void Awake()
        {
            _attackCaster = GetComponent<BossAttackCaster>();
        }

        private void Start()
        {
            // 플레이어 태그 탐색
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
                // 초기 상태 설정
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

            // 현재 상태에 따라 행동 실행
            switch (_currentState)
            {
                case State.Idle:
                    // 플레이어를 찾으면 거리 유지 상태로 전환
                    break;
                case State.MaintainingDistance:
                    LookAtPlayer();
                    MaintainOptimalDistance();
                    // 최적 거리가 유지되면 공격 상태로 전환
                    if (IsAtOptimalDistance())
                    {
                        TransitionToState(State.Attacking);
                    }
                    break;
                case State.Attacking:
                    // 공격 패턴 실행 (코루틴으로 처리)
                    break;
                case State.Cooldown:
                    // 쿨타임 중에는 플레이어를 바라보기만 함
                    LookAtPlayer();
                    break;
            }
        }

        private void TransitionToState(State newState)
        {
            _currentState = newState;
            // 상태 전환 시 필요한 로직 실행
            switch (_currentState)
            {
                case State.Idle:
                    // 할 일 없음
                    break;
                case State.MaintainingDistance:
                    // 할 일 없음
                    break;
                case State.Attacking:
                    // 공격 코루틴 시작
                    StartCoroutine(AttackPatternCoroutine());
                    break;
                case State.Cooldown:
                    // 쿨타임 코루틴 시작
                    StartCoroutine(CooldownCoroutine());
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

        private IEnumerator AttackPatternCoroutine()
        {
            // 지금은 50% 확률로 일반 공격, 50% 확률로 패링불가 공격을 시도
            if (Random.value < 0.5f)
            {
                _attackCaster.PerformStandardAttack();
            }
            else
            {
                _attackCaster.PerformUnparryableAttack();
            }

            // 공격이 끝날 때까지 잠시 대기 (애니메이션 시간 등 고려)
            yield return new WaitForSeconds(1.0f);

            // 공격이 끝나면 쿨타임 상태로 전환
            TransitionToState(State.Cooldown);
        }

        private IEnumerator CooldownCoroutine()
        {
            yield return new WaitForSeconds(_attackCooldown);
            // 쿨타임이 끝나면 다시 거리 유지 상태로 돌아가 다음 공격을 준비
            TransitionToState(State.MaintainingDistance);
        }
    }
}