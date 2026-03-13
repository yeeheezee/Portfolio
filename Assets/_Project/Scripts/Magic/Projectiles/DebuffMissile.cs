using UnityEngine;
using WizardBrawl.Core;
using WizardBrawl.Magic.Data;

namespace WizardBrawl.Magic
{
    /// <summary>
    /// 직격 디버프와 만료/충돌 폭발 슬로우를 처리하는 전용 투사체.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class DebuffMissile : MonoBehaviour
    {
        private DebuffMagicData _data;
        private GameObject _owner;
        private Rigidbody _rigidbody;
        private SphereCollider _sphereCollider;
        private float _explodeAtTime;
        private bool _isParryable;
        private ElementType _parryElement = ElementType.None;
        private bool _hasExploded;
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
        /// 발사 전 디버프 투사체 설정값을 초기화함.
        /// </summary>
        public void Initialize(DebuffMagicData data, GameObject owner)
        {
            _data = data;
            _owner = owner;
            _explodeAtTime = Time.time + _data.ProjectileLifetime;
            _isParryable = _data.IsParryable;
            _parryElement = _data.ParryElement;
        }

        /// <summary>
        /// 지정 방향으로 디버프 투사체를 발사함.
        /// </summary>
        public void Launch(Vector3 direction)
        {
            _hasLaunched = true;
            _lastPosition = transform.position;
            _rigidbody.linearVelocity = direction.normalized * _data.ProjectileSpeed;
        }

        private void Update()
        {
            if (_hasExploded || _data == null)
            {
                return;
            }

            if (Time.time >= _explodeAtTime)
            {
                Explode("timeout");
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
            if (!_hasLaunched || distance <= 0.0001f)
            {
                return;
            }

            float radius = _sphereCollider != null ? _sphereCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z) : 0.1f;
            RaycastHit[] hits = Physics.SphereCastAll(_lastPosition, radius, delta.normalized, distance, Physics.AllLayers, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, static (a, b) => a.distance.CompareTo(b.distance));

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
            if (_hasExploded || _data == null)
            {
                return false;
            }

            if (_owner != null && other.transform.root == _owner.transform.root)
            {
                return false;
            }

            Transform root = other.transform.root;
            bool hasParryHandler = other.TryGetComponent<IParryable>(out var parryableObject);
            bool hasStatusReceiver = root.TryGetComponent<IStatusReceiver>(out IStatusReceiver statusReceiver);

            // Ignore passive trigger volumes such as camera/confiner helpers.
            if (other.isTrigger && !hasParryHandler && !hasStatusReceiver)
            {
                return false;
            }

            Debug.Log(
                $"[DebuffMissile] collision target={other.name} layer={other.gameObject.layer} " +
                $"root={other.transform.root.name} isTrigger={other.isTrigger} position={other.transform.position}");

            if (_isParryable && hasParryHandler)
            {
                if (parryableObject.OnParrySuccess(_parryElement))
                {
                    _hasExploded = true;
                    Destroy(gameObject);
                }

                return true;
            }

            int targetLayerMask = 1 << other.gameObject.layer;
            bool isTargetLayer = (_data.TargetLayers.value & targetLayerMask) != 0;
            if (isTargetLayer && hasStatusReceiver)
            {
                Debug.Log(
                    $"[DebuffMissile] apply_debuff targetRoot={root.name} type={_data.DebuffType} duration={_data.Duration:0.00} magnitude={_data.Magnitude:0.00}");
                statusReceiver.ApplyStatus(StatusEvent.CreateDebuff(_data.DebuffType, _data.Duration, _data.Magnitude, _owner));
                Debug.Log($"[DebuffMissile] direct_hit target={other.name} root={root.name} type={_data.DebuffType}");
            }

            Explode("collision");
            return true;
        }

        private void Explode(string reason)
        {
            if (_hasExploded)
            {
                return;
            }

            _hasExploded = true;

            Collider[] hits = Physics.OverlapSphere(transform.position, _data.BurstRadius, _data.TargetLayers);
            int appliedCount = 0;

            for (int i = 0; i < hits.Length; i++)
            {
                GameObject target = hits[i].gameObject;
                if (_owner != null && target.transform.root == _owner.transform.root)
                {
                    continue;
                }

                Transform hitRoot = target.transform.root;
                if (hitRoot.TryGetComponent<IStatusReceiver>(out IStatusReceiver statusReceiver))
                {
                    statusReceiver.ApplyStatus(StatusEvent.CreateCrowdControl(CrowdControlType.Slow, _data.BurstSlowDuration, _data.BurstSlowStrength, _owner));
                    Debug.Log(
                        $"[DebuffMissile] burst_apply_target root={hitRoot.name} duration={_data.BurstSlowDuration:0.00} strength={_data.BurstSlowStrength:0.00}");
                    appliedCount++;
                }
            }

            if (appliedCount > 0)
            {
                Debug.Log($"[DebuffMissile] burst_apply reason={reason}, count={appliedCount}");
            }
            else
            {
                Debug.Log($"[DebuffMissile] burst_no_target reason={reason}");
            }

            Destroy(gameObject);
        }
    }
}
