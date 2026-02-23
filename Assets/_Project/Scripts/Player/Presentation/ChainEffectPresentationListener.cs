using System.Collections;
using UnityEngine;
using WizardBrawl.Player.Services;

namespace WizardBrawl.Player.Presentation
{
    /// <summary>
    /// 체인 대효과 이벤트를 수신해 카메라/SFX/VFX 연출을 실행함.
    /// </summary>
    public sealed class ChainEffectPresentationListener : MonoBehaviour
    {
        [Header("Camera Shake")]
        [SerializeField] private Transform _cameraShakeTarget;
        [SerializeField] private bool _enableCameraShake = true;
        [SerializeField, Min(0f)] private float _shakeDuration = 0.1f;
        [SerializeField, Min(0f)] private float _shakeAmplitude = 0.08f;

        [Header("SFX")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _chainEffectSfx;

        [Header("VFX")]
        [SerializeField] private ParticleSystem _chainEffectVfxPrefab;
        [SerializeField, Min(0f)] private float _vfxAutoDestroyDelay = 3f;

        private Coroutine _shakeCoroutine;
        private Vector3 _shakeOriginLocalPosition;
        private bool _isShaking;

        private void OnEnable()
        {
            ChainEffectEventBus.EffectApplied += OnChainEffectApplied;
        }

        private void OnDisable()
        {
            ChainEffectEventBus.EffectApplied -= OnChainEffectApplied;
            StopShakeAndRestore();
        }

        private void OnChainEffectApplied(ChainEffectAppliedEvent evt)
        {
            if (evt.Context.Caster != gameObject)
            {
                return;
            }

            PlaySfx();
            SpawnVfx(evt.Context.TargetPoint);
            TriggerCameraShake();
        }

        private void PlaySfx()
        {
            if (_audioSource == null || _chainEffectSfx == null)
            {
                return;
            }

            _audioSource.PlayOneShot(_chainEffectSfx);
        }

        private void SpawnVfx(Vector3 worldPoint)
        {
            if (_chainEffectVfxPrefab == null)
            {
                return;
            }

            ParticleSystem spawned = Instantiate(_chainEffectVfxPrefab, worldPoint, Quaternion.identity);
            if (_vfxAutoDestroyDelay > 0f)
            {
                Destroy(spawned.gameObject, _vfxAutoDestroyDelay);
            }
        }

        private void TriggerCameraShake()
        {
            if (!_enableCameraShake || _cameraShakeTarget == null || _shakeDuration <= 0f || _shakeAmplitude <= 0f)
            {
                return;
            }

            if (_isShaking)
            {
                return;
            }

            _shakeCoroutine = StartCoroutine(ShakeRoutine());
        }

        private IEnumerator ShakeRoutine()
        {
            _isShaking = true;
            _shakeOriginLocalPosition = _cameraShakeTarget.localPosition;
            float elapsed = 0f;

            while (elapsed < _shakeDuration)
            {
                elapsed += Time.deltaTime;
                Vector3 offset = Random.insideUnitSphere * _shakeAmplitude;
                _cameraShakeTarget.localPosition = _shakeOriginLocalPosition + offset;
                yield return null;
            }

            _cameraShakeTarget.localPosition = _shakeOriginLocalPosition;
            _isShaking = false;
            _shakeCoroutine = null;
        }

        private void StopShakeAndRestore()
        {
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                _shakeCoroutine = null;
            }

            if (_cameraShakeTarget != null && _isShaking)
            {
                _cameraShakeTarget.localPosition = _shakeOriginLocalPosition;
            }

            _isShaking = false;
        }
    }
}
