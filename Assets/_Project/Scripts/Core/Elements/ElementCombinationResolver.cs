namespace WizardBrawl.Core
{
    /// <summary>
    /// 2슬롯 속성을 6조합으로 해석하는 도메인 서비스.
    /// </summary>
    public sealed class ElementCombinationResolver
    {
        /// <summary>
        /// 슬롯 상태를 조합 결과로 변환함.
        /// </summary>
        /// <param name="state">현재 슬롯 상태.</param>
        /// <param name="combinationType">해석 결과 조합.</param>
        /// <returns>유효 조합이면 true, 아니면 false.</returns>
        public bool TryResolve(ElementSlotState state, out ElementCombinationType combinationType)
        {
            return TryResolve(state.SlotA, state.SlotB, out combinationType);
        }

        /// <summary>
        /// 두 속성을 순서 미구분 규칙으로 조합 판정함.
        /// </summary>
        /// <param name="first">첫 번째 속성.</param>
        /// <param name="second">두 번째 속성.</param>
        /// <param name="combinationType">해석 결과 조합.</param>
        /// <returns>유효 조합이면 true, 아니면 false.</returns>
        public bool TryResolve(ElementType first, ElementType second, out ElementCombinationType combinationType)
        {
            combinationType = ElementCombinationType.None;

            if (first == ElementType.None || second == ElementType.None)
            {
                return false;
            }

            ElementType a = first <= second ? first : second;
            ElementType b = first <= second ? second : first;

            combinationType = (a, b) switch
            {
                (ElementType.R, ElementType.R) => ElementCombinationType.RR,
                (ElementType.B, ElementType.B) => ElementCombinationType.BB,
                (ElementType.Y, ElementType.Y) => ElementCombinationType.YY,
                (ElementType.R, ElementType.B) => ElementCombinationType.RB,
                (ElementType.R, ElementType.Y) => ElementCombinationType.RY,
                (ElementType.B, ElementType.Y) => ElementCombinationType.BY,
                _ => ElementCombinationType.None
            };

            return combinationType != ElementCombinationType.None;
        }
    }
}
