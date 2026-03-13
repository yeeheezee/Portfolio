using System.Collections.Generic;
using UnityEngine;
using WizardBrawl.Core;
using WizardBrawl.Magic.Data;

namespace WizardBrawl.Magic.Effects
{
    /// <summary>
    /// 직선 빔 즉시 판정 실행 로직.
    /// </summary>
    public class BeamEffect : IMagicEffect
    {
        private readonly BeamMagicData _data;

        public BeamEffect(BeamMagicData data)
        {
            _data = data;
        }

        public void Execute(GameObject caster, Transform spawnPoint, Vector3 fireDirection, Vector3 targetPoint)
        {
            Vector3 dir = fireDirection.sqrMagnitude <= 0.0001f ? spawnPoint.forward : fireDirection.normalized;
            SpawnVfx(spawnPoint, dir);

            RaycastHit[] hits = Physics.SphereCastAll(
                spawnPoint.position,
                Mathf.Max(0.01f, _data.Radius),
                dir,
                Mathf.Max(0.01f, _data.Range),
                _data.TargetLayers
            );

            int appliedCount = 0;
            HashSet<Transform> processedRoots = new HashSet<Transform>();
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i].collider;
                if (hit == null)
                {
                    continue;
                }

                Transform root = hit.transform.root;
                if (!processedRoots.Add(root))
                {
                    continue;
                }

                if (caster != null && root == caster.transform.root)
                {
                    continue;
                }

                if (root.TryGetComponent<IStatusReceiver>(out IStatusReceiver receiver))
                {
                    receiver.ApplyStatus(StatusEvent.CreateDamage(_data.Damage, _data.IsUltimateHit, caster));
                    appliedCount++;
                    continue;
                }

                if (root.TryGetComponent<Health>(out Health health))
                {
                    health.TakeDamage(_data.Damage);
                    appliedCount++;
                }
            }

            Debug.Log($"[BeamEffect] applied={appliedCount}, range={_data.Range:0.0}, radius={_data.Radius:0.00}");
        }

        private void SpawnVfx(Transform spawnPoint, Vector3 direction)
        {
            if (_data.BeamVfxPrefab == null)
            {
                return;
            }

            GameObject vfx = Object.Instantiate(
                _data.BeamVfxPrefab,
                spawnPoint.position,
                Quaternion.LookRotation(direction)
            );

            float lifetime = Mathf.Max(0.05f, _data.BeamVfxLifetime);
            Object.Destroy(vfx, lifetime);
        }
    }
}
