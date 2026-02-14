namespace WizardBrawl.Core
{
    /// <summary>
    /// 패링 가능한 모든 객체가 구현해야 하는 인터페이스.
    /// </summary>
    public interface IParryable
    {
        /// <summary>
        /// 패링 성공 시 호출되는 메서드.
        /// </summary>
        /// <param name="parriedElement">패링으로 획득한 속성.</param>
        /// <returns>패링 성공 시 투사체를 파괴해야 하면 true, 아니면 false.</returns>
        bool OnParrySuccess(ElementType parriedElement);
    }
}
