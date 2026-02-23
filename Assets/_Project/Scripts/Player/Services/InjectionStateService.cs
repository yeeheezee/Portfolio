using UnityEngine;
using WizardBrawl.Core;
using WizardBrawl.Magic.Data.SpellRecipe;

namespace WizardBrawl.Player.Services
{
    /// <summary>
    /// 주입(arm/consume/pending) 상태를 관리함.
    /// </summary>
    public sealed class InjectionStateService
    {
        private bool _isArmed;
        private bool _hasPendingConsume;
        private SpellImpactType _pendingImpactType = SpellImpactType.None;
        private ElementType _pendingElement = ElementType.None;

        public bool ToggleArm()
        {
            _isArmed = !_isArmed;
            return _isArmed;
        }

        public bool TryPrepareInject(PlayerElementSlot elementSlot, out ElementType injectedElement, out string blockedReason)
        {
            injectedElement = ElementType.None;
            blockedReason = null;

            if (!_isArmed)
            {
                blockedReason = "not_armed";
                return false;
            }

            if (elementSlot == null || elementSlot.CurrentState.SlotA == ElementType.None)
            {
                blockedReason = "empty_slot";
                return false;
            }

            injectedElement = elementSlot.CurrentState.SlotA;
            return true;
        }

        public void MarkQueued(SpellImpactType impactType, ElementType injectedElement)
        {
            _isArmed = false;
            _hasPendingConsume = true;
            _pendingImpactType = impactType;
            _pendingElement = injectedElement;
        }

        public void ConsumeImmediate(PlayerElementSlot elementSlot, SpellImpactType impactType, ElementType expectedElement)
        {
            _isArmed = false;
            TryConsume(elementSlot, impactType, expectedElement);
        }

        public void ConfirmQueued(PlayerElementSlot elementSlot)
        {
            if (!_hasPendingConsume)
            {
                return;
            }

            TryConsume(elementSlot, _pendingImpactType, _pendingElement);
            ClearPending();
        }

        public bool CancelQueued()
        {
            if (!_hasPendingConsume)
            {
                return false;
            }

            _isArmed = true;
            ClearPending();
            return true;
        }

        public void ResetState()
        {
            _isArmed = false;
            ClearPending();
        }

        private void TryConsume(PlayerElementSlot elementSlot, SpellImpactType impactType, ElementType expectedElement)
        {
            if (elementSlot == null)
            {
                return;
            }

            if (elementSlot.TryConsumeFront(out ElementType consumed, $"inject:{impactType}"))
            {
                Debug.Log($"[Inject] consumed={consumed} expected={expectedElement} impact={impactType}");
            }
        }

        private void ClearPending()
        {
            _hasPendingConsume = false;
            _pendingImpactType = SpellImpactType.None;
            _pendingElement = ElementType.None;
        }
    }
}
