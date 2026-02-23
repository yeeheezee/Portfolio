using System;
using System.Collections;
using UnityEngine;

namespace WizardBrawl.Player
{
    public enum CastTimingState
    {
        Idle,
        Casting,
        Recovery
    }

    public sealed class CastTimingService
    {
        private MonoBehaviour _runner;
        private Coroutine _castRoutine;
        private CastTimingState _state = CastTimingState.Idle;

        public bool IsLocked => _state != CastTimingState.Idle;
        public CastTimingState State => _state;

        public void Initialize(MonoBehaviour runner)
        {
            _runner = runner;
        }

        public bool TryBegin(
            float windupSeconds,
            float recoverySeconds,
            Func<bool> executeCast,
            string source,
            string skillName)
        {
            if (_runner == null)
            {
                Debug.LogWarning("[CastState] blocked: reason=runner_not_initialized");
                return false;
            }

            if (executeCast == null)
            {
                Debug.LogWarning("[CastState] blocked: reason=missing_execute_callback");
                return false;
            }

            if (IsLocked)
            {
                Debug.Log($"[CastState] blocked: source={source}, state={_state}");
                return false;
            }

            _castRoutine = _runner.StartCoroutine(CastRoutine(
                Mathf.Max(0f, windupSeconds),
                Mathf.Max(0f, recoverySeconds),
                executeCast,
                source,
                skillName));
            return true;
        }

        public void Cancel(string reason)
        {
            if (_runner != null && _castRoutine != null)
            {
                _runner.StopCoroutine(_castRoutine);
                _castRoutine = null;
            }

            SetState(CastTimingState.Idle, reason);
        }

        private IEnumerator CastRoutine(
            float windupSeconds,
            float recoverySeconds,
            Func<bool> executeCast,
            string source,
            string skillName)
        {
            SetState(CastTimingState.Casting, $"source={source}, magic={skillName}");

            if (windupSeconds > 0f)
            {
                yield return new WaitForSeconds(windupSeconds);
            }

            bool castSuccess = executeCast();
            Debug.Log($"[CastState] execute: source={source}, magic={skillName}, result={(castSuccess ? "success" : "failed")}");
            if (!castSuccess)
            {
                SetState(CastTimingState.Idle, $"source={source}, reason=cast_failed");
                _castRoutine = null;
                yield break;
            }

            SetState(CastTimingState.Recovery, $"source={source}");
            if (recoverySeconds > 0f)
            {
                yield return new WaitForSeconds(recoverySeconds);
            }

            SetState(CastTimingState.Idle, $"source={source}, reason=recovery_done");
            _castRoutine = null;
        }

        private void SetState(CastTimingState nextState, string reason)
        {
            if (_state == nextState)
            {
                return;
            }

            Debug.Log($"[CastState] {_state} -> {nextState} | {reason}");
            _state = nextState;
        }
    }
}
