using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

// One entry in the visual bank: a read-only part ID (sourced from the part database)
// paired with the BodyPartJoint prefab to spawn. Typing the value as BodyPartJoint
// makes the inspector reject any prefab that lacks the component.
[Serializable]
public class PartVisualEntry
{
    [HorizontalGroup, ReadOnly, LabelWidth(50), LabelText("ID")]
    public string       partId;
    [HorizontalGroup, HideLabel, AssetsOnly]
    public BodyPartJoint prefab;
}

// Maps part IDs → BodyPartJoint prefabs for runtime assembly by MoriMonchiVisualizer.
// One asset per project, referenced from GameManager.
// 1. "Populate from Database" syncs the ID rows from the part databases.
// 2. Assign prefabs (or "Fill Empty with Defaults" to drop the base parts into the blanks).
[CreateAssetMenu(menuName = "RunRunSimulator/Databases/Part Visual Bank")]
public class PartVisualBankSO : SerializedScriptableObject
{
    [Required, AssetsOnly, BoxGroup("Setup")]
    [SerializeField] private CreatureDatabaseSO creatureDatabase;

    [Title("Default parts", "Used by 'Fill Empty with Defaults' for any unassigned entry.")]
    [AssetsOnly, BoxGroup("Setup")] [SerializeField] private BodyPartJoint baseBody;
    [AssetsOnly, BoxGroup("Setup")] [SerializeField] private BodyPartJoint baseArm;
    [AssetsOnly, BoxGroup("Setup")] [SerializeField] private BodyPartJoint baseEye;
    [AssetsOnly, BoxGroup("Setup")] [SerializeField] private BodyPartJoint baseMouth;

    [ButtonGroup("Setup/Buttons")]
    [Button("Populate from Database", ButtonSizes.Large), GUIColor(0.4f, 1f, 0.6f)]
    private void PopulateFromDatabase()
    {
#if UNITY_EDITOR
        if (creatureDatabase == null)
        {
            Debug.LogWarning("[PartVisualBankSO] Assign a CreatureDatabaseSO first.");
            return;
        }

        bool changed = false;
        changed |= SyncSlot(ref bodies,  creatureDatabase.BodyShapes?.GetAllIDs(), "Bodies");
        changed |= SyncSlot(ref arms,    creatureDatabase.Arms?.GetAllIDs(),        "Arms");
        changed |= SyncSlot(ref eyes,    creatureDatabase.Eyes?.GetAllIDs(),        "Eyes");
        changed |= SyncSlot(ref mouths,  creatureDatabase.Mouths?.GetAllIDs(),      "Mouths");

        RebuildCaches();
        if (changed) UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log(changed
            ? "[PartVisualBankSO] Sync complete — assign prefabs to any new entries."
            : "[PartVisualBankSO] Already up to date.");
#endif
    }

    [ButtonGroup("Setup/Buttons")]
    [Button("Fill Empty with Defaults", ButtonSizes.Large), GUIColor(0.5f, 0.8f, 1f)]
    private void FillEmptyWithDefaults()
    {
#if UNITY_EDITOR
        int filled = 0;
        filled += FillSlot(bodies, baseBody);
        filled += FillSlot(arms,   baseArm);
        filled += FillSlot(eyes,   baseEye);
        filled += FillSlot(mouths, baseMouth);

        RebuildCaches();
        if (filled > 0) UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[PartVisualBankSO] Filled {filled} empty entr{(filled == 1 ? "y" : "ies")} with defaults.");
#endif
    }

    [BoxGroup("Bodies")]
    [ListDrawerSettings(DraggableItems = false, HideAddButton = true, HideRemoveButton = true)]
    [SerializeField] private List<PartVisualEntry> bodies = new List<PartVisualEntry>();

    [BoxGroup("Arms")]
    [ListDrawerSettings(DraggableItems = false, HideAddButton = true, HideRemoveButton = true)]
    [SerializeField] private List<PartVisualEntry> arms   = new List<PartVisualEntry>();

    [BoxGroup("Eyes")]
    [ListDrawerSettings(DraggableItems = false, HideAddButton = true, HideRemoveButton = true)]
    [SerializeField] private List<PartVisualEntry> eyes   = new List<PartVisualEntry>();

    [BoxGroup("Mouths")]
    [ListDrawerSettings(DraggableItems = false, HideAddButton = true, HideRemoveButton = true)]
    [SerializeField] private List<PartVisualEntry> mouths = new List<PartVisualEntry>();

    // ── Runtime caches (O(1) lookup, rebuilt on load / editor changes) ──

    private Dictionary<string, BodyPartJoint> _bodies;
    private Dictionary<string, BodyPartJoint> _arms;
    private Dictionary<string, BodyPartJoint> _eyes;
    private Dictionary<string, BodyPartJoint> _mouths;

    private void OnEnable()   => RebuildCaches();
    private void OnValidate() => RebuildCaches();

    private void RebuildCaches()
    {
        _bodies = ToDict(bodies);
        _arms   = ToDict(arms);
        _eyes   = ToDict(eyes);
        _mouths = ToDict(mouths);
    }

    // ── Public lookup ─────────────────────────────────────────────

    public BodyPartJoint GetBody(string id)  => Lookup(_bodies ?? ToDict(bodies), id, "Body");
    public BodyPartJoint GetArm(string id)   => Lookup(_arms   ?? ToDict(arms),   id, "Arm");
    public BodyPartJoint GetEye(string id)   => Lookup(_eyes   ?? ToDict(eyes),   id, "Eye");
    public BodyPartJoint GetMouth(string id) => Lookup(_mouths ?? ToDict(mouths), id, "Mouth");

    // ── Private helpers ───────────────────────────────────────────

    private static Dictionary<string, BodyPartJoint> ToDict(List<PartVisualEntry> list)
    {
        var d = new Dictionary<string, BodyPartJoint>();
        if (list == null) return d;
        foreach (var e in list)
            if (!string.IsNullOrEmpty(e.partId))
                d[e.partId] = e.prefab;
        return d;
    }

    private static BodyPartJoint Lookup(Dictionary<string, BodyPartJoint> dict, string id, string slot)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (dict.TryGetValue(id, out var p) && p != null) return p;
        Debug.LogWarning($"[PartVisualBankSO] No {slot} prefab for ID '{id}'.");
        return null;
    }

    private static int FillSlot(List<PartVisualEntry> entries, BodyPartJoint def)
    {
        if (entries == null || def == null) return 0;
        int filled = 0;
        foreach (var e in entries)
            if (e.prefab == null) { e.prefab = def; filled++; }
        return filled;
    }

    // Adds entries for IDs missing from the list, removes entries whose IDs are gone
    // from the database, and keeps existing prefab assignments intact.
    private static bool SyncSlot(ref List<PartVisualEntry> entries, List<string> dbIds, string label)
    {
        entries ??= new List<PartVisualEntry>();

        if (dbIds == null || dbIds.Count == 0)
        {
            Debug.LogWarning($"[PartVisualBankSO] No IDs found for {label} — is the database assigned and populated?");
            return false;
        }

        bool changed  = false;
        var  existing = new HashSet<string>(entries.Select(e => e.partId));
        var  dbSet    = new HashSet<string>(dbIds);

        foreach (var id in dbIds)
            if (!existing.Contains(id))
            {
                entries.Add(new PartVisualEntry { partId = id });
                changed = true;
            }

        if (entries.RemoveAll(e => !dbSet.Contains(e.partId)) > 0) changed = true;

        entries.Sort((a, b) => string.Compare(a.partId, b.partId, StringComparison.Ordinal));
        return changed;
    }
}
