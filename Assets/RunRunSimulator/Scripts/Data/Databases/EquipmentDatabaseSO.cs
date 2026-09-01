using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "EquipmentDatabase", menuName = "RunRunSimulator/Databases/Equipment Database")]
public class EquipmentDatabaseSO : KeyedDatabaseSO<EquipmentSO>
{
    [Title("Equipment Dictionary", "Primary data source — add and edit entries here.")]
    [Searchable]
    [DictionaryDrawerSettings(KeyLabel = "ID", ValueLabel = "Item",
        DisplayMode = DictionaryDisplayOptions.ExpandedFoldout)]
    [OdinSerialize]
    private Dictionary<string, EquipmentSO> equipment = new Dictionary<string, EquipmentSO>();

    protected override Dictionary<string, EquipmentSO> Entries => equipment;

    protected override string IDPrefix => "EQ";

    protected override void SetEntryID(EquipmentSO entry, string id) => entry.ID = id;

#if UNITY_EDITOR
    private static EquipmentDatabaseSO editorInstance;

    public static EquipmentDatabaseSO Editor
    {
        get
        {
            if (editorInstance != null) return editorInstance;
            var guids = UnityEditor.AssetDatabase.FindAssets("t:EquipmentDatabaseSO");
            if (guids.Length > 0)
                editorInstance = UnityEditor.AssetDatabase.LoadAssetAtPath<EquipmentDatabaseSO>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
            return editorInstance;
        }
    }
#endif

    public Dictionary<string, EquipmentSO> Equipment => equipment;

    [ShowInInspector, ReadOnly, LabelText("Total Equipment")]
    public int EquipmentCount => equipment?.Count ?? 0;
}
}
