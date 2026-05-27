using System;
using System.IO;
using Unity.Plastic.Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

// Requires: com.unity.nuget.newtonsoft-json package in the project.
[CreateAssetMenu(fileName = "InheritanceOddsTable", menuName = "RunRunSimulator/Inheritance Odds Table")]
public class InheritanceOddsTableSO : SerializedScriptableObject
{
    public enum Slot { Parent, Grandparent, GreatGrandparent, Mutation, Base }

    // ── Singleton ─────────────────────────────────────────────────
    public static InheritanceOddsTableSO Current { get; private set; }
    private void OnEnable() => Current = this;

    // ── Weights ───────────────────────────────────────────────────
    [InfoBox("Relative weights — normalized internally. Edit and press 'Save to JSON' to hot-reload in-game.")]
    [LabelWidth(190)] public float ParentWeight           = 40f;
    [LabelWidth(190)] public float GrandparentWeight      = 20f;
    [LabelWidth(190)] public float GreatGrandparentWeight = 10f;
    [LabelWidth(190)] public float MutationWeight         = 20f;
    [LabelWidth(190)] public float BaseWeight             = 10f;

    [ShowInInspector, ReadOnly, LabelWidth(190)]
    private float TotalWeight =>
        ParentWeight + GrandparentWeight + GreatGrandparentWeight + MutationWeight + BaseWeight;

    // ── Roll ──────────────────────────────────────────────────────
    public Slot Roll()
    {
        float total = TotalWeight;
        if (total <= 0f) return Slot.Parent;

        float roll = UnityEngine.Random.Range(0f, total);
        float c    = 0f;

        c += ParentWeight;            if (roll < c) return Slot.Parent;
        c += GrandparentWeight;       if (roll < c) return Slot.Grandparent;
        c += GreatGrandparentWeight;  if (roll < c) return Slot.GreatGrandparent;
        c += MutationWeight;          if (roll < c) return Slot.Mutation;
        return Slot.Base;
    }

    // ── JSON hot-reload ───────────────────────────────────────────
    private static string JsonPath =>
        Path.Combine(Application.persistentDataPath, "inheritance_odds.json");

    [ButtonGroup("JSON"), Button("Save to JSON"), GUIColor(1f, 0.85f, 0.3f)]
    public void SaveToJson()
    {
        var data = new OddsJson
        {
            parent           = ParentWeight,
            grandparent      = GrandparentWeight,
            greatGrandparent = GreatGrandparentWeight,
            mutation         = MutationWeight,
            @base            = BaseWeight,
        };
        File.WriteAllText(JsonPath, JsonConvert.SerializeObject(data, Formatting.Indented));
        Debug.Log($"[InheritanceOddsTable] Saved → {JsonPath}");
    }

    [ButtonGroup("JSON"), Button("Load from JSON"), GUIColor(0.5f, 0.85f, 1f)]
    public void LoadFromJson()
    {
        if (!File.Exists(JsonPath))
        {
            Debug.LogWarning($"[InheritanceOddsTable] File not found: {JsonPath}");
            return;
        }
        var data = JsonConvert.DeserializeObject<OddsJson>(File.ReadAllText(JsonPath));
        if (data == null) return;

        ParentWeight           = data.parent;
        GrandparentWeight      = data.grandparent;
        GreatGrandparentWeight = data.greatGrandparent;
        MutationWeight         = data.mutation;
        BaseWeight             = data.@base;
        Debug.Log($"[InheritanceOddsTable] Loaded from {JsonPath}");
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    [Serializable]
    private class OddsJson
    {
        public float parent;
        public float grandparent;
        public float greatGrandparent;
        public float mutation;
        public float @base;
    }
}
