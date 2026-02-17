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

        public CrowdControlEffect(CrowdControlMagicData data)
        {
            _data = data;
        }

        public void Execute(GameObject caster, Transform spawnPoint, Vector3 fireDirection)
        {
            Collider[] hits = Physics.OverlapSphere(spawnPoint.position, _data.Radius, _data.TargetLayers);
            int appliedCount = 0;

            for (int i = 0; i < hits.Length; i++)
            {
                GameObject target = hits[i].gameObject;
                if (target == caster)
                {
                    continue;
                }

                if (target.TryGetComponent<IStatusReceiver>(out IStatusReceiver receiver))
                {
                    receiver.ApplyStatus(StatusEvent.CreateCrowdControl(_data.ControlType, _data.Duration, _data.Strength, caster));
                    appliedCount++;
                }
            }

            Debug.Log($"[CrowdControlEffect] type={_data.ControlType}, applied={appliedCount}");
        }
    }
}
