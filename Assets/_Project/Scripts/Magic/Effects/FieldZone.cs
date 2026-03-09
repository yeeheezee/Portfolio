using System.Collections.Generic;
using UnityEngine;
using WizardBrawl.Core;
using WizardBrawl.Magic.Data;

namespace WizardBrawl.Magic.Effects
{
    /// <summary>
    /// 지속형 장판 틱 판정을 담당하는 런타임 컴포넌트.
    /// </summary>
    public class FieldZone : MonoBehaviour
    {
        private FieldMagicData _data;
        private GameObject _caster;
        private float _expireAt;
        private float _nextTickAt;

        public void Initialize(FieldMagicData data, GameObject caster)
        {
            _data = data;
            _caster = caster;
            _expireAt = Time.time + Mathf.Max(0.05f, _data.Duration);
            _nextTickAt = Time.time;
        }

        private void Update()
        {
            if (_data == null)
            {
                Destroy(gameObject);
                return;
            }

            if (Time.time >= _expireAt)
            {
                Destroy(gameObject);
                return;
            }

            if (Time.time < _nextTickAt)
            {
                return;
            }

            TickDamage();
            _nextTickAt = Time.time + Mathf.Max(0.05f, _data.TickInterval);
        }

        private void TickDamage()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, Mathf.Max(0.01f, _data.Radius), _data.TargetLayers);
            HashSet<Transform> processedRoots = new HashSet<Transform>();
            int appliedCount = 0;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (hit == null)
                {
                    continue;
                }

                Transform root = hit.transform.root;
                if (!processedRoots.Add(root))
                {
                    continue;
                }

                if (_caster != null && root == _caster.transform.root)
                {
                    continue;
                }

                GameObject target = hit.gameObject;
                if (target.TryGetComponent<IStatusReceiver>(out IStatusReceiver receiver))
                {
                    receiver.ApplyStatus(StatusEvent.CreateDamage(_data.DamagePerTick, _data.IsUltimateHit, _caster));
                    appliedCount++;
                    continue;
                }

                if (target.TryGetComponent<Health>(out Health health))
                {
                    health.TakeDamage(_data.DamagePerTick);
                    appliedCount++;
                }
            }

            Debug.Log($"[FieldZone] tick applied={appliedCount}, radius={_data.Radius:0.00}");
        }
    }
}
