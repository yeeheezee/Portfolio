using UnityEngine;
using UnityEngine.InputSystem;


namespace WizardBrawl.Player
{
    /// <summary>
    /// 플레이어의 점프 기능을 담당하는 클래스.
    /// </summary>
    /// <remarks>
    /// 이 컴포넌트는 Rigidbody와 PlayerMovement 컴포넌트가 반드시 함께 존재해야 함.
    /// 입력은 OnJump 함수로 전달받음.
    /// </remarks>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerJump : MonoBehaviour
    {
        [Header("Jump Settings")]
        [Tooltip("점프 시 가해지는 힘의 크기")]
        [SerializeField] private float jumpForce = 12f;

        [Header("Ground Check")]
        [Tooltip("캐릭터의 발밑에서 땅을 감지할 레이캐스트의 시작점 오프셋")]
        [SerializeField] private Vector3 groundCheckOffset = new Vector3(0f, 0.1f, 0f);

        [Tooltip("레이캐스트 길이")]
        [SerializeField] private float groundCheckDistance = 0.2f;

        [Tooltip("땅으로 인식할 레이어 지정")]
        [SerializeField] private LayerMask groundLayer;

        /// <summary>
        /// 외부에서 이 캐릭터가 땅에 있는지 확인할 수 있는 프로퍼티.
        /// </summary>
        public bool IsGrounded { get; private set; }

        // --- Private Fields ---
        private Vector3 _startPoint; // 시작 지점 계산

        // --- Component References ---
        private Rigidbody _rb;
        private PlayerMovement _playerMovement;

        private void Awake()
        {
            // 컴포넌트 캐싱
            _rb = GetComponent<Rigidbody>();
            _playerMovement = GetComponent<PlayerMovement>();
        }

        private void FixedUpdate()
        {
            // 캐릭터의 위치에서 오프셋 계산하여 Raycast 시작 지점 계산.
            _startPoint = transform.position + groundCheckOffset;

            CheckIfGrounded();
        }

        /// <summary>
        /// Player Input의 Unity Event를 통해 수행
        /// </summary>
        public void PerformJump()
        {
            if (IsGrounded && _playerMovement.CanMove)
            {
                // 현재 속도에서 y만 제거
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);

                // 점프
                _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }

        private void CheckIfGrounded()
        {
            // 충돌했다면 true, 아니면 false 반환.
            IsGrounded = Physics.Raycast(_startPoint, Vector3.down, groundCheckDistance, groundLayer);
        }
        private void OnDrawGizmosSelected()
        {
            // 색상 설정
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            // IsGrounded 상태에 따라 기즈모 색상을 변경.
            Gizmos.color = IsGrounded ? transparentGreen : transparentRed;

            // Raycast 경로 그리기.
            Gizmos.DrawLine(_startPoint, _startPoint + Vector3.down * groundCheckDistance);
        }
    }
}