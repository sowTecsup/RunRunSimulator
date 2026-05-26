using Sirenix.OdinInspector;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Required, AssetsOnly]
    [Title("RunRunSimulator — Genetics Lab", "Assign the Creature Database to begin.", TitleAlignments.Centered)]
    [BoxGroup("Setup")]
    [SerializeField] private CreatureDatabaseSO _database;

    // ──────────────── Current Creature ────────────────

    [BoxGroup("Current Creature")]
    [SerializeField, ReadOnly, InlineProperty, HideLabel]
    private CreatureDNA _currentDNA = new CreatureDNA();

    [BoxGroup("Current Creature")]
    [SerializeField, ReadOnly, LabelText("DNA String")]
    private string _currentDNAString = "---";

    [Button("Generate Random Creature", ButtonSizes.Large), GUIColor(0.4f, 0.85f, 0.4f)]
    [BoxGroup("Current Creature")]
    private void GenerateRandomCreature()
    {
        _currentDNA       = CreatureGenerator.GenerateRandom(_database);
        _currentDNAString = _currentDNA.ToStringID();
        RefreshRarityBreakdown();
        Debug.Log($"[GameManager] Generated: {_currentDNAString}");
    }

    // ──────────────── Rarity Breakdown ────────────────

    [Title("Rarity Breakdown")]
    [BoxGroup("Current Creature / Rarity")]
    [ShowInInspector, ReadOnly, LabelText("Body Shape"), LabelWidth(80)]
    private Rarity _rarityBodyShape;

    [BoxGroup("Current Creature / Rarity")]
    [ShowInInspector, ReadOnly, LabelText("Arms"), LabelWidth(80)]
    private Rarity _rarityArms;

    [BoxGroup("Current Creature / Rarity")]
    [ShowInInspector, ReadOnly, LabelText("Eyes"), LabelWidth(80)]
    private Rarity _rarityEyes;

    [BoxGroup("Current Creature / Rarity")]
    [ShowInInspector, ReadOnly, LabelText("Mouth"), LabelWidth(80)]
    private Rarity _rarityMouth;

    [BoxGroup("Current Creature / Rarity")]
    [ShowInInspector, ReadOnly, LabelText("Score"), LabelWidth(80)]
    private string _rarityScore = "---";

    private void RefreshRarityBreakdown()
    {
        if (_database == null) return;

        var bodyShape = _database.GetBodyShape(_currentDNA.BodyShapeID);
        var arm       = _database.GetArm(_currentDNA.ArmID);
        var eye       = _database.GetEye(_currentDNA.EyeID);
        var mouth     = _database.GetMouth(_currentDNA.MouthID);

        _rarityBodyShape = bodyShape?.Rarity ?? Rarity.Common;
        _rarityArms      = arm?.Rarity       ?? Rarity.Common;
        _rarityEyes      = eye?.Rarity       ?? Rarity.Common;
        _rarityMouth     = mouth?.Rarity     ?? Rarity.Common;

        float avg = ((int)_rarityBodyShape + (int)_rarityArms + (int)_rarityEyes + (int)_rarityMouth) / 4f;
        _rarityScore = $"{(Rarity)Mathf.RoundToInt(avg)}  (avg {avg:F2})";
    }

    // ──────────────── Load by ID ────────────────

    [BoxGroup("Load by ID")]
    [InfoBox("Format: BODYSHAPEID-ARMID-EYEID-MOUTHID-RRGGBB   (e.g.  BS1-A4-E2-M3-FF00AA)")]
    [SerializeField, LabelText("DNA String")]
    private string _loadIDInput = "";

    [Button("Load from ID"), GUIColor(0.4f, 0.6f, 0.95f)]
    [BoxGroup("Load by ID")]
    private void LoadFromID()
    {
        if (string.IsNullOrWhiteSpace(_loadIDInput))
        {
            Debug.LogWarning("[GameManager] No ID entered.");
            return;
        }

        _currentDNA       = CreatureDNA.FromID(_loadIDInput);
        _currentDNAString = _currentDNA.ToStringID();
        RefreshRarityBreakdown();

        Debug.Log($"[GameManager] Loaded: {_currentDNAString}");
        ValidateDNA(_currentDNA);
    }

    private void ValidateDNA(CreatureDNA dna)
    {
        if (_database == null) return;

        LogPart("Body", _database.GetBodyShape(dna.BodyShapeID), dna.BodyShapeID);
        LogPart("Arms", _database.GetArm(dna.ArmID),             dna.ArmID);
        LogPart("Eyes", _database.GetEye(dna.EyeID),             dna.EyeID);
        LogPart("Mouth", _database.GetMouth(dna.MouthID),        dna.MouthID);
    }

    private static void LogPart(string label, BodyPart part, string id)
    {
        if (part != null)
            Debug.Log($"  [OK] {label,-6} → [{id}] {part.Name}  ({part.Rarity})");
        else
            Debug.LogWarning($"  [!!] {label,-6} → ID '{id}' not found in database.");
    }
}
