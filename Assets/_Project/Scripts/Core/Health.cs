using UnityEngine;
using System;

namespace WizardBrawl.Core
{
    /// <summary>
    /// 개체의 생명력(HP)을 관리하는 컴포넌트.
    /// </summary>
    public class Health : MonoBehaviour
    {
        [Header("체력 설정")]
        [Tooltip("인스펙터에서 설정할 최대 체력 초기값. 0보다 커야 합니다.")]
        [SerializeField] private float _initialMaxHealth = 100f;

        /// <summary>
        /// 이 개체의 최대 체력 값.
        /// </summary>
        public float MaxHealth { get; private set; }

        /// <summary>
        /// 현재 이 개체의 사망 상태인지 여부.
        /// </summary>
        public bool IsDead { get; private set; } = false;

        /// <summary>
        /// 현재 체력.
        /// </summary>
        public float CurrentHealth { get; private set; }

        /// <summary>
        /// 체력 변경 시 발생하는 이벤트. (현재 체력, 최대 체력)
        /// </summary>
        public event Action<float, float> OnHealthChanged;

        /// <summary>
        /// 체력이 0이 되어 사망 시 단 한 번 발생하는 이벤트.
        /// </summary>
        public event Action OnDeath;

        private void Awake()
        {
            if (_initialMaxHealth <= 0)
            {
                Debug.LogError("최대 체력 초기값(_initialMaxHealth)은 0보다 커야 합니다!", this);
                _initialMaxHealth = 100f;
            }
            MaxHealth = _initialMaxHealth;
            CurrentHealth = MaxHealth;
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        /// <summary>
        /// 지정된 양만큼 피해를 적용함.
        /// </summary>
        /// <param name="damageAmount">받을 피해량. 음수 값은 무시됨.</param>
        public void TakeDamage(float damageAmount)
        {
            if (IsDead || damageAmount < 0) return;
            SetHealth(CurrentHealth - damageAmount);
        }

        /// <summary>
        /// 지정된 양만큼 체력을 회복함.
        /// </summary>
        /// <param name="healAmount">회복할 체력량. 음수 값은 무시됨.</param>
        public void Heal(float healAmount)
        {
            if (IsDead || healAmount < 0) return;
            SetHealth(CurrentHealth + healAmount);
        }

        /// <summary>
        /// 체력 값을 안전하게 변경하고 관련 이벤트를 호출함.
        /// </summary>
        /// <param name="newHealthValue">설정할 새로운 체력 값.</param>
        private void SetHealth(float newHealthValue)
        {
            float newHealth = Mathf.Clamp(newHealthValue, 0f, MaxHealth);
            if (CurrentHealth != newHealth)
            {
                CurrentHealth = newHealth;
                OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            }

            if (CurrentHealth <= 0f && !IsDead)
            {
                Die();
            }
        }

        /// <summary>
        /// 사망 처리 로직.
        /// </summary>
        private void Die()
        {
            IsDead = true;
            OnDeath?.Invoke();
        }
    }
}