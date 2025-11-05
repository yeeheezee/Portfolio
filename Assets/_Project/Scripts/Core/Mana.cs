using UnityEngine;
using System; // Action 이벤트를 사용하기 위해 추가

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
            CurrentMana = _startMana;
        }

        /// <summary>
        /// 지정된 양만큼 마나가 충분한지 확인.
        /// </summary>
        /// <param name="amount">필요한 마나 양.</param>
        /// <returns>사용 가능하면 true, 아니면 false.</returns>
        public bool IsManaAvailable(float amount)
        {
            return CurrentMana >= amount;
        }

        /// <summary>
        /// 지정된 양만큼 마나를 소모함.
        /// </summary>
        /// <param name="amount">소모할 마나 양. 음수 값은 무시됨.</param>
        public void UseMana(float amount)
        {
            if (!IsManaAvailable(amount))
            {
                Debug.LogWarning("마나가 부족하여 스킬을 사용할 수 없습니다.");
                return;
            }

            CurrentMana -= amount;
            // 마나가 0 미만으로 내려가지 않도록 보정.
            if (CurrentMana < 0) CurrentMana = 0;

            // 마나 변경 이벤트 호출
            OnManaChanged?.Invoke(CurrentMana, _maxMana);
        }

        /// <summary>
        /// 지정된 양만큼 마나를 회복함.
        /// </summary>
        /// <param name="amount">회복할 마나 양. 음수 값은 무시됨.</param>
        public void RestoreMana(float amount)
        {
            CurrentMana += amount;
            if (CurrentMana > _maxMana) CurrentMana = _maxMana;

            OnManaChanged?.Invoke(CurrentMana, _maxMana);
        /// <summary>
        /// 마나 값을 안전하게 변경하고 이벤트를 호출함.
        /// </summary>
        /// <param name="newManaValue">설정할 새로운 마나 값.</param>
        }
    }
}