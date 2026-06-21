using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
namespace MoriMonchiSimulator
{

public class GeneticsLabPreview : MonoBehaviour
{
    // ── Setup ─────────────────────────────────────────────────────

    [BoxGroup("Setup"), Required]
    [SerializeField] private GameManager gameManager;

    // ── Current Creature ──────────────────────────────────────────

    [BoxGroup("Current Creature")]
    [FormerlySerializedAs("_currentDNA")]
    [SerializeField, ReadOnly, InlineProperty, HideLabel]
    private CreatureDNA currentDNA = new CreatureDNA();

    [BoxGroup("Current Creature")]
    [FormerlySerializedAs("_currentDNAString")]
    [SerializeField, ReadOnly, LabelText("DNA String")]
    private string currentDNAString = "---";

    [Title("Rarity Breakdown")]
    [ShowInInspector, ReadOnly, LabelText("Body Shape"), LabelWidth(80)]
    [BoxGroup("Current Creature/Rarity")]
    private Rarity rarityBodyShape;

    [ShowInInspector, ReadOnly, LabelText("Arms"), LabelWidth(80), BoxGroup("Current Creature/Rarity")]
    private Rarity rarityArms;

    [ShowInInspector, ReadOnly, LabelText("Eyes"), LabelWidth(80), BoxGroup("Current Creature/Rarity")]
    private Rarity rarityEyes;

    [ShowInInspector, ReadOnly, LabelText("Mouth"), LabelWidth(80), BoxGroup("Current Creature/Rarity")]
    private Rarity rarityMouth;

    [ShowInInspector, ReadOnly, LabelText("Score"), LabelWidth(80), BoxGroup("Current Creature/Rarity")]
    private string rarityScore = "---";

    // ── Load by ID ────────────────────────────────────────────────

    [BoxGroup("Load by ID")]
    [InfoBox("Format: BODYSHAPEID-ARMID-EYEID-MOUTHID-RRGGBB   (e.g.  BS0-A3-E1-M2-FF00AA)")]
    [FormerlySerializedAs("_loadIDInput")]
    [SerializeField, LabelText("DNA String")]
    private string loadIDInput = "";

    // ── Buttons ───────────────────────────────────────────────────

    [Button("Generate Random Creature", ButtonSizes.Large), GUIColor(0.4f, 0.85f, 0.4f)]
    [BoxGroup("Current Creature")]
    private void GenerateRandomCreature()
    {
        if (gameManager == null) { Debug.LogWarning("[GeneticsLabPreview] No GameManager assigned."); return; }
        currentDNA       = CreatureGenerator.GenerateRandom(gameManager.Database, gameManager.RarityOddsTable);
        currentDNAString = currentDNA.ToStringID();
        RefreshRarityBreakdown();
        Debug.Log($"[GeneticsLabPreview] Generated (preview): {currentDNAString}");
    }

    [Button("Load from ID"), GUIColor(0.4f, 0.6f, 0.95f), BoxGroup("Load by ID")]
    private void LoadFromID()
    {
        if (gameManager == null) { Debug.LogWarning("[GeneticsLabPreview] No GameManager assigned."); return; }
        if (string.IsNullOrWhiteSpace(loadIDInput)) { Debug.LogWarning("[GeneticsLabPreview] No ID entered."); return; }

        currentDNA       = CreatureDNA.FromID(loadIDInput);
        currentDNAString = currentDNA.ToStringID();
        RefreshRarityBreakdown();
        Debug.Log($"[GeneticsLabPreview] Loaded: {currentDNAString}");
        ValidateDNA(currentDNA);
    }

    // ── Private Methods ───────────────────────────────────────────

    private void RefreshRarityBreakdown()
    {
        var database = gameManager.Database;
        if (database == null) return;

        var bodyShape = database.GetBodyShape(currentDNA.BodyShapeID);
        var arm       = database.GetArm(currentDNA.ArmID);
        var eye       = database.GetEye(currentDNA.EyeID);
        var mouth     = database.GetMouth(currentDNA.MouthID);

        rarityBodyShape = bodyShape?.Rarity ?? Rarity.Common;
        rarityArms      = arm?.Rarity       ?? Rarity.Common;
        rarityEyes      = eye?.Rarity       ?? Rarity.Common;
        rarityMouth     = mouth?.Rarity     ?? Rarity.Common;

        float avg   = ((int)rarityBodyShape + (int)rarityArms + (int)rarityEyes + (int)rarityMouth) / 4f;
        rarityScore = $"{(Rarity)Mathf.RoundToInt(avg)}  (avg {avg:F2})";
    }

    private void ValidateDNA(CreatureDNA dna)
    {
        var database = gameManager.Database;
        if (database == null) return;
        LogPart("Body",  database.GetBodyShape(dna.BodyShapeID), dna.BodyShapeID);
        LogPart("Arms",  database.GetArm(dna.ArmID),             dna.ArmID);
        LogPart("Eyes",  database.GetEye(dna.EyeID),             dna.EyeID);
        LogPart("Mouth", database.GetMouth(dna.MouthID),         dna.MouthID);
    }

    private static void LogPart(string label, BodyPart part, string id)
    {
        if (part != null) Debug.Log($"  [OK] {label,-6} → [{id}] {part.Name}  ({part.Rarity})");
        else              Debug.LogWarning($"  [!!] {label,-6} → ID '{id}' not found in database.");
    }
}
}
