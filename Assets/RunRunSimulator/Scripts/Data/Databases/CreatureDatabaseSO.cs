using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "CreatureDatabase", menuName = "RunRunSimulator/Databases/Creature Database (Orchestrator)")]
public class CreatureDatabaseSO : SerializedScriptableObject
{
    [Title("Sub-Databases", "Assign each typed database asset below.")]
    [Required, AssetsOnly, BoxGroup("Sub-Databases")]
    public BodyShapeDatabaseSO BodyShapes;

    [Required, AssetsOnly, BoxGroup("Sub-Databases")]
    public HornDatabaseSO Horns;

    [Required, AssetsOnly, BoxGroup("Sub-Databases")]
    public BackDatabaseSO Backs;

    [Required, AssetsOnly, BoxGroup("Sub-Databases")]
    public WingDatabaseSO Wings;

    [Required, AssetsOnly, BoxGroup("Sub-Databases")]
    public FaceDatabaseSO Faces;

    [Button("Validate — Check for Duplicate IDs Across All Databases", ButtonSizes.Large)]
    [GUIColor(1f, 0.8f, 0.2f)]
    public void ValidateAllDatabases()
    {
        var seen     = new HashSet<string>();
        int dupCount = 0;

        Check("BodyShapes", BodyShapes?.GetAllIDs(), seen, ref dupCount);
        Check("Horns",      Horns?.GetAllIDs(),      seen, ref dupCount);
        Check("Backs",      Backs?.GetAllIDs(),      seen, ref dupCount);
        Check("Wings",      Wings?.GetAllIDs(),      seen, ref dupCount);
        Check("Faces",      Faces?.GetAllIDs(),      seen, ref dupCount);

        if (dupCount == 0)
            Debug.Log("[CreatureDB] Validation PASSED — no duplicate IDs found across all databases.");
        else
            Debug.LogError($"[CreatureDB] Validation FAILED — {dupCount} duplicate ID(s) detected. See details above.");
    }

    private static void Check(string category, List<string> ids, HashSet<string> seen, ref int dups)
    {
        if (ids == null) return;
        foreach (string id in ids)
        {
            if (!seen.Add(id))
            {
                Debug.LogError($"  [DUPLICATE] ID '{id}' in '{category}' already exists elsewhere.");
                dups++;
            }
        }
    }

    public BodyShapePart GetBodyShape(string id) => BodyShapes?.GetPartByID(id);
    public HornPart      GetHorn(string id)       => Horns?.GetPartByID(id);
    public BackPart      GetBack(string id)       => Backs?.GetPartByID(id);
    public WingPart      GetWing(string id)       => Wings?.GetPartByID(id);
    public FacePart      GetFace(string id)       => Faces?.GetPartByID(id);
}
}
