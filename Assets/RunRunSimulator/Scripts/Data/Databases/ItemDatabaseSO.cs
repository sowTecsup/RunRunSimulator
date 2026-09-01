using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "RunRunSimulator/Databases/Item Database")]
public class ItemDatabaseSO : KeyedDatabaseSO<ItemDefinitionSO>
{
    [Title("Item Dictionary", "Primary data source — the key IS the id; the value is the definition.")]
    [Searchable]
    [DictionaryDrawerSettings(
        KeyLabel = "ID",
        ValueLabel = "Definition",
        DisplayMode = DictionaryDisplayOptions.ExpandedFoldout)]
    [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
    [OdinSerialize]
    private Dictionary<string, ItemDefinitionSO> items = new Dictionary<string, ItemDefinitionSO>();

    protected override Dictionary<string, ItemDefinitionSO> Entries => items;

    protected override string IDPrefix => "I";

    protected override void SetEntryID(ItemDefinitionSO entry, string id) => entry.Id = id;
}
}
