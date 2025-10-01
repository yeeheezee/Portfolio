using UnityEngine;
using System;

namespace WizardBrawl.Core
{

    /// <summary>
    /// 체력을 가진 개체의 생명력 관리.
    /// </summary>
    public class Health : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("최대 체력 설정.")]
        private float _maxHealth = 100f;

        private bool _isDead = false;

        /// <summary>
        /// 최대 체력.
        /// </summary>
        public float MaxHealth { get => _maxHealth; private set => _maxHealth = value; }

        /// <summary>
        /// 현재 체력.
        /// </summary>
        public float CurrentHealth { get; private set; }

        /// <summary>
        /// 체력이 0이 되었을 때 호출되는 이벤트.
        /// </summary>
        public event Action OnDeath;

        private void Awake()
        {
            // 활성화 시 현재 체력을 최대 체력으로
            CurrentHealth = MaxHealth;
        }

        /// <summary>
        /// 지정된 양만큼의 피해를 입음.
        /// </summary>
        /// <param name="damageAmount">받을 피해량.</param>
        public void TakeDamage(float damageAmount)
        {
            // 사망 상태일 경우, 더 이상 피해를 받지 않도록 처리.
            if (_isDead) return;

            // 체력이 0 미만으로 내려가지 않도록 보정.
            CurrentHealth = Mathf.Max(CurrentHealth - damageAmount, 0f);

            Debug.Log($"{gameObject.name} took {damageAmount} damage. Current Health : {CurrentHealth}");

            // 체력이 0 이하가 되면 사망.
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

            // OnDeath 이벤트를 호출하여 사망을 통지.
            OnDeath?.Invoke();
        }
    }
}