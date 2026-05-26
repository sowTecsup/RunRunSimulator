using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

[CreateAssetMenu(fileName = "CreatureDatabase", menuName = "RunRunSimulator/Creature Database (Orchestrator)")]
public class CreatureDatabaseSO : SerializedScriptableObject
{
    [Title("Sub-Databases", "Assign each typed database asset below.")]
    [Required, AssetsOnly, BoxGroup("Sub-Databases")]
    public BodyShapeDatabaseSO BodyShapes;

    [Required, AssetsOnly, BoxGroup("Sub-Databases")]
    public ArmDatabaseSO Arms;

    [Required, AssetsOnly, BoxGroup("Sub-Databases")]
    public EyeDatabaseSO Eyes;

    [Required, AssetsOnly, BoxGroup("Sub-Databases")]
    public MouthDatabaseSO Mouths;

    // ──────────────── Validation ────────────────

    [Button("Validate — Check for Duplicate IDs Across All Databases", ButtonSizes.Large)]
    [GUIColor(1f, 0.8f, 0.2f)]
    public void ValidateAllDatabases()
    {
        var seen     = new HashSet<string>();
        int dupCount = 0;

        Check("BodyShapes", BodyShapes?.GetAllIDs(), seen, ref dupCount);
        Check("Arms",       Arms?.GetAllIDs(),       seen, ref dupCount);
        Check("Eyes",       Eyes?.GetAllIDs(),        seen, ref dupCount);
        Check("Mouths",     Mouths?.GetAllIDs(),      seen, ref dupCount);

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

    // ──────────────── API ────────────────

    public BodyShapePart GetBodyShape(string id) => BodyShapes?.GetPartByID(id);
    public ArmPart       GetArm(string id)        => Arms?.GetPartByID(id);
    public EyePart       GetEye(string id)         => Eyes?.GetPartByID(id);
    public MouthPart     GetMouth(string id)       => Mouths?.GetPartByID(id);
}
