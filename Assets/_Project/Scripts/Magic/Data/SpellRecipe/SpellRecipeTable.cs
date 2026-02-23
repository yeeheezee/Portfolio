using System.Collections.Generic;
using UnityEngine;

namespace WizardBrawl.Magic.Data.SpellRecipe
{
    /// <summary>
    /// 체인 레시피 테이블(SO).
    /// </summary>
    [CreateAssetMenu(fileName = "SO_SpellRecipeTable", menuName = "Magic/Spell Recipe Table")]
    public class SpellRecipeTable : ScriptableObject
    {
        [SerializeField] private List<SpellRecipeEntry> _entries = new List<SpellRecipeEntry>();

        private Dictionary<SpellRecipeKey, SpellRecipeEntry> _cache;

        public bool TryGetRecipe(SpellRecipeKey key, out SpellRecipeEntry entry)
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

            _cache = new Dictionary<SpellRecipeKey, SpellRecipeEntry>();

            foreach (SpellRecipeEntry entry in _entries)
            {
                if (entry == null)
                {
                    continue;
                }

                SpellRecipeKey key = entry.Key;
                if (_cache.ContainsKey(key))
                {
                    Debug.LogWarning($"[SpellRecipe] duplicate key ignored: {key}", this);
                    continue;
                }

                _cache.Add(key, entry);
            }
        }

        private void ValidateDuplicateKeys()
        {
            HashSet<SpellRecipeKey> seen = new HashSet<SpellRecipeKey>();
            foreach (SpellRecipeEntry entry in _entries)
            {
                if (entry == null)
                {
                    continue;
                }

                SpellRecipeKey key = entry.Key;
                if (!seen.Add(key))
                {
                    Debug.LogWarning($"[SpellRecipe] duplicate key found in table: {key}", this);
                }
            }
        }
    }
}
