using UnityEngine;
using WizardBrawl.Core;

namespace WizardBrawl.Magic.Data
{
    /// <summary>
    /// 모든 마법 데이터 관리를 위한 클래스
    /// </summary>
    public abstract class MagicData : ScriptableObject
    {
        [Header("공통 정보")]
        [SerializeField]
        [Tooltip("마법의 이름.")]
        private string _magicName = "New Magic";

        [SerializeField]
        [TextArea]
        [Tooltip("마법에 대한 설명.")]
        private string _description;

        [SerializeField]
        [Tooltip("UI에 표시될 마법 아이콘.")]
        private Sprite _icon;

        [Header("공통 능력치")]
        [SerializeField]
        [Tooltip("마법 시전에 소모되는 마나.")]
        private float _manaCost = 10f;

        [SerializeField]
        [Tooltip("마법 사용 후 재사용 대기시간.")]
        private float _cooldown = 1f;
        
        [SerializeField]
        [Tooltip("이 마법을 플레이어가 패링할 수 있는지 여부.")]
        private bool _isParryable = true;

        // --- Public Properties ---
        public string MagicName => _magicName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public float ManaCost => _manaCost;
        public float Cooldown => _cooldown;
        public bool IsParryable => _isParryable;

        /// <summary>
        /// 이 MagicData에 해당하는 마법 실행 로직
        /// </summary>
        /// <returns>생성된 마법 효과 인스턴스.</returns>
        public abstract IMagicEffect CreateEffect();
    }
}