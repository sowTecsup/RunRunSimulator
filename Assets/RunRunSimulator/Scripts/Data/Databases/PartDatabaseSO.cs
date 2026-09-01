using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

public abstract class PartDatabaseSO<T> : KeyedDatabaseSO<T> where T : BodyPart
{
    [Title("Parts Dictionary", "Primary data source — add and edit entries here.")]
    [Searchable]
    [DictionaryDrawerSettings(
        KeyLabel = "ID",
        ValueLabel = "Part",
        DisplayMode = DictionaryDisplayOptions.ExpandedFoldout)]
    [OdinSerialize]
    [PreviouslySerializedAs("_parts")]
    private Dictionary<string, T> parts = new Dictionary<string, T>();

    protected override Dictionary<string, T> Entries => parts;

    protected override void SetEntryID(T entry, string id) => entry.ID = id;

    protected override void OnPopulated(int added) => RollAllNames();

    [ButtonGroup("Admin")]
    [Button("Roll All Names"), GUIColor(0.5f, 0.85f, 1f)]
    private void RollAllNames()
    {
        int count = 0;
        foreach (var part in parts.Values.Where(p => p != null))
        {
            part.Name = PartNameBank.GetRandomName(part.Set, part.GetPartRole());
            count++;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(part);
#endif
        }
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log($"[{GetType().Name}] Rolled names for {count} parts.");
    }

    public T GetPartByID(string id) => GetByID(id);

    public T GetRandomPart(Rarity? rarityFilter = null, PartSet? setFilter = null)
    {
        var pool = parts.Values.Where(p => p != null);

        if (rarityFilter.HasValue) pool = pool.Where(p => p.Rarity == rarityFilter.Value);
        if (setFilter.HasValue)    pool = pool.Where(p => p.Set    == setFilter.Value);

        var list = pool.ToList();
        return list.Count > 0 ? list[Random.Range(0, list.Count)] : null;
    }

    public Dictionary<string, T> Parts => parts;

    [ShowInInspector, ReadOnly, LabelText("Total Parts")]
    public int PartCount => parts?.Count ?? 0;

    [Title("Parts Overview")]
    [ShowInInspector, ReadOnly]
    [TableList(AlwaysExpanded = false, DrawScrollView = true, MaxScrollViewHeight = 300)]
    public List<T> PartsTable => parts?.Values.Where(p => p != null).ToList() ?? new List<T>();
}
}
