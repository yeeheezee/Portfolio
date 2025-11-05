using UnityEngine;
using UnityEngine.InputSystem;


namespace WizardBrawl.Player
{
    /// <summary>
    /// 플레이어의 점프 기능과 지면 상태(Grounded)를 관리함.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerJump : MonoBehaviour
    {
        [Header("Jump Settings")]
        [Tooltip("점프 시 가해지는 힘의 크기")]
        [SerializeField] private float jumpForce = 12f;

        [Header("Ground Check")]
        [Tooltip("땅 감지를 위한 Raycast의 시작점 오프셋")]
        [SerializeField] private Vector3 groundCheckOffset = new Vector3(0f, 0.1f, 0f);

        [Tooltip("땅을 감지할 Raycast의 최대 거리")]
        [SerializeField] private float groundCheckDistance = 0.2f;

        [Tooltip("땅으로 인식할 레이어")]
        [SerializeField] private LayerMask groundLayer;

        /// <summary>
        /// 현재 캐릭터가 지면에 있는지 여부.
        /// </summary>
        public bool IsGrounded { get; private set; }

        private Rigidbody _rb;
        private PlayerMovement _playerMovement;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _playerMovement = GetComponent<PlayerMovement>();
        }

        private void FixedUpdate()
        {
            CheckIfGrounded();
        }

        /// <summary>
        /// 점프를 시도함.
        /// </summary>
        public void PerformJump()
        {
            if (IsGrounded && _playerMovement.CanMove)
            {
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
                _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
        }

        /// <summary>
        /// 캐릭터 발밑으로 Raycast를 사용하여 지면과의 충돌을 확인하고 IsGrounded 상태를 갱신함.
        /// </summary>
        private void CheckIfGrounded()
        {
            Vector3 startPoint = transform.position + groundCheckOffset;
            IsGrounded = Physics.Raycast(startPoint, Vector3.down, groundCheckDistance, groundLayer);
        }

        /// <summary>
        /// Scene 뷰에서 지면 감지 Raycast를 시각적으로 표시함.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Vector3 startPoint = transform.position + groundCheckOffset;
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(startPoint, startPoint + Vector3.down * groundCheckDistance);
        }
    }
}