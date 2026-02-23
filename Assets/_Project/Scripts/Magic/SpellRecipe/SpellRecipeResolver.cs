using WizardBrawl.Magic.Data;
using WizardBrawl.Magic.Data.SpellRecipe;
using UnityEngine;

namespace WizardBrawl.Magic.SpellRecipe
{
    /// <summary>
    /// 체인 키로 레시피를 조회하고 폴백 정책을 적용함.
    /// </summary>
    public sealed class SpellRecipeResolver
    {
        private readonly SpellRecipeTable _table;

        public SpellRecipeResolver(SpellRecipeTable table)
        {
            _table = table;
        }

        public SpellRecipeResolution Resolve(SpellRecipeKey key, MagicData defaultMagic)
        {
            if (_table == null)
            {
                Debug.Log($"[SpellRecipe] table missing: key={key} fallback={defaultMagic?.MagicName ?? "None"}");
                return SpellRecipeResolution.Fallback(key, defaultMagic, "table_missing");
            }

            if (_table.TryGetRecipe(key, out SpellRecipeEntry entry))
            {
                MagicData selectedMagic = entry.MagicOverride == null ? defaultMagic : entry.MagicOverride;
                Debug.Log($"[SpellRecipe] hit: key={key} magic={selectedMagic?.MagicName ?? "None"}");
                return SpellRecipeResolution.Hit(key, entry, selectedMagic);
            }

            Debug.Log($"[SpellRecipe] missing: key={key} fallback={defaultMagic?.MagicName ?? "None"}");
            return SpellRecipeResolution.Fallback(key, defaultMagic, "recipe_missing");
        }
    }

    /// <summary>
    /// 레시피 조회 결과.
    /// </summary>
    public readonly struct SpellRecipeResolution
    {
        private SpellRecipeResolution(
            SpellRecipeKey key,
            SpellRecipeEntry entry,
            MagicData selectedMagic,
            bool hasRecipe,
            bool usedFallback,
            string reason)
        {
            Key = key;
            Entry = entry;
            SelectedMagic = selectedMagic;
            HasRecipe = hasRecipe;
            UsedFallback = usedFallback;
            Reason = reason;
        }

        public SpellRecipeKey Key { get; }
        public SpellRecipeEntry Entry { get; }
        public MagicData SelectedMagic { get; }
        public bool HasRecipe { get; }
        public bool UsedFallback { get; }
        public string Reason { get; }

        public static SpellRecipeResolution Hit(SpellRecipeKey key, SpellRecipeEntry entry, MagicData selectedMagic)
        {
            return new SpellRecipeResolution(key, entry, selectedMagic, true, false, "hit");
        }

        public static SpellRecipeResolution Fallback(SpellRecipeKey key, MagicData selectedMagic, string reason)
        {
            return new SpellRecipeResolution(key, null, selectedMagic, false, true, reason);
        }
    }
}
