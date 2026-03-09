using UnityEngine;
using WizardBrawl.Core;
using WizardBrawl.Magic.Effects;

namespace WizardBrawl.Magic.Data
{
    /// <summary>
    /// 다중 갈래(팬샷) 투사체 마법 데이터.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFanProjectileMagic", menuName = "Magic/Fan Projectile Magic Data")]
    public class FanProjectileMagicData : MagicData
    {
        [Header("투사체 설정")]
        [Tooltip("발사될 투사체 프리팹.")]
        [SerializeField] private GameObject _projectilePrefab;

        [Tooltip("투사체 기본 공격력.")]
        [SerializeField] private int _damage = 10;

        [Tooltip("투사체 이동 속도.")]
        [SerializeField] private float _speed = 14f;

        [Tooltip("투사체 최대 생존 시간.")]
        [SerializeField] private float _lifetime = 3f;

        [Header("팬샷 설정")]
        [Tooltip("발사 투사체 개수.")]
        [SerializeField] private int _projectileCount = 3;

        [Tooltip("팬샷 전체 각도(도).")]
        [SerializeField] private float _fanAngle = 18f;

        [Tooltip("궁극기 체인 판정용 투사체 여부.")]
        [SerializeField] private bool _isUltimateHit;

        public GameObject ProjectilePrefab => _projectilePrefab;
        public int Damage => _damage;
        public float Speed => _speed;
        public float Lifetime => _lifetime;
        public int ProjectileCount => _projectileCount;
        public float FanAngle => _fanAngle;
        public bool IsUltimateHit => _isUltimateHit;

        public override IMagicEffect CreateEffect()
        {
            return new FanProjectileEffect(this);
        }
    }
}
