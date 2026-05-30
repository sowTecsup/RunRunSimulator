using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "InheritanceOddsTable", menuName = "RunRunSimulator/Inheritance Odds Table")]
public class InheritanceOddsTableSO : SerializedScriptableObject
{
    public enum Slot { Parent, Grandparent, GreatGrandparent, Mutation, Base }

    // ── Singleton ─────────────────────────────────────────────────
    public static InheritanceOddsTableSO Current { get; private set; }
    private void OnEnable() => Current = this;

    // ── Weights ───────────────────────────────────────────────────
    [InfoBox("Relative weights — normalized internally.")]
    [LabelWidth(190)] public float ParentWeight           = 40f;
    [LabelWidth(190)] public float GrandparentWeight      = 20f;
    [LabelWidth(190)] public float GreatGrandparentWeight = 10f;
    [LabelWidth(190)] public float MutationWeight         = 20f;
    [LabelWidth(190)] public float BaseWeight             = 10f;

    [ShowInInspector, ReadOnly, LabelWidth(190)]
    private float TotalWeight =>
        ParentWeight + GrandparentWeight + GreatGrandparentWeight + MutationWeight + BaseWeight;

    // ── Breeding timer ────────────────────────────────────────────
    // Display/reference only. The authoritative duration is hardcoded server-side
    // in start-breeding.js (BREED_DURATION_MS). Changing this does NOT change the
    // real timer — same known limitation as CombatManagerSO.DeathChance.
    [InfoBox("Solo display/referencia. La duración real está hardcoded en start-breeding.js (server-side).")]
    [LabelWidth(190)] public int BreedDurationMinutes = 30;

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
}
