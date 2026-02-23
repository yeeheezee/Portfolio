using System.Collections.Generic;
using UnityEngine;

namespace WizardBrawl.Magic.Data.SpellRecipe
{
    /// <summary>
    /// 주입 소효과 테이블(SO).
    /// </summary>
    [CreateAssetMenu(fileName = "SO_InjectionEffectTable", menuName = "Magic/Injection Effect Table")]
    public class InjectionEffectTable : ScriptableObject
    {
        [SerializeField] private List<InjectionEffectEntry> _entries = new List<InjectionEffectEntry>();

        private Dictionary<InjectionEffectKey, InjectionEffectEntry> _cache;

        public bool TryGetEffect(InjectionEffectKey key, out InjectionEffectEntry entry)
        {
            EnsureCache();
            return _cache.TryGetValue(key, out entry);
        }

        private void OnValidate()
        {
            _cache = null;
            ValidateDuplicateKeys();
        }

        private void EnsureCache()
        {
            if (_cache != null)
            {
                return;
            }

            _cache = new Dictionary<InjectionEffectKey, InjectionEffectEntry>();
            foreach (InjectionEffectEntry entry in _entries)
            {
                if (entry == null)
                {
                    continue;
                }

                InjectionEffectKey key = entry.Key;
                if (_cache.ContainsKey(key))
                {
                    Debug.LogWarning($"[InjectionEffect] duplicate key ignored: {key}", this);
                    continue;
                }

                _cache.Add(key, entry);
            }
        }

        private void ValidateDuplicateKeys()
        {
            HashSet<InjectionEffectKey> seen = new HashSet<InjectionEffectKey>();
            foreach (InjectionEffectEntry entry in _entries)
            {
                if (entry == null)
                {
                    continue;
                }

                InjectionEffectKey key = entry.Key;
                if (!seen.Add(key))
                {
                    Debug.LogWarning($"[InjectionEffect] duplicate key found in table: {key}", this);
                }
            }
        }
    }
}
