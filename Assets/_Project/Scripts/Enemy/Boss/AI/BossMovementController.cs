using UnityEngine;

namespace WizardBrawl.Enemy
{
    /// <summary>
    /// 보스의 이동 예약과 물리 이동 적용을 담당함.
    /// </summary>
    public sealed class BossMovementController
    {
        private readonly Transform _transform;
        private readonly Rigidbody _rigidbody;

        private Vector3 _pendingMovePosition;
        private bool _hasPendingMove;

        public BossMovementController(Transform transform, Rigidbody rigidbody)
        {
            _transform = transform;
            _rigidbody = rigidbody;
        }

        public void ClearPendingMove()
        {
            _hasPendingMove = false;
        }

        public void FlushPendingMove()
        {
            if (!_hasPendingMove)
            {
                return;
            }

            _hasPendingMove = false;

            if (_rigidbody != null && !_rigidbody.isKinematic)
            {
                _rigidbody.MovePosition(_pendingMovePosition);
                return;
            }

            _transform.position = _pendingMovePosition;
        }

        public void MoveTowards(Vector3 targetPosition, float maxDistanceDelta)
        {
            _pendingMovePosition = Vector3.MoveTowards(_transform.position, targetPosition, maxDistanceDelta);
            _hasPendingMove = true;
        }

        public void MoveLateral(Vector3 toTarget, int direction, float maxDistanceDelta)
        {
            Vector3 flattened = toTarget;
            flattened.y = 0f;
            if (flattened.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector3 lateral = Vector3.Cross(Vector3.up, flattened.normalized) * direction;
            _pendingMovePosition = Vector3.MoveTowards(_transform.position, _transform.position + lateral, maxDistanceDelta);
            _hasPendingMove = true;
        }

        public void MoveAwayFrom(Vector3 sourcePosition, float maxDistanceDelta)
        {
            Vector3 awayDirection = (_transform.position - sourcePosition).normalized;
            if (awayDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            _pendingMovePosition = Vector3.MoveTowards(_transform.position, _transform.position + awayDirection, maxDistanceDelta);
            _hasPendingMove = true;
        }
    }
}
