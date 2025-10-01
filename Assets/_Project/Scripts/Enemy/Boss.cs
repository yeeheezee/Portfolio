using WizardBrawl.Core;
using UnityEngine;

namespace WizardBrawl.Enemy
{
    /// <summary>
    /// 보스 개체의 전반적인 로직을 관리함.
    /// </summary>
    [RequireComponent(typeof(Health))] // Health 컴포넌트가 반드시 필요함을 명시.
    public class Boss : MonoBehaviour
    {
        private Health _health;

        /// <summary>
        /// 필요한 컴포넌트 참조를 초기화함.
        /// </summary>
        private void Awake()
        {
            // 자신의 게임 오브젝트에 붙어있는 Health 컴포넌트를 가져옴.
            _health = GetComponent<Health>();
        }

        /// <summary>
        /// 이 컴포넌트가 활성화될 때 이벤트 구독을 설정.
        /// </summary>
        private void OnEnable()
        {
            // Health 컴포넌트의 OnDeath 이벤트에 HandleDeath 메서드를 구독(연결)함.
            _health.OnDeath += HandleDeath;
        }

        /// <summary>
        /// 이 컴포넌트가 비활성화될 때 이벤트 구독을 해제.
        /// </summary>
        private void OnDisable()
        {
            // 구독했던 HandleDeath 메서드를 OnDeath 이벤트에서 구독 해제함.
            _health.OnDeath -= HandleDeath;
        }

        /// <summary>
        /// Health.OnDeath 이벤트가 발생했을 때 호출될 메서드.
        /// </summary>
        private void HandleDeath()
        {
            Debug.Log("보스가 처치됨. 오브젝트를 파괴합니다.");

            // 자신의 게임 오브젝트를 파괴함.
            Destroy(gameObject);
        }
    }
}