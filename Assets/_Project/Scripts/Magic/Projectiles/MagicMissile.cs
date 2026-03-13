using System;
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
        private ElementType _parryElement = ElementType.None;
        private bool _isUltimateHit;
        private GameObject _owner;
        private Rigidbody _rigidbody;
        private SphereCollider _sphereCollider;
        private Vector3 _lastPosition;
        private bool _hasLaunched;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _sphereCollider = GetComponent<SphereCollider>();
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _lastPosition = transform.position;
        }

        /// <summary>
        /// 발사 전, 투사체의 능력치를 설정함.
        /// </summary>
        /// <param name="damage">적용할 피해량.</param>
        /// <param name="speed">이동 속도.</param>
        /// <param name="lifetime">최대 생존 시간.</param>
        /// <param name="isParryable">패링 가능 여부.</param>
        /// <param name="parryElement">패링 성공 시 플레이어가 획득할 속성.</param>
        public void Initialize(float damage, float speed, float lifetime, bool isParryable, ElementType parryElement, bool isUltimateHit, GameObject owner)
        {
            _damage = damage;
            _speed = speed;
            _isParryable = isParryable;
            _parryElement = parryElement;
            _isUltimateHit = isUltimateHit;
            _owner = owner;
            Destroy(gameObject, lifetime);
        }

        /// <summary>
        /// 지정된 방향으로 투사체를 발사함.
        /// </summary>
        /// <param name="direction">발사할 방향 벡터.</param>
        public void Launch(Vector3 direction)
        {
            _hasLaunched = true;
            _lastPosition = transform.position;
            _rigidbody.linearVelocity = direction.normalized * _speed;
        }

        private void Update()
        {
            if (!_hasLaunched)
            {
                return;
            }

            SweepForMissedCollision();
            _lastPosition = transform.position;
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleCollision(other);
        }

        private void SweepForMissedCollision()
        {
            Vector3 currentPosition = transform.position;
            Vector3 delta = currentPosition - _lastPosition;
            float distance = delta.magnitude;
            if (distance <= 0.0001f)
            {
                return;
            }

            float radius = _sphereCollider != null ? _sphereCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z) : 0.1f;
            RaycastHit[] hits = Physics.SphereCastAll(_lastPosition, radius, delta.normalized, distance, Physics.AllLayers, QueryTriggerInteraction.Collide);
            Array.Sort(hits, static (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                if (HandleCollision(hits[i].collider))
                {
                    return;
                }
            }
        }

        private bool HandleCollision(Collider other)
        {
            if (_owner != null && other.transform.root == _owner.transform.root)
            {
                return false;
            }

            Transform root = other.transform.root;
            bool hasParryHandler = other.TryGetComponent<IParryable>(out var parryableObject);
            bool hasStatusReceiver = root.TryGetComponent<IStatusReceiver>(out var statusReceiver);
            bool hasHealth = root.TryGetComponent<Health>(out var targetHealth);

            // Ignore passive trigger volumes such as camera/confiner helpers.
            if (other.isTrigger && !hasParryHandler && !hasStatusReceiver && !hasHealth)
            {
                return false;
            }

            Debug.Log(
                $"[MagicMissile] collision target={other.name} layer={other.gameObject.layer} " +
                $"root={other.transform.root.name} isTrigger={other.isTrigger} position={other.transform.position}");

            if (_isParryable && hasParryHandler)
            {
                if (parryableObject.OnParrySuccess(_parryElement)) Destroy(gameObject);
                return true;
            }

            if (hasStatusReceiver)
            {
                Debug.Log(
                    $"[MagicMissile] apply_damage_via_status targetRoot={root.name} damage={_damage:0.00} ultimate={_isUltimateHit}");
                statusReceiver.ApplyStatus(StatusEvent.CreateDamage(_damage, _isUltimateHit, _owner));
                Destroy(gameObject);
                return true;
            }

            if (hasHealth)
            {
                Debug.Log($"[MagicMissile] apply_damage_via_health targetRoot={root.name} damage={_damage:0.00}");
                targetHealth.TakeDamage(_damage);
                Destroy(gameObject);
                return true;
            }

            // 그 외의 모든 것(벽, 바닥 등)과 충돌 시 파괴
            Destroy(gameObject);
            return true;
        }
    }
}
