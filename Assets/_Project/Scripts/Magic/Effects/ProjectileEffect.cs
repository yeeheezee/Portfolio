using UnityEngine;
using WizardBrawl.Core;
using WizardBrawl.Magic.Data;

namespace WizardBrawl.Magic.Effects
{
    /// <summary>
    /// 투사체 마법의 실행 로직.
    /// </summary>
    public class ProjectileEffect : IMagicEffect
    {
        private readonly ProjectileMagicData _data;

        public ProjectileEffect(ProjectileMagicData data)
        {
            _data = data;
        }

        /// <summary>
        /// 투사체 프리팹을 생성하고 발사함.
        /// </summary>
        public void Execute(GameObject caster, Vector3 fireDirection)
        /// <param name="caster">마법을 시전한 주체.</param>
        /// <param name="spawnPoint">마법 효과가 시작될 위치와 방향.</param>
        /// <param name="fireDirection">마법이 나아갈 초기 방향.</param>
        {
            if (_data.ProjectilePrefab == null)
            {
                Debug.LogWarning($"마법 '{_data.MagicName}'에 투사체 프리팹이 설정되지 않았습니다.");
                return;
            }

            Transform spawnPoint = caster.transform.Find("MagicSpawnPoint");
            if (spawnPoint == null)
            {
                spawnPoint = caster.transform; // 못 찾으면 시전자 위치에서 발사.
            }

            GameObject projectileGO = Object.Instantiate(
                _data.ProjectilePrefab,
                spawnPoint.position,
                Quaternion.LookRotation(fireDirection)
            );

            if (projectileGO.TryGetComponent<MagicMissile>(out MagicMissile projectile))
            {
                projectile.Initialize(_data.Damage, _data.Speed, _data.Lifetime, _data.IsParryable);
                projectile.Launch(fireDirection);
            }
            else
            {
                Debug.LogError($"'{_data.ProjectilePrefab.name}' 프리팹에 MagicMissile 컴포넌트가 없습니다.");
            }
        }
    }
}