using System.Collections.Generic;
using UnityEngine;

namespace WizardBrawl.Magic.Data.SpellRecipe
{
    /// <summary>
    /// 체인 대효과 테이블(SO).
    /// </summary>
    [CreateAssetMenu(fileName = "SO_ChainEffectTable", menuName = "Magic/Chain Effect Table")]
    public class ChainEffectTable : ScriptableObject
    {
        [SerializeField] private List<ChainEffectEntry> _entries = new List<ChainEffectEntry>();

        private Dictionary<ChainEffectKey, ChainEffectEntry> _cache;

        public bool TryGetEffect(ChainEffectKey key, out ChainEffectEntry entry)
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

            _cache = new Dictionary<ChainEffectKey, ChainEffectEntry>();
            foreach (ChainEffectEntry entry in _entries)
            {
                if (entry == null)
                {
                    continue;
                }

                ChainEffectKey key = entry.Key;
                if (_cache.ContainsKey(key))
                {
                    Debug.LogWarning($"[ChainEffect] duplicate key ignored: {key}", this);
                    continue;
                }

                _cache.Add(key, entry);
            }
        }

        private void ValidateDuplicateKeys()
        {
            HashSet<ChainEffectKey> seen = new HashSet<ChainEffectKey>();
            foreach (ChainEffectEntry entry in _entries)
            {
                if (entry == null)
                {
                    continue;
                }

                ChainEffectKey key = entry.Key;
                if (!seen.Add(key))
                {
                    Debug.LogWarning($"[ChainEffect] duplicate key found in table: {key}", this);
                }
            }
        }
    }
}
