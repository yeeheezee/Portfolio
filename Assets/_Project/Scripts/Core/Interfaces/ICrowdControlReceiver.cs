namespace WizardBrawl.Core
{
    /// <summary>
    /// CC 효과를 적용받는 대상이 구현해야 하는 계약.
    /// </summary>
    public interface ICrowdControlReceiver
    {
        /// <summary>
        /// CC 효과를 적용함.
        /// </summary>
        /// <param name="controlType">CC 타입.</param>
        /// <param name="duration">지속 시간(초).</param>
        /// <param name="strength">강도(정규화 값).</param>
        void ApplyCrowdControl(CrowdControlType controlType, float duration, float strength);
    }
}
