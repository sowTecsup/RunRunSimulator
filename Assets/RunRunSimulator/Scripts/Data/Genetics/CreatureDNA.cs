using System;
using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

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
    public Color BaseColor = Color.white;

    [ColorUsage(false)]
    public Color SecondaryColor = Color.white;

    public FurType FurType = FurType.Smooth;

    // ── Identity ──────────────────────────────────────────────────
    public string   CustomName = "";     // editable display name — auto-assigned on Mint/Breed
    public long     Timestamp = 0;       // UTC ticks set on Stamp(); 0 = not yet registered
    public DateTime BirthDate;           // human-readable creation time

    // ── Lineage ───────────────────────────────────────────────────
    public string       MotherID    = "";
    public string       FatherID    = "";
    public List<string> ChildrenIDs = new List<string>();

    // ── Social ────────────────────────────────────────────────────
    public CreatureGender Gender = CreatureGender.Unknown;

    // Behavioral archetype — random at mint/hatch, NOT inherited, NOT in the
    // genetic string. Drives world movement (see MoriMochiAgent / PersonalityProfileSO).
    public Personality Personality = Personality.Curious;

    // ── Progression ───────────────────────────────────────────────
    public int FightCount = 0;
    public int WinCount   = 0;
    public int BreedCount = 0;

    // ── Tier per slot ─────────────────────────────────────────────
    public Tier BodyTier  = Tier.Tier1;
    public Tier ArmTier   = Tier.Tier1;
    public Tier EyeTier   = Tier.Tier1;
    public Tier MouthTier = Tier.Tier1;

    // ── Base Stats (point-buy en Mint, heredados en Breed) ─
    public float BaseConstitution = 0f;
    public float BaseAttack       = 0f;
    public float BaseSpeed        = 0f;

    // ── Gear-derived Stats (start at 0, filled by future equipment) ─
    public float BaseDefense  = 0f;
    public float BaseLuck     = 0f;
    public float BaseEvasion  = 0f;

    // ── Combat history ────────────────────────────────────────────
    // One CombatRecord per finished fight (local + async), turn-by-turn for the
    // future Combat Visualizer. Persisted with the DNA (local + cloud), unbounded
    // — MaxFightCount can change, so we don't cap it here.
    public List<CombatRecord> CombatHistory = new List<CombatRecord>();

    // ── Mortality ─────────────────────────────────────────────────
    public bool IsDead = false;

    // ── Runtime needs (World) ─────────────────────────────────────
    // Mutable wellbeing (health/energy/affect) the MoriMochiAgent ticks while spawned. Lives here
    // because CreatureDNA is already the persisted save record (like CombatHistory/BusyState) — so
    // it rides the local + Cloud Save with zero extra plumbing. NOT part of the genetic string.
    public NeedsState Needs = new NeedsState();

    // ── Busy state ────────────────────────────────────────────────
    public BusyReason BusyState = BusyReason.None;
    public bool IsBusy => BusyState != BusyReason.None;
    public bool IsSold => BusyState == BusyReason.Sold;

    // When this creature was enqueued for async combat (UTC). Display-only metadata
    // for the Resultados tab ("encolado a las HH:mm"); set on enqueue, not part of
    // the genetic string. default = never queued.
    public DateTime QueuedAt;
    public DateTime SaleDate;

    // ── Breeding timer (local cache for display; server is authoritative) ─
    public long   BreedReadyAt   = 0;    // server epoch ms when the egg can hatch; 0 = not breeding
    public string BreedPartnerID = "";   // the other parent — lets the client rebuild the pair locally
    public string LocationKey  = "";   // furniture cell key ("x_y") of the place this creature is anchored to (pen / shelf / corral); "" = free roamer (cannon)
    public int    LocationSlot = -1;   // fixed slot index inside that place; -1 = unassigned

    // ── Equipment (typed slots; stores EquipmentSO IDs, resolved vs EquipmentDatabaseSO) ─
    // Metadata like Gender/Personality — NOT part of the genetic string. Persists with
    // the DNA (local + cloud) as light IDs. Edited via the typed slot fields below
    // (hidden raw so the drag-drop slots are the single edit surface).
    [HideInInspector]
    public Dictionary<EquipmentSlot, string> Equipped = new Dictionary<EquipmentSlot, string>();

#if UNITY_EDITOR
    [Sirenix.OdinInspector.ShowInInspector, Sirenix.OdinInspector.BoxGroup("Equipment"), Sirenix.OdinInspector.AssetsOnly, Sirenix.OdinInspector.LabelText("Weapon")]
    private EquipmentSO WeaponSlotEditor { get => ResolveSlot(EquipmentSlot.Weapon); set => AssignSlot(EquipmentSlot.Weapon, value); }
    [Sirenix.OdinInspector.ShowInInspector, Sirenix.OdinInspector.BoxGroup("Equipment"), Sirenix.OdinInspector.AssetsOnly, Sirenix.OdinInspector.LabelText("Armor")]
    private EquipmentSO ArmorSlotEditor  { get => ResolveSlot(EquipmentSlot.Armor);  set => AssignSlot(EquipmentSlot.Armor, value); }
    [Sirenix.OdinInspector.ShowInInspector, Sirenix.OdinInspector.BoxGroup("Equipment"), Sirenix.OdinInspector.AssetsOnly, Sirenix.OdinInspector.LabelText("Amulet")]
    private EquipmentSO AmuletSlotEditor { get => ResolveSlot(EquipmentSlot.Amulet); set => AssignSlot(EquipmentSlot.Amulet, value); }

    private EquipmentSO ResolveSlot(EquipmentSlot slot) =>
        Equipped != null && Equipped.TryGetValue(slot, out var id) ? EquipmentDatabaseSO.Editor?.GetByID(id) : null;

    private void AssignSlot(EquipmentSlot slot, EquipmentSO so)
    {
        Equipped ??= new Dictionary<EquipmentSlot, string>();
        if (so == null) { Equipped.Remove(slot); return; }
        if (so.Slot != slot)
        {
            Debug.LogWarning($"[CreatureDNA] '{so.Name}' es de slot {so.Slot}, no encaja en {slot}.");
            return;
        }
        Equipped[slot] = so.ID;
    }
#endif

    // Whole days lived since BirthDate (UTC). Drives the NameTag life stage; 0 until stamped.
    public int AgeDays => BirthDate == default ? 0 : Mathf.Max(0, (int)(DateTime.UtcNow - BirthDate).TotalDays);

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
        $"{BodyShapeID}-{ArmID}-{EyeID}-{MouthID}-{ColorUtility.ToHtmlStringRGB(BaseColor)}";

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

        if (!ColorUtility.TryParseHtmlString("#" + colorHex, out dna.BaseColor))
            Debug.LogWarning($"[CreatureDNA] Could not parse color '{colorHex}', defaulting to white.");

        return dna;
    }
}
}
