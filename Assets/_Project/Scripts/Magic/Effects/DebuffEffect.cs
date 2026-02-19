using UnityEngine;
using WizardBrawl.Core;
using WizardBrawl.Magic;
using WizardBrawl.Magic.Data;

namespace WizardBrawl.Magic.Effects
{
    /// <summary>
    /// 디버프 전용 투사체를 생성하고 발사하는 실행 로직.
    /// </summary>
    public class DebuffEffect : IMagicEffect
    {
        private readonly DebuffMagicData _data;

        /// <summary>
        /// 디버프 실행에 사용할 데이터를 주입함.
        /// </summary>
        public DebuffEffect(DebuffMagicData data)
        {
            _data = data;
        }

        /// <summary>
        /// 디버프 투사체를 생성하고 초기화 후 발사함.
        /// </summary>
        public void Execute(GameObject caster, Transform spawnPoint, Vector3 fireDirection)
        {
            if (_data.ProjectilePrefab == null)
            {
                Debug.LogWarning($"마법 '{_data.MagicName}'에 디버프 투사체 프리팹이 설정되지 않았습니다.");
                return;
            }

            GameObject projectileGO = Object.Instantiate(
                _data.ProjectilePrefab,
                spawnPoint.position,
                Quaternion.LookRotation(fireDirection)
            );

            if (!projectileGO.TryGetComponent<DebuffMissile>(out var projectile))
            {
                if (projectileGO.TryGetComponent<MagicMissile>(out var magicMissile))
                {
                    magicMissile.enabled = false;
                }

                projectile = projectileGO.AddComponent<DebuffMissile>();
                Debug.LogWarning($"'{_data.ProjectilePrefab.name}' 프리팹에 DebuffMissile이 없어 런타임에서 자동 추가함.");
            }

            projectile.Initialize(_data, caster);
            projectile.Launch(fireDirection);
        }
    }
}
