using UnityEngine;

namespace WizardBrawl.Core
{
    /// <summary>
    /// 모든 마법 실행 로직 클래스가 구현해야 하는 인터페이스.
    /// </summary>
    public interface IMagicEffect
    {
        /// <summary>
        /// 마법 효과를 실행함.
        /// </summary>
        /// <param name="caster">마법을 시전하는 게임 오브젝트.</param>
        /// <param name="spawnPoint">마법이 생성될 위치.</param>
        /// <param name="fireDirection">마법이 발사될 방향.</param>
        /// <param name="targetPoint">마법이 적용되어야 하는 목표 지점.</param>
        void Execute(GameObject caster, Transform spawnPoint, Vector3 fireDirection, Vector3 targetPoint);
    }
}
