using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT || TEXTMESHPRO_UGUI_PRESENT || UNITY_TEXTMESHPRO
using TMPro;
#endif

namespace WizardBrawl.Enemy
{
    /// <summary>
    /// 보스 전투 UI/SFX 바인딩 포인트를 한 곳에서 연결함.
    /// </summary>
    [RequireComponent(typeof(BossAI))]
    public class BossBattlePresentationBridge : MonoBehaviour
    {
        private const string MissingBindingTag = "[BossUI] MISSING_BINDING";
        private const string MissingCueTag = "[BossSFX] MISSING_CUE";

        [Header("UI")]
        [SerializeField] private GameObject _bossHudRoot;
        [SerializeField] private Slider _bossHealthSlider;
#if TMP_PRESENT || TEXTMESHPRO_UGUI_PRESENT || UNITY_TEXTMESHPRO
        [SerializeField] private TMP_Text _phaseLabel;
#endif

        [Header("SFX")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _phaseTransitionClip;

        private BossAI _bossAI;

        private void Awake()
        {
            _bossAI = GetComponent<BossAI>();
        }

        private void OnEnable()
        {
            if (_bossAI == null)
            {
                return;
            }

            _bossAI.OnBossHealthChanged += HandleBossHealthChanged;
            _bossAI.OnPhaseChanged += HandlePhaseChanged;
            RefreshBindings();
        }

        private void OnDisable()
        {
            if (_bossAI == null)
            {
                return;
            }

            _bossAI.OnBossHealthChanged -= HandleBossHealthChanged;
            _bossAI.OnPhaseChanged -= HandlePhaseChanged;
        }

        private void RefreshBindings()
        {
            if (_bossHudRoot != null)
            {
                _bossHudRoot.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"{MissingBindingTag}: target=BossHudRoot", this);
            }

            HandlePhaseChanged(_bossAI.CurrentPhase);
        }

        private void HandleBossHealthChanged(float currentHealth, float maxHealth)
        {
            if (_bossHealthSlider == null)
            {
                Debug.LogWarning($"{MissingBindingTag}: target=BossHealthSlider", this);
                return;
            }

            _bossHealthSlider.minValue = 0f;
            _bossHealthSlider.maxValue = maxHealth;
            _bossHealthSlider.value = currentHealth;
        }

        private void HandlePhaseChanged(BossCombatPhase phase)
        {
#if TMP_PRESENT || TEXTMESHPRO_UGUI_PRESENT || UNITY_TEXTMESHPRO
            if (_phaseLabel != null)
            {
                _phaseLabel.text = phase == BossCombatPhase.Phase2 ? "PHASE 2" : "PHASE 1";
            }
            else
            {
                Debug.LogWarning($"{MissingBindingTag}: target=PhaseLabel", this);
            }
#endif

            if (phase == BossCombatPhase.Phase2)
            {
                PlayPhaseTransitionCue();
            }
        }

        private void PlayPhaseTransitionCue()
        {
            if (_audioSource == null || _phaseTransitionClip == null)
            {
                Debug.LogWarning($"{MissingCueTag}: cue=phase_transition", this);
                return;
            }

            _audioSource.PlayOneShot(_phaseTransitionClip);
        }
    }
}
