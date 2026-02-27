using UnityEngine;
using WizardBrawl.Core;

namespace WizardBrawl.Player
{
    /// <summary>
    /// Gameplay 이벤트를 Animator 파라미터로 전달하는 브리지.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class PlayerAnimationEventBridge : MonoBehaviour
    {
        [Header("Animator")]
        [Tooltip("이벤트를 전달할 Animator. 비어 있으면 하위에서 자동 탐색함.")]
        [SerializeField] private Animator _animator;

        private Health _health;
        private float _lastHealth = -1f;
        private bool _isInitialized;

        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int StunHash = Animator.StringToHash("Stun");
        private static readonly int DeathHash = Animator.StringToHash("Death");

        private void Awake()
        {
            _health = GetComponent<Health>();
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }
        }

        private void OnEnable()
        {
            if (_health == null)
            {
                return;
            }

            _health.OnHealthChanged += HandleHealthChanged;
            _health.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            if (_health == null)
            {
                return;
            }

            _health.OnHealthChanged -= HandleHealthChanged;
            _health.OnDeath -= HandleDeath;
        }

        private void HandleHealthChanged(float currentHealth, float maxHealth)
        {
            if (!_isInitialized)
            {
                _lastHealth = currentHealth;
                _isInitialized = true;
                return;
            }

            if (currentHealth < _lastHealth && currentHealth > 0f)
            {
                Trigger(HitHash);
            }

            _lastHealth = currentHealth;
        }

        private void HandleDeath()
        {
            Trigger(DeathHash);
        }

        public void TriggerStun()
        {
            Trigger(StunHash);
        }

        private void Trigger(int hash)
        {
            if (_animator == null)
            {
                return;
            }

            _animator.ResetTrigger(hash);
            _animator.SetTrigger(hash);
        }
    }
}
