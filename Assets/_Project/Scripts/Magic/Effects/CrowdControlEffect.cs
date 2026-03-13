using System.Collections.Generic;
using UnityEngine;
using WizardBrawl.Core;
using WizardBrawl.Magic.Data;

namespace WizardBrawl.Magic.Effects
{
    /// <summary>
    /// 범위 내 대상에게 CC를 적용하는 실행 로직.
    /// </summary>
    public class CrowdControlEffect : IMagicEffect
    {
        private readonly CrowdControlMagicData _data;

        /// <summary>
        /// CC 실행에 사용할 데이터를 주입함.
        /// </summary>
        public CrowdControlEffect(CrowdControlMagicData data)
        {
            _data = data;
        }

        /// <summary>
        /// 지정 범위의 대상에게 CC 상태 이벤트를 적용함.
        /// </summary>
        public void Execute(GameObject caster, Transform spawnPoint, Vector3 fireDirection, Vector3 targetPoint)
        {
            SpawnVfx(targetPoint);

            Collider[] hits = Physics.OverlapSphere(targetPoint, _data.Radius, _data.TargetLayers);
            int appliedCount = 0;
            HashSet<Transform> processedRoots = new HashSet<Transform>();

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (hit == null)
                {
                    continue;
                }

                GameObject target = hit.gameObject;
                Transform root = target.transform.root;
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
                    receiver.ApplyStatus(StatusEvent.CreateCrowdControl(_data.ControlType, _data.Duration, _data.Strength, caster));
                    Debug.Log(
                        $"[CrowdControlEffect] target={target.name} root={root.name} " +
                        $"type={_data.ControlType} duration={_data.Duration:0.00} strength={_data.Strength:0.00}");
                    appliedCount++;
                }
            }

            Debug.Log($"[CrowdControlEffect] type={_data.ControlType}, applied={appliedCount}");
        }

        private void SpawnVfx(Vector3 targetPoint)
        {
            if (_data.CcVfxPrefab == null)
            {
                return;
            }

            GameObject vfx = Object.Instantiate(_data.CcVfxPrefab, targetPoint, Quaternion.identity);
            float lifetime = Mathf.Max(0.05f, _data.CcVfxLifetime);
            Object.Destroy(vfx, lifetime);
        }
    }
}
