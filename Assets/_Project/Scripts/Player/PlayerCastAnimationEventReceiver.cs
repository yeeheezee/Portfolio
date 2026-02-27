using UnityEngine;

namespace WizardBrawl.Player
{
    /// <summary>
    /// Animation Event를 PlayerAttackCaster로 위임하는 브리지.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerCastAnimationEventReceiver : MonoBehaviour
    {
        [SerializeField] private PlayerAttackCaster _caster;

        private void Awake()
        {
            if (_caster == null)
            {
                _caster = GetComponentInParent<PlayerAttackCaster>();
            }

            if (_caster == null)
            {
                Debug.LogWarning("[AnimEvent] PlayerCastAnimationEventReceiver has no PlayerAttackCaster reference.", this);
            }
        }

        public void OnCastFire()
        {
            _caster?.OnAnimationCastFire();
        }

        public void OnCastComplete()
        {
            _caster?.OnAnimationCastComplete();
        }

        public void OnRecoveryEnd()
        {
            _caster?.OnAnimationRecoveryEnd();
        }
    }
}
