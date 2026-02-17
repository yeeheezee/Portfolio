using UnityEngine;
using WizardBrawl.Core;
using WizardBrawl.Magic.Effects;

namespace WizardBrawl.Magic.Data
{
    /// <summary>
    /// '투사체 발사' 유형의 마법 데이터.
    /// </summary>
    [CreateAssetMenu(fileName = "NewProjectileMagic", menuName = "Magic/Projectile Magic Data")]
    public class ProjectileMagicData : MagicData
    {
        [Header("투사체 전용 설정")]
        [Tooltip("발사될 투사체 프리팹.")]
        [SerializeField] private GameObject _projectilePrefab;

        [Tooltip("투사체의 기본 공격력.")]
        [SerializeField] private int _damage = 10;

        [Tooltip("투사체의 이동 속도.")]
        [SerializeField] private float _speed = 15f;

        [Tooltip("투사체의 최대 생존 시간.")]
        [SerializeField] private float _lifetime = 3f;

        [Tooltip("궁극기 체인 판정용 투사체 여부.")]
        [SerializeField] private bool _isUltimateHit;

        public GameObject ProjectilePrefab => _projectilePrefab;
        public int Damage => _damage;
        public float Speed => _speed;
        public float Lifetime => _lifetime;
        public bool IsUltimateHit => _isUltimateHit;

        /// <summary>
        /// 이 데이터에 맞는 ProjectileEffect 실행 로직을 생성하여 반환함.
        /// </summary>
        /// <returns>생성된 ProjectileEffect 인스턴스.</returns>
        public override IMagicEffect CreateEffect()
        {
            return new ProjectileEffect(this);
        }
    }
}
