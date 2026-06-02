using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

// Flat catalog of every furniture definition, resolved by Id (mirror of the creature
// databases). The shop reads this list; the spawner looks up a placed piece's def here.
[CreateAssetMenu(fileName = "FurnitureDatabase", menuName = "RunRunSimulator/Furniture Database")]
public class FurnitureDatabaseSO : ScriptableObject
{
    [TableList, SerializeField] private List<FurnitureDefinitionSO> items = new List<FurnitureDefinitionSO>();

    public FurnitureDefinitionSO GetById(string id)
    {
        foreach (var it in items)
            if (it != null && it.Id == id) return it;
        return null;
    }

    [Button("Validate IDs", ButtonSizes.Large), GUIColor(1f, 0.8f, 0.2f)]
    private void ValidateIds()
    {
        var seen = new HashSet<string>();
        int dups = 0;
        foreach (var it in items)
        {
            if (it == null || string.IsNullOrEmpty(it.Id)) continue;
            if (it.Id.Contains("-")) Debug.LogError($"[FurnitureDB] Id '{it.Id}' contains '-' (illegal — it's a save separator).");
            if (!seen.Add(it.Id)) { Debug.LogError($"[FurnitureDB] Duplicate id '{it.Id}'."); dups++; }
        }
        Debug.Log(dups == 0 ? "[FurnitureDB] Validation PASSED." : $"[FurnitureDB] {dups} duplicate id(s).");
    }
}
