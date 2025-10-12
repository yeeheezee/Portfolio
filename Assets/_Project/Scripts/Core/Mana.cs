using UnityEngine;
using System; // Action 이벤트를 사용하기 위해 추가

namespace WizardBrawl.Core
{
    /// <summary>
    /// 마나(자원) 관리를 위한 컴포넌트.
    /// </summary>
    public class Mana : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("최대 마나.")]
        private float _maxMana = 100f;
        [Tooltip("초기 마나.")]
        private float _startMana = 100f;

        // 현재 마나. 외부에서는 읽기만 가능하도록 private set 설정.
        public float CurrentMana { get; private set; }

        // 마나 변경 시 UI 등에 알리기 위한 이벤트. (현재 마나, 최대 마나)
        public event Action<float, float> OnManaChanged;

        private void Awake()
        {
            // 시작 시 마나를 최대로 설정.
            CurrentMana = _startMana;
        }

        /// <summary>
        /// 마나가 충분히 있는지 확인.
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
        /// <param name="amount">소모할 마나 양.</param>
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
        /// <param name="amount">회복할 마나 양.</param>
        public void RestoreMana(float amount)
        {
            CurrentMana += amount;
            // 마나가 최대치를 초과하지 않도록 보정.
            if (CurrentMana > _maxMana) CurrentMana = _maxMana;

            // 마나 변경 이벤트 호출
            OnManaChanged?.Invoke(CurrentMana, _maxMana);
        }
    }
}