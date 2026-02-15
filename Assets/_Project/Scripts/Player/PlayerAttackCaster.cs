using UnityEngine;
using WizardBrawl.Core;
using WizardBrawl.Magic;
using WizardBrawl.Magic.Data;

namespace WizardBrawl.Player
{
    /// <summary>
    /// 플레이어의 공격 입력을 받아 마법을 시전함.
    /// </summary>
    public class PlayerAttackCaster : BaseCaster
    {
        [Header("마법 슬롯")]
        [Tooltip("주 공격으로 사용할 마법의 데이터 에셋.")]
        [SerializeField] private MagicData _primaryAttackMagic;

        [Header("조합 마법 슬롯")]
        [Tooltip("RR 조합 성공 시 사용할 마법 데이터.")]
        [SerializeField] private MagicData _rrMagic;

        [Tooltip("BB 조합 성공 시 사용할 마법 데이터.")]
        [SerializeField] private MagicData _bbMagic;

        [Tooltip("YY 조합 성공 시 사용할 마법 데이터.")]
        [SerializeField] private MagicData _yyMagic;

        [Tooltip("RB 조합 성공 시 사용할 마법 데이터.")]
        [SerializeField] private MagicData _rbMagic;

        [Tooltip("RY 조합 성공 시 사용할 마법 데이터.")]
        [SerializeField] private MagicData _ryMagic;

        [Tooltip("BY 조합 성공 시 사용할 마법 데이터.")]
        [SerializeField] private MagicData _byMagic;

        [Header("메인 카메라")]
        [Tooltip("마법을 시전할 방향을 정할 메인카메라")]
        [SerializeField] private Camera _mainCamera;

        private readonly ElementCombinationResolver _combinationResolver = new ElementCombinationResolver();
        private PlayerElementSlot _elementSlot;

        protected override void Awake()
        {
            base.Awake();
            _elementSlot = GetComponent<PlayerElementSlot>();

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
        }

        /// <summary>
        /// 주 공격 마법을 시전함.
        /// </summary>
        public void PerformAttack()
        {
            if (_mainCamera == null)
            {
                Debug.LogError("메인 카메라가 할당되지 않았습니다!", this);
                return;
            }

            MagicData selectedMagic = _primaryAttackMagic;
            if (_elementSlot != null && _combinationResolver.TryResolve(_elementSlot.CurrentState, out ElementCombinationType combo))
            {
                selectedMagic = SelectMagicByCombination(combo);
                Debug.Log($"[ElementCombo] success: {combo}");
            }
            else
            {
                Debug.Log("[ElementCombo] fail: fallback to primary");
            }

            UseSkill(selectedMagic, _mainCamera.transform.forward);
        }

        private MagicData SelectMagicByCombination(ElementCombinationType combinationType)
        {
            MagicData magic = combinationType switch
            {
                ElementCombinationType.RR => _rrMagic,
                ElementCombinationType.BB => _bbMagic,
                ElementCombinationType.YY => _yyMagic,
                ElementCombinationType.RB => _rbMagic,
                ElementCombinationType.RY => _ryMagic,
                ElementCombinationType.BY => _byMagic,
                _ => _primaryAttackMagic
            };

            return magic == null ? _primaryAttackMagic : magic;
        }
    }
}
