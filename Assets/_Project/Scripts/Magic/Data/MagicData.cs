using UnityEngine;
using WizardBrawl.Core;

namespace WizardBrawl.Magic.Data
{
    /// <summary>
    /// 모든 마법의 기본 데이터를 정의하는 추상 ScriptableObject.
    /// </summary>
    public abstract class MagicData : ScriptableObject
    {
        [Header("공통 정보")]
        [Tooltip("마법의 이름.")]
        [SerializeField] private string _magicName = "New Magic";

        [Tooltip("마법에 대한 설명.")]
        [TextArea][SerializeField] private string _description;

        [Tooltip("UI에 표시될 마법 아이콘.")]
        [SerializeField] private Sprite _icon;

        [Header("공통 능력치")]
        [Tooltip("마법 시전에 소모되는 마나.")]
        [SerializeField] private float _manaCost = 10f;

        [Tooltip("마법 사용 후 재사용 대기시간.")]
        [SerializeField] private float _cooldown = 1f;

        [Tooltip("이 마법을 플레이어가 패링할 수 있는지 여부.")]
        [SerializeField] private bool _isParryable = true;

        public string MagicName => _magicName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public float ManaCost => _manaCost;
        public float Cooldown => _cooldown;
        public bool IsParryable => _isParryable;

        /// <summary>
        /// 이 데이터에 맞는 마법 실행 로직(IMagicEffect)을 생성하여 반환.
        /// </summary>
        /// <returns>생성된 마법 효과 인스턴스.</returns>
        public abstract IMagicEffect CreateEffect();
    }
}