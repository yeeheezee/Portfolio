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
        private float _explodeAtTime;
        private bool _isParryable;
        private ElementType _parryElement = ElementType.None;
        private bool _hasExploded;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
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
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasExploded || _data == null)
            {
                return;
            }

            if (_owner != null && other.transform.root == _owner.transform.root)
            {
                return;
            }

            if (_isParryable && other.TryGetComponent<IParryable>(out var parryableObject))
            {
                if (parryableObject.OnParrySuccess(_parryElement))
                {
                    _hasExploded = true;
                    Destroy(gameObject);
                }

                return;
            }

            int targetLayerMask = 1 << other.gameObject.layer;
            bool isTargetLayer = (_data.TargetLayers.value & targetLayerMask) != 0;
            if (isTargetLayer && other.TryGetComponent<IStatusReceiver>(out IStatusReceiver statusReceiver))
            {
                statusReceiver.ApplyStatus(StatusEvent.CreateDebuff(_data.DebuffType, _data.Duration, _data.Magnitude, _owner));
                Debug.Log($"[DebuffMissile] direct_hit target={other.name} type={_data.DebuffType}");
            }

            Explode("collision");
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

                if (target.TryGetComponent<IStatusReceiver>(out IStatusReceiver statusReceiver))
                {
                    statusReceiver.ApplyStatus(StatusEvent.CreateCrowdControl(CrowdControlType.Slow, _data.BurstSlowDuration, _data.BurstSlowStrength, _owner));
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
