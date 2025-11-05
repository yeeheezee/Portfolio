using UnityEngine;
using System;

namespace WizardBrawl.Core
{
    /// <summary>
    /// 개체의 마나(자원)를 관리하는 컴포넌트.
    /// </summary>
    public class Mana : MonoBehaviour
    {
        [Header("마나 설정")]
        [Tooltip("인스펙터에서 설정할 최대 마나 초기값. 0보다 커야 합니다.")]
        [SerializeField] private float _initialMaxMana = 100f;

        [Tooltip("게임 시작 시 보유하고 있을 초기 마나.")]
        [SerializeField] private float _startMana = 100f;

        /// <summary>
        /// 이 개체의 최대 마나 값.
        /// </summary>
        public float MaxMana { get; private set; }

        /// <summary>
        /// 현재 마나.
        /// </summary>
        public float CurrentMana { get; private set; }

        /// <summary>
        /// 마나 변경 시 발생하는 이벤트. (현재 마나, 최대 마나)
        /// </summary>
        public event Action<float, float> OnManaChanged;

        private void Awake()
        {
            if (_initialMaxMana <= 0)
            {
                Debug.LogError("최대 마나 초기값(_initialMaxMana)은 0보다 커야 합니다!", this);
                _initialMaxMana = 100f;
            }
            MaxMana = _initialMaxMana;
            CurrentMana = Mathf.Clamp(_startMana, 0f, MaxMana);
        }

        private void Start()
        {
            OnManaChanged?.Invoke(CurrentMana, MaxMana);
        }

        /// <summary>
        /// 지정된 양만큼 마나가 충분한지 확인.
        /// </summary>
        /// <param name="amount">필요한 마나 양.</param>
        /// <returns>사용 가능하면 true, 아니면 false.</returns>
        public bool IsManaAvailable(float amount)
        {
            if (amount < 0) return false;
            return CurrentMana >= amount;
        }

        /// <summary>
        /// 지정된 양만큼 마나를 소모함.
        /// </summary>
        /// <param name="amount">소모할 마나 양. 음수 값은 무시됨.</param>
        public void UseMana(float amount)
        {
            if (amount < 0) return;
            SetMana(CurrentMana - amount);
        }

        /// <summary>
        /// 지정된 양만큼 마나를 회복함.
        /// </summary>
        /// <param name="amount">회복할 마나 양. 음수 값은 무시됨.</param>
        public void RestoreMana(float amount)
        {
            if (amount < 0) return;
            SetMana(CurrentMana + amount);
        }

        /// <summary>
        /// 마나 값을 안전하게 변경하고 이벤트를 호출함.
        /// </summary>
        /// <param name="newManaValue">설정할 새로운 마나 값.</param>
        private void SetMana(float newManaValue)
        {
            float newMana = Mathf.Clamp(newManaValue, 0f, MaxMana);
            if (CurrentMana != newMana)
            {
                CurrentMana = newMana;
                OnManaChanged?.Invoke(CurrentMana, MaxMana);
            }
        }
    }
}