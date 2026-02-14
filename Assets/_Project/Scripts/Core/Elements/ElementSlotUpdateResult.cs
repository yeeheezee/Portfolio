namespace WizardBrawl.Core
{
    /// <summary>
    /// 슬롯 갱신 전/후 상태와 사유를 포함한 결과값.
    /// </summary>
    public readonly struct ElementSlotUpdateResult
    {
        public ElementSlotUpdateResult(ElementSlotState before, ElementSlotState after, string reason)
        {
            Before = before;
            After = after;
            Reason = reason;
        }

        public ElementSlotState Before { get; }
        public ElementSlotState After { get; }
        public string Reason { get; }
    }
}
