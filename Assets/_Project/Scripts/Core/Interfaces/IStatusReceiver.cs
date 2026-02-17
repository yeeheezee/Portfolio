namespace WizardBrawl.Core
{
    /// <summary>
    /// 상태 이벤트를 단일 진입점으로 수신하는 계약.
    /// </summary>
    public interface IStatusReceiver
    {
        /// <summary>
        /// 상태 이벤트를 수신하고 처리함.
        /// </summary>
        /// <param name="statusEvent">처리할 상태 이벤트.</param>
        void ApplyStatus(StatusEvent statusEvent);

        /// <summary>
        /// 궁극기 체인 강화 시간창이 열려 있는지 반환함.
        /// </summary>
        /// <returns>시간창이 열려 있으면 true.</returns>
        bool IsUltimateChainReady();
    }
}
