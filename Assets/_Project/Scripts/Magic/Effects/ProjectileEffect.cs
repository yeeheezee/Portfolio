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
        /// 마법 효과 실행.
        /// </summary>
        public void Execute(GameObject caster)
        {
            // 프리팹이 설정되지 않았으면 경고 출력 후 종료.
            if (_data.ProjectilePrefab == null)
            {
                Debug.LogWarning($"마법 '{_data.MagicName}'에 투사체 프리팹이 미설정");
                return;
            }

            // 발사 위치를 찾음.
            Transform spawnPoint = caster.transform.Find("MagicSpawnPoint");
            if (spawnPoint == null)
            {
                spawnPoint = caster.transform; // 못 찾으면 시전자 위치에서 발사.
            }

            // 프리팹 인스턴스화.
            GameObject projectileGO = Object.Instantiate(
                _data.ProjectilePrefab,
                spawnPoint.position,
                spawnPoint.rotation
            );

            // 카메라 기준 발사 방향 계산.
            Vector3 fireDirection = Camera.main.transform.forward;

            // MagicProjectile 컴포넌트를 가져와 데이터로 초기화하고 발사.
            if (projectileGO.TryGetComponent<MagicMissile>(out MagicMissile projectile))
            {
                projectile.Initialize(_data.Damage, _data.Speed, _data.Lifetime);
                projectile.Launch(fireDirection);
            }
            else
            {
                Debug.LogError($"'{_data.ProjectilePrefab.name}' 프리팹에 MagicProjectile 스크립트 미설정");
            }
        }
    }
}