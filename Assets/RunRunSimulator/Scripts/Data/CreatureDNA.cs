using System;
using System.Collections.Generic;
using UnityEngine;

// Genetic string format: "BODYSHAPE-ARM-EYE-MOUTH-RRGGBB"  (e.g. "BS0-A3-E1-M2-FF00AA")
// Registry key (UniqueID): "<genetic_string>-<timestamp_ticks>"
// Part IDs must not contain '-'.
[Serializable]
public class CreatureDNA
{
    // ── Genetics ──────────────────────────────────────────────────
    public string BodyShapeID = "";
    public string ArmID       = "";
    public string EyeID       = "";
    public string MouthID     = "";

    [ColorUsage(false)]
    public Color PrimaryColor = Color.white;

    // ── Identity ──────────────────────────────────────────────────
    public long     Timestamp = 0;       // UTC ticks set on Stamp(); 0 = not yet registered
    public DateTime BirthDate;           // human-readable creation time

    // ── Lineage ───────────────────────────────────────────────────
    public string       MotherID    = "";
    public string       FatherID    = "";
    public List<string> ChildrenIDs = new List<string>();

    // ── Social ────────────────────────────────────────────────────
    public CreatureGender Gender = CreatureGender.Unknown;

    // ── Progression ───────────────────────────────────────────────
    public int FightCount = 0;
    public int WinCount   = 0;
    public int BreedCount = 0;

    // ── Tier per slot ─────────────────────────────────────────────
    public Tier BodyTier  = Tier.Tier1;
    public Tier ArmTier   = Tier.Tier1;
    public Tier EyeTier   = Tier.Tier1;
    public Tier MouthTier = Tier.Tier1;

    // ── Base Stats (asignados en Mint por StatCalculator — Etapa 2.1) ─
    public float BaseHP     = 0f;
    public float BaseAttack = 0f;
    public float BaseSpeed  = 0f;

    // ── Mortality ─────────────────────────────────────────────────
    public bool IsDead = false;

    // Unique registry key: two creatures with identical genes are still different entries.
    public string UniqueID => Timestamp > 0 ? $"{ToStringID()}-{Timestamp}" : "";

    // Call once before registering. Sets Timestamp and BirthDate atomically.
    public void Stamp()
    {
        var now   = DateTime.UtcNow;
        Timestamp = now.Ticks;
        BirthDate = now;
    }

    public string ToStringID() =>
        $"{BodyShapeID}-{ArmID}-{EyeID}-{MouthID}-{ColorUtility.ToHtmlStringRGB(PrimaryColor)}";

    // Returns "{body} {arm} {eye} {mouth}" using part Name fields; falls back to part IDs.
    public string GetDisplayName(CreatureDatabaseSO db)
    {
        string body  = db?.GetBodyShape(BodyShapeID)?.Name ?? BodyShapeID;
        string arm   = db?.GetArm(ArmID)?.Name             ?? ArmID;
        string eye   = db?.GetEye(EyeID)?.Name             ?? EyeID;
        string mouth = db?.GetMouth(MouthID)?.Name         ?? MouthID;
        return $"{body} {arm} {eye} {mouth}";
    }

    // Parses only the genetic string (BODYSHAPE-ARM-EYE-MOUTH-RRGGBB).
    // Does not parse Timestamp or lineage — use JSON deserialization for full state.
    public static CreatureDNA FromID(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("[CreatureDNA] Cannot parse a null or empty ID.");
            return new CreatureDNA();
        }

        int lastDash = id.LastIndexOf('-');
        if (lastDash < 0 || id.Length - lastDash - 1 != 6)
        {
            Debug.LogError($"[CreatureDNA] Invalid ID '{id}'. Expected: BODYSHAPE-ARM-EYE-MOUTH-RRGGBB");
            return new CreatureDNA();
        }

        string   colorHex = id.Substring(lastDash + 1);
        string[] parts    = id.Substring(0, lastDash).Split('-');

        if (parts.Length != 4)
        {
            Debug.LogError($"[CreatureDNA] Expected 4 part tokens, got {parts.Length} in '{id}'.");
            return new CreatureDNA();
        }

        var dna = new CreatureDNA
        {
            BodyShapeID = parts[0],
            ArmID       = parts[1],
            EyeID       = parts[2],
            MouthID     = parts[3],
        };

        if (!ColorUtility.TryParseHtmlString("#" + colorHex, out dna.PrimaryColor))
            Debug.LogWarning($"[CreatureDNA] Could not parse color '{colorHex}', defaulting to white.");

        return dna;
    }
}
