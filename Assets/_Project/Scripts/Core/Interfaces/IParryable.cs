namespace WizardBrawl.Core
{
    /// <summary>
    /// 패링될 수 있는 모든 객체가 구현해야 하는 인터페이스.
    /// 투사체로부터 패링 성공/실패 이벤트를 전달받는 역할을 함.
    /// </summary>
    public interface IParryable
    {
        /// <summary>
        /// 패링 성공 시 호출되는 메서드.
        /// </summary>
        /// <returns>패링 성공 시 투사체를 파괴해야 하면 true, 아니면 false.</returns>
        bool OnParrySuccess();
    }
}