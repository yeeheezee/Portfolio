using UnityEngine;

namespace WizardBrawl.Player
{
    /// <summary>
    /// 플레이어의 수평 이동과 회전을 담당함.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(PlayerJump))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("캐릭터의 지상 최대 이동 속도.")]
        [SerializeField] private float moveSpeed = 8f;

        [Tooltip("캐릭터가 목표 방향을 바라보는 데 걸리는 시간. 낮을수록 빠르게 회전.")]
        [SerializeField] private float rotationSmoothTime = 0.1f;

        [Tooltip("속도 변화의 최대치를 제한하여 물리력이 강하게 적용되는 것을 방지.")]
        [SerializeField] private float maxVelocityChange = 20.0f;

        /// <summary>
        /// 외부에서 움직임을 제어하기 위한 플래그. false 시 즉시 정지.
        /// </summary>
        public bool CanMove { get; set; } = true;

        private Vector2 _moveInput;
        private Vector3 _targetDirection;
        private float _targetRotationVelocity;

        private Rigidbody _rb;
        private Transform _mainCameraTransform;
        private PlayerJump _playerJump;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _mainCameraTransform = Camera.main.transform;
            _playerJump = GetComponent<PlayerJump>();
            _rb.freezeRotation = true;
        }

        /// <summary>
        /// 입력 시스템으로부터 이동 값을 받음.
        /// </summary>
        /// <param name="moveInput">2D 이동 입력 벡터.</param>
        public void SetMoveInput(Vector2 moveInput)
        {
            _moveInput = moveInput;
        }

        private void FixedUpdate()
        {
            if (!CanMove)
            {
                StopMovement();
                return;
            }

            CalculateTargetDirection();
            HandleRotation();

            if (_playerJump.IsGrounded)
            {
                ApplyGroundedMovement();
            }
            // TODO: 공중 이동 제어(Air Control) 로직 추가.
        }

        /// <summary>
        /// 현재 입력과 카메라 방향을 바탕으로 월드 좌표계의 목표 이동 방향을 계산함.
        /// </summary>
        private void CalculateTargetDirection()
        {
            Vector3 camForward = _mainCameraTransform.forward;
            Vector3 camRight = _mainCameraTransform.right;
            camForward.y = 0;
            camRight.y = 0;
            _targetDirection = (camForward.normalized * _moveInput.y + camRight.normalized * _moveInput.x).normalized;
        }

        /// <summary>
        /// 계산된 목표 방향으로 이동하도록 Rigidbody에 힘을 적용함.
        /// </summary>
        private void ApplyGroundedMovement()
        {
            Vector3 targetVelocity = _targetDirection * moveSpeed;
            Vector3 velocityChange = (targetVelocity - new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z));

            velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
            velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
            velocityChange.y = 0;

            _rb.AddForce(velocityChange, ForceMode.VelocityChange);
        }

        /// <summary>
        /// 캐릭터가 이동 방향을 부드럽게 바라보도록 회전시킴.
        /// </summary>
        private void HandleRotation()
        {
            if (_targetDirection == Vector3.zero) return;
            float targetAngle = Mathf.Atan2(_targetDirection.x, _targetDirection.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _targetRotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }

        /// <summary>
        /// 캐릭터의 수평 이동을 즉시 멈춤.
        /// </summary>
        private void StopMovement()
        {
            _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
        }
    }
}