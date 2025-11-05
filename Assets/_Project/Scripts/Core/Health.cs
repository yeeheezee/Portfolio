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

        private bool _isDead = false;
        /// <summary>
        /// 이 개체의 최대 체력 값.
        /// </summary>

        /// <summary>
        /// 현재 이 개체의 사망 상태인지 여부.
        /// </summary>
        public float MaxHealth { get => _maxHealth; private set => _maxHealth = value; }

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
            CurrentHealth = MaxHealth;
        }

        /// <summary>
        /// 지정된 양만큼 피해를 적용함.
        /// </summary>
        /// <param name="damageAmount">받을 피해량. 음수 값은 무시됨.</param>
        public void TakeDamage(float damageAmount)
        {
            // 사망 상태일 경우, 더 이상 피해를 받지 않도록 처리.
            if (_isDead) return;

            CurrentHealth = Mathf.Max(CurrentHealth - damageAmount, 0f);
        /// <summary>
        /// 지정된 양만큼 체력을 회복함.
        /// </summary>
        /// <param name="healAmount">회복할 체력량. 음수 값은 무시됨.</param>

        /// <summary>
        /// 체력 값을 안전하게 변경하고 관련 이벤트를 호출함.
        /// </summary>
        /// <param name="newHealthValue">설정할 새로운 체력 값.</param>

            if (CurrentHealth <= 0f)
            {
                Die();
            }
        }

        /// <summary>
        /// 사망 처리 로직.
        /// </summary>
        private void Die()
        {
            _isDead = true;
            Debug.Log($"{gameObject.name} has died.");

            OnDeath?.Invoke();
        }
    }
}