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
        private bool _isParryable;

        private Rigidbody _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        /// <summary>
        /// Effect로부터 최종 계산된 능력치를 받아 투사체를 초기화.
        /// </summary>
        public void Initialize(float damage, float speed, float lifetime, bool isParryable)
        {
            _damage = damage;
            _speed = speed;
            _isParryable = isParryable;
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
            // 패링 존과 충돌 확인
            if (other.TryGetComponent<IParryable>(out var parryableObject))
            {
                // 이 투사체가 패링 가능하다면, 패링 로직을 실행.
                if (_isParryable)
                {
                    if (parryableObject.OnParrySuccess())
                    {
                        Destroy(gameObject);
                    }
                }
                return;
            }

            // 플레이어나 보스 충돌 확인
            if (other.TryGetComponent<Health>(out var targetHealth))
            {
                targetHealth.TakeDamage(_damage);
                Destroy(gameObject);
            }
            // 그 외 충돌
            else
            {
                Destroy(gameObject);
            }
        }
    }
}