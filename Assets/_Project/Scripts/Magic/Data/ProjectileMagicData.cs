using UnityEngine;
using WizardBrawl.Core;
using WizardBrawl.Magic.Effects;

namespace WizardBrawl.Magic.Data
{
    /// <summary>
    /// 투사체 발사 유형의 마법 데이터 정의.
    /// </summary>
    [CreateAssetMenu(fileName = "NewProjectileMagic", menuName = "Magic/Projectile Magic Data")]
    public class ProjectileMagicData : MagicData
    {
        [Header("투사체 전용 설정")]
        [SerializeField]
        [Tooltip("발사 투사체 프리팹.")]
        private GameObject _projectilePrefab;

        [SerializeField]
        [Tooltip("투사체 기본 공격력.")]
        private int _damage = 10;

        [SerializeField]
        [Tooltip("투사체 이동 속도.")]
        private float _speed = 15f;

        [SerializeField]
        [Tooltip("투사체 소멸 시간.")]
        private float _lifetime = 3f;

        // --- Public Properties ---
        public GameObject ProjectilePrefab => _projectilePrefab;
        public int Damage => _damage;
        public float Speed => _speed;
        public float Lifetime => _lifetime;

        /// <summary>
        /// 이 데이터에 맞는 ProjectileEffect 실행 로직을 생성하여 반환함.
        /// </summary>
        public override IMagicEffect CreateEffect()
        {
            return new ProjectileEffect(this);
        }
    }
}