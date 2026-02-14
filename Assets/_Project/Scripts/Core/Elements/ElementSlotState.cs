namespace WizardBrawl.Core
{
    /// <summary>
    /// 2슬롯 속성 상태를 표현하는 값 타입.
    /// </summary>
    public readonly struct ElementSlotState
    {
        public ElementSlotState(ElementType slotA, ElementType slotB)
        {
            SlotA = slotA;
            SlotB = slotB;
        }

        public ElementType SlotA { get; }
        public ElementType SlotB { get; }

        public override string ToString()
        {
            return $"[{SlotA},{SlotB}]";
        }
    }
}
