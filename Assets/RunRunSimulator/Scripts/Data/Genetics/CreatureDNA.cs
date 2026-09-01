using System;
using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

[Serializable]
public class CreatureDNA
{
    public string BodyShapeID = "";
    public string HornID      = "";
    public string BackID      = "";
    public string WingID      = "";
    public string FaceID      = "";

    [ColorUsage(false)]
    public Color BaseColor = Color.white;

    [ColorUsage(false)]
    public Color SecondaryColor = Color.white;

    public FurType FurType = FurType.Pattern00;
    public bool IsShiny = false;

    public string   CustomName = "";
    public long     Timestamp = 0;
    public DateTime BirthDate;

    public string       MotherID    = "";
    public string       FatherID    = "";
    public List<string> ChildrenIDs = new List<string>();

    public CreatureGender Gender = CreatureGender.Unknown;

    public Role Role = Role.Protector;

    public Element Element = Element.Agua;

    public float Sociability = 0.5f;
    public float Boldness    = 0.5f;

    public int BreedCount = 0;

    public Tier BodyTier = Tier.Tier1;
    public Tier HornTier = Tier.Tier1;
    public Tier BackTier = Tier.Tier1;
    public Tier WingTier = Tier.Tier1;

    public float BaseConstitution = 0f;
    public float BaseAttack       = 0f;
    public float BaseSpeed        = 0f;

    public float BaseDefense  = 0f;
    public float BaseLuck     = 0f;
    public float BaseEvasion  = 0f;

    public bool IsDead = false;

    public NeedsState Needs = new NeedsState();

    public BusyReason BusyState = BusyReason.None;
    public bool IsBusy => BusyState != BusyReason.None;
    public bool IsSold => BusyState == BusyReason.Sold;

    public DateTime SaleDate;

    public long   BreedReadyAt   = 0;
    public string BreedPartnerID = "";
    public string LocationKey  = "";
    public int    LocationSlot = -1;

    [HideInInspector]
    public Dictionary<EquipmentSlot, string> Equipped = new Dictionary<EquipmentSlot, string>();

    [HideInInspector] public string HeldItemId = "";

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

    public int AgeDays => BirthDate == default ? 0 : Mathf.Max(0, (int)(DateTime.UtcNow - BirthDate).TotalDays);

    public string UniqueID => Timestamp > 0 ? $"{ToStringID()}-{Timestamp}" : "";

    public void Stamp()
    {
        var now   = DateTime.UtcNow;
        Timestamp = now.Ticks;
        BirthDate = now;
    }

    public string ToStringID() =>
        $"{BodyShapeID}-{HornID}-{BackID}-{WingID}-{FaceID}-{ColorUtility.ToHtmlStringRGB(BaseColor)}";

    public string GetDisplayName(CreatureDatabaseSO db)
    {
        string body = db?.GetBodyShape(BodyShapeID)?.Name ?? BodyShapeID;
        string horn = db?.GetHorn(HornID)?.Name            ?? HornID;
        string back = db?.GetBack(BackID)?.Name            ?? BackID;
        string wing = db?.GetWing(WingID)?.Name            ?? WingID;
        return $"{body} {horn} {back} {wing}";
    }

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
            Debug.LogError($"[CreatureDNA] Invalid ID '{id}'. Expected: BODYSHAPE-HORN-BACK-WING-FACE-RRGGBB");
            return new CreatureDNA();
        }

        string   colorHex = id.Substring(lastDash + 1);
        string[] parts    = id.Substring(0, lastDash).Split('-');

        if (parts.Length != 5)
        {
            Debug.LogError($"[CreatureDNA] Expected 5 part tokens, got {parts.Length} in '{id}'.");
            return new CreatureDNA();
        }

        var dna = new CreatureDNA
        {
            BodyShapeID = parts[0],
            HornID      = parts[1],
            BackID      = parts[2],
            WingID      = parts[3],
            FaceID      = parts[4],
        };

        if (!ColorUtility.TryParseHtmlString("#" + colorHex, out dna.BaseColor))
            Debug.LogWarning($"[CreatureDNA] Could not parse color '{colorHex}', defaulting to white.");

        return dna;
    }
}
}
