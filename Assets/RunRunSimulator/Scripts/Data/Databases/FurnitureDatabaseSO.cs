using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "FurnitureDatabase", menuName = "RunRunSimulator/Databases/Furniture Database")]
public class FurnitureDatabaseSO : KeyedDatabaseSO<FurnitureDefinitionSO>
{
    [Title("Furniture Dictionary", "Primary data source — the key IS the id; the value is the definition.")]
    [Searchable]
    [DictionaryDrawerSettings(
        KeyLabel = "ID",
        ValueLabel = "Definition",
        DisplayMode = DictionaryDisplayOptions.ExpandedFoldout)]
    [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
    [OdinSerialize]
    private Dictionary<string, FurnitureDefinitionSO> items = new Dictionary<string, FurnitureDefinitionSO>();

    protected override Dictionary<string, FurnitureDefinitionSO> Entries => items;

    protected override string IDPrefix => "F";

    protected override void SetEntryID(FurnitureDefinitionSO entry, string id) => entry.Id = id;

    public IEnumerable<FurnitureDefinitionSO> All => items.Values;
}
}
