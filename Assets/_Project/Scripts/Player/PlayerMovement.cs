using UnityEngine;
using UnityEngine.InputSystem;

namespace WizardBrawl.Player
{

    /// <summary>
    /// 플레이어의 수평 이동 담당 클래스.
    /// </summary>
    /// <remarks>
    /// 이 컴포넌트는 Rigidbody와 PlayerJump 컴포넌트가 반드시 함께 존재해야 함.
    /// 입력은 OnMove 함수로 전달받음.
    /// </remarks>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerJump))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("캐릭터의 지상 최대 이동 속도.")]
        [SerializeField] private float moveSpeed = 8f;

        [Tooltip("공중에서의 이동 제어력 계수.")]
        [SerializeField][Range(0f, 5f)] private float airControlFactor = 3f;

        [Tooltip("캐릭터가 목표 방향을 바라보는 데 걸리는 시간(초). 낮을수록 빠르게 회전.")]
        [SerializeField] private float rotationSmoothTime = 0.1f;

        [Tooltip("최대 속도 제한.")]
        [SerializeField] private float maxVelocityChange = 20.0f;

        /// <summary>
        /// 외부에서 이 캐릭터의 움직임을 제어할 수 있게 하는 프로퍼티.
        /// false로 설정하면 모든 움직임이 즉시 정지.
        /// </summary>
        public bool CanMove { get; set; } = true;

        // --- Private Fields ---
        private Vector2 _moveInput;             // Input System으로부터 받은 2D 입력 값.
        private Vector3 _targetDirection;       // 이동해야 할 최종 목표 방향.
        private float _targetRotationVelocity;  // 회전에 사용되는 내부 변수.

        // --- Component References ---
        private Rigidbody _rb;
        private Transform _mainCameraTransform;
        private PlayerJump _playerJump;


        private void Awake()
        {
            // 컴포넌트 캐싱
            _rb = GetComponent<Rigidbody>();
            _mainCameraTransform = Camera.main.transform;
            _playerJump = GetComponent<PlayerJump>();

            // 캐릭터가 넘어지는 것을 방지하기 위해 Rigidbody의 회전 고정.
            // 모든 회전은 HandleRotation()을 통해서만 제어.
            _rb.freezeRotation = true;
        }

        /// <summary>
        /// Player Input의 Unity Event로부터 Vector2 이동 값을 받아 처리
        /// </summary>
        public void SetMoveInput(Vector2 moveInput)
        {
            _moveInput = moveInput;
        }

        private void FixedUpdate()
        {
            if (!CanMove) // 움직일 수 없는 상황일 경우
            {
                // 속도를 즉시 0으로.
                _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f); // Y축 속도는 유지
                return;
            }

            HandleMovement();
            HandleRotation();
        }

        /// <summary>
        /// 입력 값과 카메라 방향을 바탕으로 캐릭터의 이동을 처리.
        /// </summary>
        private void HandleMovement()
        {
            // 현재 메인 카메라가 바라보는 방향을 기준으로 앞쪽과 오른쪽 방향 벡터 가져옴.
            Vector3 camForward = _mainCameraTransform.forward;
            Vector3 camRight = _mainCameraTransform.right;

            // Y축 값은 0으로 만들어 카메라가 위나 아래를 보더라도 캐릭터는 수평으로만 움직이게 제어.
            camForward.y = 0;
            camRight.y = 0;

            // (앞/뒤 입력 * 카메라 정면) + (좌/우 입력 * 카메라 오른쪽)으로 최종 목표 방향을 계산.
            _targetDirection = (camForward.normalized * _moveInput.y + camRight.normalized * _moveInput.x).normalized;

            if (_playerJump.IsGrounded)
            {
                // 목표 속도에 도달하기 위해 속도 변화량 계산.
                Vector3 targetVelocity = _targetDirection * moveSpeed;
                Vector3 velocityChange = (targetVelocity - new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z));

                // 한 프레임에 가해지는 힘 최대치 제한.
                velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
                velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
                velocityChange.y = 0; // Y축은 0으로.

                _rb.AddForce(velocityChange, ForceMode.VelocityChange);
            }
        }

        /// <summary>
        /// 캐릭터가 이동하는 방향을 부드럽게 바라보도록 회전을 처리.
        /// </summary>
        private void HandleRotation()
        {
            // 입력 없을 시 리턴.
            if (_targetDirection == Vector3.zero) return;

            // Atan2 함수를 사용하여 월드 좌표 기준의 3D 목표 방향을 Y축 회전 각도로 변환.
            float targetAngle = Mathf.Atan2(_targetDirection.x, _targetDirection.z) * Mathf.Rad2Deg;

            // 현재 각도에서 목표 각도까지 rotationSmoothTime에 걸쳐 부드럽게 회전하는 중간 각도를 계산.
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _targetRotationVelocity, rotationSmoothTime);

            // 계산된 최종 각도를 사용하여 캐릭터의 회전을 적용.
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }
    }
}