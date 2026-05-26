using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

public abstract class PartDatabaseSO<T> : SerializedScriptableObject where T : BodyPart
{
    [ShowInInspector, ReadOnly, LabelText("Total Parts")]
    private int PartCount => _parts?.Count ?? 0;

    [Title("Parts Dictionary", "Primary data source — add and edit entries here.")]
    [Searchable]
    [DictionaryDrawerSettings(
        KeyLabel = "ID",
        ValueLabel = "Part",
        DisplayMode = DictionaryDisplayOptions.ExpandedFoldout)]
    [OdinSerialize]
    private Dictionary<string, T> _parts = new Dictionary<string, T>();

    [Title("Parts Overview")]
    [ShowInInspector, ReadOnly]
    [TableList(AlwaysExpanded = false, DrawScrollView = true, MaxScrollViewHeight = 300)]
    private List<T> PartsTable => _parts?.Values.Where(p => p != null).ToList() ?? new List<T>();

    // Satisfies the "Dictionary<string, T> parts" requirement
    public Dictionary<string, T> Parts => _parts;

    public T GetPartByID(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        _parts.TryGetValue(id, out T part);
        return part;
    }

    public T GetRandomPart(Rarity? rarityFilter = null, PartSet? setFilter = null)
    {
        var pool = _parts.Values.Where(p => p != null);

        if (rarityFilter.HasValue) pool = pool.Where(p => p.Rarity == rarityFilter.Value);
        if (setFilter.HasValue)    pool = pool.Where(p => p.Set    == setFilter.Value);

        var list = pool.ToList();
        return list.Count > 0 ? list[Random.Range(0, list.Count)] : null;
    }

    public List<T>      GetBySet(PartSet set)  => _parts.Values.Where(p => p != null && p.Set == set).ToList();
    public List<string> GetAllIDs()            => _parts?.Keys.ToList() ?? new List<string>();
}
