namespace WizardBrawl.Core
{
    /// <summary>
    /// 2슬롯 속성 상태를 표현하는 값 타입.
    /// </summary>
    public readonly struct ElementSlotState
    {
        /// <summary>
        /// 슬롯 상태 값을 생성함.
        /// </summary>
        public ElementSlotState(ElementType slotA, ElementType slotB)
        {
            SlotA = slotA;
            SlotB = slotB;
        }

        public ElementType SlotA { get; }
        public ElementType SlotB { get; }

        /// <summary>
        /// 로그 출력용 문자열 형태로 변환함.
        /// </summary>
        public override string ToString()
        {
            return $"[{SlotA},{SlotB}]";
        }
    }
}
