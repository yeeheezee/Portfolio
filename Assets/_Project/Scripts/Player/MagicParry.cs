using UnityEngine;
using System.Collections;
using WizardBrawl.Core;

namespace WizardBrawl.Player
{
    /// <summary>
    /// 플레이어의 마나 패링 시스템을 담당.
    /// </summary>
    public class MagicParry : MonoBehaviour, IParryable
    {
        [Header("패링 설정")]

        [Tooltip("패링 판정이 활성화되는 시간 (초)")]
        [SerializeField] 
        private float _parryWindow = 0.3f;

        [Tooltip("패링 시도 시 소모되는 마나량")]
        [SerializeField] 
        private float _manaCost = 10f;

        [Tooltip("패링 성공 시 회복되는 마나량")]
        [SerializeField] 
        private float _manaGainOnSuccess = 30f;

        private Collider _parryCollider;
        private Mana _playerMana;
        private bool _isParrying = false;

        private void Awake()
        {
            _parryCollider = GetComponent<Collider>();
            _playerMana = GetComponentInParent<Mana>();

            if (_parryCollider == null || _playerMana == null)
            {
                Debug.LogError("필수 컴포넌트(Collider, Mana)를 찾을 수 없습니다!", this);
                enabled = false; // 컴포넌트 비활성화
                return;
            }

            _parryCollider.enabled = false;
        }

        /// <summary>
        /// 외부(입력 시스템)에서 호출하여 패링을 시도하는 메서드.
        /// </summary>
        public void AttemptParry()
        {
            //패링이 불가능하거나 마나가 충분하지 않으면
            if (_isParrying || !_playerMana.IsManaAvailable(_manaCost)) return;

            StartCoroutine(ParryCoroutine());
        }
        private IEnumerator ParryCoroutine()
        {
            _isParrying = true;
            _playerMana.UseMana(_manaCost);

            _parryCollider.enabled = true;
            yield return new WaitForSeconds(_parryWindow);
            _parryCollider.enabled = false;

            _isParrying = false;
        }

        /// <summary>
        /// 투사체(MagicMissile)가 호출하는, 패링 성공 시 처리 메서드.
        /// </summary>
        public bool OnParrySuccess()
        {
            Debug.Log("패링 성공! 마나를 회복합니다.");
            _playerMana.RestoreMana(_manaGainOnSuccess);

            return true;
        }
    }
}