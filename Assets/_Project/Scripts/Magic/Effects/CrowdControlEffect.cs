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
