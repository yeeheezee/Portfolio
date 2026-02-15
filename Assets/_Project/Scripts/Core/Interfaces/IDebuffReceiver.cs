namespace WizardBrawl.Core
{
    /// <summary>
    /// 디버프 효과를 적용받는 대상이 구현해야 하는 계약.
    /// </summary>
    public interface IDebuffReceiver
    {
        /// <summary>
        /// 디버프 효과를 적용함.
        /// </summary>
        /// <param name="debuffType">디버프 타입.</param>
        /// <param name="duration">지속 시간(초).</param>
        /// <param name="magnitude">효과 크기(정규화 값).</param>
        void ApplyDebuff(DebuffType debuffType, float duration, float magnitude);
    }
}
