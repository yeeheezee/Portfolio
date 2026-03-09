using WizardBrawl.Core;
using UnityEngine;

namespace WizardBrawl.Enemy
{
    /// <summary>
    /// 보스의 생명 주기를 관리하고 사망 이벤트에 반응함.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Boss : MonoBehaviour
    {
        private Health _health;

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            _health.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            _health.OnDeath -= HandleDeath;
        }

        /// <summary>
        /// Health 컴포넌트로부터 사망 신호를 받았을 때 호출될 콜백.
        /// </summary>
        private void HandleDeath()
        {
            Debug.Log("보스가 처치됨. 오브젝트를 파괴합니다.");
            // TODO: GameManager에 게임 승리를 알리는 로직으로 대체.
            Destroy(gameObject);
        }
    }
}