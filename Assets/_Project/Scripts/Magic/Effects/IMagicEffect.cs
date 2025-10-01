using UnityEngine;

namespace WizardBrawl.Magic.Effects
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
        void Execute(GameObject caster);
    }
}