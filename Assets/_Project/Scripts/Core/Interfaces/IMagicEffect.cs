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
        void Execute(GameObject caster, Vector3 fireDirection);
        /// <param name="spawnPoint">마법이 생성될 위치.</param>
        /// <param name="fireDirection">마법이 발사될 방향.</param>
    }
}