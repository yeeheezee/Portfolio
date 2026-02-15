using UnityEngine;
using WizardBrawl.Core;
using WizardBrawl.Magic.Data;

namespace WizardBrawl.Magic.Effects
{
    /// <summary>
    /// 범위 내 대상에게 디버프를 적용하는 실행 로직.
    /// </summary>
    public class DebuffEffect : IMagicEffect
    {
        private readonly DebuffMagicData _data;

        public DebuffEffect(DebuffMagicData data)
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

                if (target.TryGetComponent<IDebuffReceiver>(out IDebuffReceiver receiver))
                {
                    receiver.ApplyDebuff(_data.DebuffType, _data.Duration, _data.Magnitude);
                    appliedCount++;
                }
            }

            Debug.Log($"[DebuffEffect] type={_data.DebuffType}, applied={appliedCount}");
        }
    }
}
