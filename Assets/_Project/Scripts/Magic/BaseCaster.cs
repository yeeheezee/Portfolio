using UnityEngine;
using System.Collections.Generic;
using WizardBrawl.Magic.Data;
using WizardBrawl.Core;

namespace WizardBrawl.Magic
{
    /// <summary>
    /// 마법 시전의 공통 기능(쿨타임, 마나 소모)을 제공하는 추상 클래스.
    /// </summary>
    [RequireComponent(typeof(Mana))]
    public abstract class BaseCaster : MonoBehaviour
    {
        [Header("발사 위치")]
        [Tooltip("마법이 생성될 위치. 미지정 시 이 오브젝트의 Transform을 사용함.")]
        [SerializeField] protected Transform _magicSpawnPoint;

        /// <summary>
        /// 마법이 생성될 위치 Transform.
        /// </summary>
        public Transform MagicSpawnPoint => _magicSpawnPoint;

        private Mana _mana;
        private Dictionary<MagicData, float> _cooldownTimers = new Dictionary<MagicData, float>();

        protected virtual void Awake()
        {
            _mana = GetComponent<Mana>();
            if (_magicSpawnPoint == null)
            {
                _magicSpawnPoint = transform;
            }
        }

        /// <summary>
        /// 마법 스킬을 시전함.
        /// </summary>
        /// <param name="skill">사용할 마법 데이터.</param>
        /// <param name="fireDirection">마법 발사 방향.</param>
        protected void UseSkill(MagicData skill, Vector3 fireDirection)
        {
            Vector3 defaultTargetPoint = _magicSpawnPoint.position;
            TryUseSkill(skill, fireDirection, defaultTargetPoint);
        }

        /// <summary>
        /// 마법 스킬 시도를 수행하고 성공 여부를 반환함.
        /// </summary>
        protected bool TryUseSkill(MagicData skill, Vector3 fireDirection)
        {
            Vector3 defaultTargetPoint = _magicSpawnPoint.position;
            return TryUseSkill(skill, fireDirection, defaultTargetPoint);
        }

        /// <summary>
        /// 마법 스킬 시도를 수행하고 성공 여부를 반환함.
        /// </summary>
        protected bool TryUseSkill(MagicData skill, Vector3 fireDirection, Vector3 targetPoint)
        {
            if (skill == null)
            {
                Debug.LogError("시도한 스킬(MagicData)이 할당되지 않았습니다! 인스펙터를 확인해주세요.", this);
                return false;
            }

            if (!CanUseSkill(skill))
            {
                string failReason = ResolveSkillBlockReason(skill);
                Debug.Log($"[CastResult] use blocked: skill={skill.MagicName}, reason={failReason}");
                return false;
            }

            _mana.UseMana(skill.ManaCost);
            skill.CreateEffect().Execute(gameObject, _magicSpawnPoint, fireDirection, targetPoint);
            _cooldownTimers[skill] = Time.time;
            return true;
        }

        /// <summary>
        /// 지정된 마법의 쿨타임이 완료되었는지 확인.
        /// </summary>
        /// <param name="skill">확인할 마법 데이터.</param>
        /// <returns>사용 가능하면 true, 아니면 false.</returns>
        public bool IsSkillReady(MagicData skill)
        {
            if (skill == null) return false;
            _cooldownTimers.TryGetValue(skill, out float lastUsedTime);
            return Time.time >= lastUsedTime + skill.Cooldown;
        }

        /// <summary>
        /// 마나/쿨타임 기준으로 시전 가능 여부를 반환함.
        /// </summary>
        protected bool CanUseSkillNow(MagicData skill)
        {
            if (skill == null) return false;
            return CanUseSkill(skill);
        }

        /// <summary>
        /// 스킬 시전에 필요한 게임플레이 조건(쿨타임, 마나)이 충족되었는지 확인함.
        /// </summary>
        /// <param name="skill">검사할 마법 데이터.</param>
        /// <returns>사용 가능하면 true, 아니면 false.</returns>
        private bool CanUseSkill(MagicData skill)
        {
            if (!IsSkillReady(skill)) return false;
            if (!_mana.IsManaAvailable(skill.ManaCost)) return false;
            return true;
        }

        private string ResolveSkillBlockReason(MagicData skill)
        {
            if (skill == null)
            {
                return "null_skill";
            }

            if (!IsSkillReady(skill))
            {
                return "cooldown";
            }

            if (!_mana.IsManaAvailable(skill.ManaCost))
            {
                return "insufficient_mana";
            }

            return "unknown";
        }
    }
}
