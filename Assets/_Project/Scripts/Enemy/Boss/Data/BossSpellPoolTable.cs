using System.Collections.Generic;
using UnityEngine;

namespace WizardBrawl.Enemy
{
    /// <summary>
    /// 보스가 사용할 스킬 풀 테이블(SO).
    /// </summary>
    [CreateAssetMenu(fileName = "SO_BossSpellPoolTable", menuName = "Enemy/Boss Spell Pool Table")]
    public class BossSpellPoolTable : ScriptableObject
    {
        [SerializeField] private List<BossSpellEntry> _entries = new List<BossSpellEntry>();

        public IReadOnlyList<BossSpellEntry> Entries => _entries;
    }
}
