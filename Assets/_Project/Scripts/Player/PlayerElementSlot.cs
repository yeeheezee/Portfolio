using UnityEngine;
using WizardBrawl.Core;

namespace WizardBrawl.Player
{
    /// <summary>
    /// 플레이어의 속성 2슬롯 상태를 관리하고 로그를 출력함.
    /// </summary>
    public class PlayerElementSlot : MonoBehaviour
    {
        private readonly ElementSlotService _slotService = new ElementSlotService();
        private bool _isMutationLocked;

        public ElementSlotState CurrentState => _slotService.CurrentState;

        /// <summary>
        /// 패링 성공으로 획득한 속성을 슬롯에 저장함.
        /// </summary>
        /// <param name="elementType">획득 속성.</param>
        public void SaveFromParry(ElementType elementType)
        {
            if (_isMutationLocked)
            {
                Debug.Log($"[ElementSlot] save blocked while locked | element={elementType}");
                return;
            }

            ElementSlotUpdateResult result = _slotService.Save(elementType, $"parry:{elementType}");
            LogUpdate(result);
        }

        public void SetMutationLocked(bool isLocked)
        {
            _isMutationLocked = isLocked;
        }

        public void SetParrySaveLocked(bool isLocked)
        {
            // Backward-compatible wrapper.
            SetMutationLocked(isLocked);
        }

        /// <summary>
        /// 슬롯을 초기화함.
        /// </summary>
        /// <param name="reason">초기화 사유.</param>
        public void ResetSlots(string reason)
        {
            if (_isMutationLocked && !IsAllowedWhileLocked(reason))
            {
                Debug.Log($"[ElementSlot] reset blocked while locked | reason={reason}");
                return;
            }

            ElementSlotUpdateResult result = _slotService.Reset(reason);
            LogUpdate(result);
        }

        /// <summary>
        /// 슬롯의 앞 요소를 소비함(FIFO).
        /// </summary>
        /// <param name="consumed">소비된 속성.</param>
        /// <param name="reason">소비 사유.</param>
        /// <returns>소비 성공 시 true.</returns>
        public bool TryConsumeFront(out ElementType consumed, string reason)
        {
            if (_isMutationLocked && !IsAllowedWhileLocked(reason))
            {
                consumed = ElementType.None;
                Debug.Log($"[ElementSlot] consume blocked while locked | reason={reason}");
                return false;
            }

            bool success = _slotService.TryConsumeFront(out consumed, out ElementSlotUpdateResult result, reason);
            LogUpdate(result);
            return success;
        }

        private static bool IsAllowedWhileLocked(string reason)
        {
            // 타게팅 중 확정 시 슬롯 소비는 허용해야 함.
            return !string.IsNullOrEmpty(reason) && reason.StartsWith("inject:");
        }

        private static void LogUpdate(ElementSlotUpdateResult result)
        {
            Debug.Log($"[ElementSlot] {result.Before} -> {result.After} | {result.Reason}");
        }
    }
}
