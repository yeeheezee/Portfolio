using UnityEngine;
using WizardBrawl.Core;
using WizardBrawl.Magic.Data;

namespace WizardBrawl.Magic.Effects
{
    /// <summary>
    /// 팬샷(다중 갈래 투사체) 실행 로직.
    /// </summary>
    public class FanProjectileEffect : IMagicEffect
    {
        private readonly FanProjectileMagicData _data;

        public FanProjectileEffect(FanProjectileMagicData data)
        {
            _data = data;
        }

        public void Execute(GameObject caster, Transform spawnPoint, Vector3 fireDirection, Vector3 targetPoint)
        {
            if (_data.ProjectilePrefab == null)
            {
                Debug.LogWarning($"마법 '{_data.MagicName}'에 투사체 프리팹이 설정되지 않았습니다.");
                return;
            }

            Vector3 baseDirection = fireDirection.sqrMagnitude <= 0.0001f ? spawnPoint.forward : fireDirection.normalized;
            int count = Mathf.Max(1, _data.ProjectileCount);
            float angle = Mathf.Max(0f, _data.FanAngle);
            int firedCount = 0;

            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : (float)i / (count - 1);
                float yaw = Mathf.Lerp(-angle * 0.5f, angle * 0.5f, t);
                Vector3 shotDirection = Quaternion.AngleAxis(yaw, Vector3.up) * baseDirection;

                GameObject projectileGO = Object.Instantiate(
                    _data.ProjectilePrefab,
                    spawnPoint.position,
                    Quaternion.LookRotation(shotDirection)
                );

                if (!projectileGO.TryGetComponent<MagicMissile>(out MagicMissile projectile))
                {
                    Debug.LogError($"'{_data.ProjectilePrefab.name}' 프리팹에 MagicMissile 컴포넌트가 없습니다.");
                    Object.Destroy(projectileGO);
                    continue;
                }

                projectile.Initialize(
                    _data.Damage,
                    _data.Speed,
                    _data.Lifetime,
                    _data.IsParryable,
                    _data.ParryElement,
                    _data.IsUltimateHit,
                    caster
                );
                projectile.Launch(shotDirection);
                firedCount++;
            }

            Debug.Log($"[FanProjectileEffect] fired={firedCount}, count={count}, fanAngle={angle:0.0}");
        }
    }
}
