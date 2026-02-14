namespace WizardBrawl.Core
{
    /// <summary>
    /// 2슬롯 속성 저장/교체 규칙을 담당하는 도메인 서비스.
    /// </summary>
    public sealed class ElementSlotService
    {
        private ElementType _slotA = ElementType.None;
        private ElementType _slotB = ElementType.None;

        public ElementSlotState CurrentState => new ElementSlotState(_slotA, _slotB);

        /// <summary>
        /// 패링으로 획득한 속성을 슬롯 규칙에 따라 반영함.
        /// </summary>
        /// <param name="elementType">획득한 속성.</param>
        /// <param name="reason">로그 사유.</param>
        /// <returns>갱신 결과.</returns>
        public ElementSlotUpdateResult Save(ElementType elementType, string reason)
        {
            ElementSlotState before = CurrentState;
            if (elementType == ElementType.None)
            {
                return new ElementSlotUpdateResult(before, before, reason);
            }

            if (_slotA == ElementType.None)
            {
                _slotA = elementType;
            }
            else if (_slotB == ElementType.None)
            {
                _slotB = elementType;
            }
            else
            {
                _slotA = _slotB;
                _slotB = elementType;
            }

            return new ElementSlotUpdateResult(before, CurrentState, reason);
        }

        /// <summary>
        /// 슬롯을 초기화함.
        /// </summary>
        /// <param name="reason">로그 사유.</param>
        /// <returns>초기화 결과.</returns>
        public ElementSlotUpdateResult Reset(string reason)
        {
            ElementSlotState before = CurrentState;
            _slotA = ElementType.None;
            _slotB = ElementType.None;
            return new ElementSlotUpdateResult(before, CurrentState, reason);
        }
    }
}
