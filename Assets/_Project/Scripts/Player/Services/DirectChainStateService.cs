using WizardBrawl.Core;
using WizardBrawl.Magic.Data.SpellRecipe;

namespace WizardBrawl.Player.Services
{
    /// <summary>
    /// 직접 시전 체인 상태(최근 원소, 타게팅 대기 중 pending)를 관리함.
    /// </summary>
    public sealed class DirectChainStateService
    {
        private readonly ElementCombinationResolver _combinationResolver = new ElementCombinationResolver();

        private ElementType _lastDebuffElement = ElementType.None;
        private ElementType _lastCrowdControlElement = ElementType.None;
        private ElementType _pendingInjectedElement = ElementType.None;
        private SpellImpactType _pendingImpactType = SpellImpactType.None;

        public void Reset()
        {
            _lastDebuffElement = ElementType.None;
            _lastCrowdControlElement = ElementType.None;
            ClearPending();
        }

        public void BeginPending(SpellImpactType impactType, ElementType injectedElement)
        {
            _pendingImpactType = impactType;
            _pendingInjectedElement = injectedElement;
        }

        public void ClearPending()
        {
            _pendingImpactType = SpellImpactType.None;
            _pendingInjectedElement = ElementType.None;
        }

        public void ConfirmPendingAndRecordSuccess()
        {
            if (_pendingImpactType != SpellImpactType.None && _pendingInjectedElement != ElementType.None)
            {
                RecordSuccess(_pendingImpactType, _pendingInjectedElement);
            }

            ClearPending();
        }

        public void RecordSuccess(SpellImpactType impactType, ElementType injectedElement)
        {
            switch (impactType)
            {
                case SpellImpactType.Debuff:
                    _lastDebuffElement = injectedElement;
                    _lastCrowdControlElement = ElementType.None;
                    break;
                case SpellImpactType.CrowdControl:
                    _lastCrowdControlElement = injectedElement;
                    _lastDebuffElement = ElementType.None;
                    break;
                case SpellImpactType.Ultimate:
                    _lastCrowdControlElement = ElementType.None;
                    _lastDebuffElement = ElementType.None;
                    break;
            }
        }

        public bool TryBuildRecipeKeyForDirectCast(
            SpellImpactType impactType,
            ElementType injectedElement,
            out SpellRecipeKey key,
            out ElementCombinationType combo)
        {
            key = default;
            combo = ElementCombinationType.None;

            ChainStage stage;
            ElementType fromElement;

            switch (impactType)
            {
                case SpellImpactType.CrowdControl:
                    if (_lastDebuffElement == ElementType.None)
                    {
                        return false;
                    }

                    stage = ChainStage.DebuffToCrowdControl;
                    fromElement = _lastDebuffElement;
                    break;
                case SpellImpactType.Ultimate:
                    if (_lastCrowdControlElement == ElementType.None)
                    {
                        return false;
                    }

                    stage = ChainStage.CrowdControlToUltimate;
                    fromElement = _lastCrowdControlElement;
                    break;
                default:
                    return false;
            }

            _combinationResolver.TryResolve(fromElement, injectedElement, out combo);
            key = new SpellRecipeKey(stage, fromElement, injectedElement);
            return true;
        }
    }
}
