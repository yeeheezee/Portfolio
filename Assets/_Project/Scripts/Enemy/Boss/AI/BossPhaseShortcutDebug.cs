using UnityEngine;
using WizardBrawl.Core;

namespace WizardBrawl.Enemy
{
    /// <summary>
    /// TEST ONLY: 5 키 입력으로 보스 HP를 49%로 강제 조정하는 디버그 컴포넌트.
    /// 제출/배포 전 제거해야 함.
    /// </summary>
    public class BossPhaseShortcutDebug : MonoBehaviour
    {
        [Tooltip("키 입력을 받을 대상 체력 컴포넌트. 미지정 시 같은 오브젝트에서 자동 탐색.")]
        [SerializeField] private Health _health;

        private void Awake()
        {
            if (_health == null)
            {
                _health = GetComponent<Health>();
            }
        }

        private void Update()
        {
            if (_health == null)
            {
                return;
            }

            if (!Input.GetKeyDown(KeyCode.Alpha5) && !Input.GetKeyDown(KeyCode.Keypad5))
            {
                return;
            }

            float targetHealth = _health.MaxHealth * 0.49f;
            if (_health.CurrentHealth <= targetHealth)
            {
                Debug.Log($"[BossDebug] phase shortcut skipped: current={_health.CurrentHealth:0.0}, target={targetHealth:0.0}");
                return;
            }

            float damageToApply = _health.CurrentHealth - targetHealth;
            _health.TakeDamage(damageToApply);
            Debug.Log($"[BossDebug] phase shortcut applied: hp={_health.CurrentHealth:0.0}/{_health.MaxHealth:0.0} (49%)");
        }
    }
}
