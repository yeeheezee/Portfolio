using UnityEngine;
using WizardBrawl.Core;

namespace WizardBrawl.Magic
{
    /// <summary>
    /// 투사체 오브젝트의 로직. 충돌 처리를 담당함.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
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
        /// 발사 전, 투사체의 능력치를 설정함.
        /// </summary>
        /// <param name="damage">적용할 피해량.</param>
        /// <param name="speed">이동 속도.</param>
        /// <param name="lifetime">최대 생존 시간.</param>
        /// <param name="isParryable">패링 가능 여부.</param>
        public void Initialize(float damage, float speed, float lifetime, bool isParryable)
        {
            _damage = damage;
            _speed = speed;
            _isParryable = isParryable;
            Destroy(gameObject, lifetime);
        }

        /// <summary>
        /// 지정된 방향으로 투사체를 발사함.
        /// </summary>
        /// <param name="direction">발사할 방향 벡터.</param>
        public void Launch(Vector3 direction)
        {
            _rigidbody.linearVelocity = direction.normalized * _speed;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<IParryable>(out var parryableObject))
            {
                if (_isParryable)
                {
                    if (parryableObject.OnParrySuccess())
                    {
                        Destroy(gameObject);
                    }
                }
                return;
            }

            if (other.TryGetComponent<Health>(out var targetHealth))
            {
                targetHealth.TakeDamage(_damage);
                Destroy(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
            // 그 외의 모든 것(벽, 바닥 등)과 충돌 시 파괴
        }
    }
}