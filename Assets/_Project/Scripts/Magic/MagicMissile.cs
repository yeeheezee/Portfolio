using UnityEngine;
using WizardBrawl.Core;

namespace WizardBrawl.Magic
{
    /// <summary>
    /// 가장 기본적인 투사체 마법
    /// 일직선으로 투사체를 발사해 적에게 데미지를 입힘.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class MagicMissile : MonoBehaviour
    {
        private float _speed;
        private float _damage;

        private Rigidbody _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        /// <summary>
        /// Effect로부터 최종 계산된 능력치를 받아 투사체를 초기화.
        /// </summary>
        public void Initialize(float damage, float speed, float lifetime)
        {
            _damage = damage;
            _speed = speed;
            // 지정된 시간이 지나면 이 게임 오브젝트를 파괴.
            Destroy(gameObject, lifetime);
        }

        /// <summary>
        /// 지정된 방향으로 투사체를 발사.
        /// </summary>
        public void Launch(Vector3 direction)
        {
            // Rigidbody의 속도를 설정하여 직선으로 날아가게 함.
            _rigidbody.linearVelocity = direction.normalized * _speed;
        }

        /// <summary>
        /// 다른 Collider와 충돌 시 호출.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            // Health 컴포넌트를 가진 대상에게 피해를 줌.
            if (other.TryGetComponent<Health>(out Health targetHealth))
            {
                targetHealth.TakeDamage(_damage);
            }

            // 충돌한 대상이 누구든지 즉시 파괴됨.
            Destroy(gameObject);
        }
    }
}