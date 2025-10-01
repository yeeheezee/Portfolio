using UnityEngine;
using System.Collections.Generic;
using WizardBrawl.Magic.Data;

namespace WizardBrawl.Magic
{
    /// <summary>
    /// 스킬 시전과 쿨타임 관리 기능을 제공.
    /// 추상 클래스.
    /// </summary>
    public abstract class BaseCaster : MonoBehaviour
    {
        [Header("발사 위치")]
        [SerializeField]
        protected Transform _magicSpawnPoint;
        public Transform MagicSpawnPoint => _magicSpawnPoint;

        private Dictionary<MagicData, float> _cooldownTimers = new Dictionary<MagicData, float>();

        /// <summary>
        /// 지정된 마법 스킬을 사용하고 쿨타임을 기록함.
        /// 자식 클래스에서 이 기능을 호출하여 스킬을 사용함.
        /// </summary>
        protected void UseSkill(MagicData skill)
        {
            // 스킬이 없거나 쿨타임이 차지 않았으면 실행 중단.
            if (skill == null)
            {
                Debug.LogError("시도한 스킬(MagicData)이 할당되지 않았습니다!"); // 로그 추가
                return;
            }
            if (!IsSkillReady(skill))
            {
                Debug.LogWarning("스킬 쿨타임이 아직 차지 않았습니다!"); // 로그 추가
                return;
            }

            // Effect 생성 및 실행
            skill.CreateEffect().Execute(gameObject);
            // 쿨타임 기록
            _cooldownTimers[skill] = Time.time;

            Debug.Log($"'{gameObject.name}'이(가) 스킬 '{skill.MagicName}' 사용");
        }

        /// <summary>
        /// 지정된 마법 스킬의 쿨타임 체크.
        /// 외부에서도 쿨타임 상태를 확인할 수 있도록 public으로 설정.
        /// </summary>
        public bool IsSkillReady(MagicData skill)
        {
            if (skill == null) return false;

            // 딕셔너리에서 스킬의 마지막 사용 시간을 찾아옴. 사용한 적 없으면 0을 반환.
            _cooldownTimers.TryGetValue(skill, out float lastUsedTime);

            // 현재 시간이 (마지막 사용 시간 + 쿨타임)보다 크거나 같으면 준비 완료.
            return Time.time >= lastUsedTime + skill.Cooldown;
        }
    }
}