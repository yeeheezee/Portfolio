using UnityEngine;
using System.Collections;
using WizardBrawl.Core;

namespace WizardBrawl.Player
{
    /// <summary>
    /// 플레이어의 마법 패링 시스템. IParryable을 구현함.
    /// </summary>
    public class MagicParry : MonoBehaviour, IParryable
    {
        [Header("패링 설정")]
        [Tooltip("패링 판정이 활성화되는 시간 (초)")]
        [SerializeField] private float _parryWindow = 0.3f;

        [Tooltip("패링 시도 시 소모되는 마나량")]
        [SerializeField] private float _manaCost = 10f;

        [Tooltip("패링 성공 시 회복되는 마나량")]
        [SerializeField] private float _manaGainOnSuccess = 30f;

        private Collider _parryCollider;
        private Mana _playerMana;
        private bool _isParrying;

        private void Awake()
        {
            _parryCollider = GetComponent<Collider>();
            _playerMana = GetComponentInParent<Mana>();

            if (_parryCollider == null || _playerMana == null)
            {
                Debug.LogError("MagicParry에 필수 컴포넌트(Collider, Mana)를 찾을 수 없습니다!", this);
                enabled = false;
                return;
            }
            _parryCollider.enabled = false;
        }

        /// <summary>
        /// 패링을 시도함.
        /// </summary>
        public void AttemptParry()
        {
            if (_isParrying || !_playerMana.IsManaAvailable(_manaCost)) return;
            StartCoroutine(ParryCoroutine());
        }

        /// <summary>
        /// IParryable 인터페이스 구현. 패링 성공 시 호출됨.
        /// </summary>
        /// <returns>패링 성공 시 투사체는 파괴되어야 하므로 true를 반환.</returns>
        public bool OnParrySuccess()
        {
            Debug.Log("패링 성공! 마나를 회복합니다.");
            _playerMana.RestoreMana(_manaGainOnSuccess);
            return true;
        }

        /// <summary>
        /// 정해진 시간 동안만 패링 판정을 활성화하는 코루틴.
        /// </summary>
        private IEnumerator ParryCoroutine()
        {
            _isParrying = true;
            _playerMana.UseMana(_manaCost);
            _parryCollider.enabled = true;
            yield return new WaitForSeconds(_parryWindow);
            _parryCollider.enabled = false;
            _isParrying = false;
        }
    }
}