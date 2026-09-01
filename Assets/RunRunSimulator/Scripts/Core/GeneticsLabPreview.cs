using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
namespace MoriMonchiSimulator
{

public class GeneticsLabPreview : MonoBehaviour
{

    [BoxGroup("Setup"), Required]
    [SerializeField] private GameManager gameManager;

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

    [ShowInInspector, ReadOnly, LabelText("Horn"), LabelWidth(80), BoxGroup("Current Creature/Rarity")]
    private Rarity rarityHorn;

    [ShowInInspector, ReadOnly, LabelText("Back"), LabelWidth(80), BoxGroup("Current Creature/Rarity")]
    private Rarity rarityBack;

    [ShowInInspector, ReadOnly, LabelText("Wing"), LabelWidth(80), BoxGroup("Current Creature/Rarity")]
    private Rarity rarityWing;

    [ShowInInspector, ReadOnly, LabelText("Face"), LabelWidth(80), BoxGroup("Current Creature/Rarity")]
    private Rarity rarityFace;

    [ShowInInspector, ReadOnly, LabelText("Score"), LabelWidth(80), BoxGroup("Current Creature/Rarity")]
    private string rarityScore = "---";

    [BoxGroup("Load by ID")]
    [InfoBox("Format: BODYSHAPEID-HORNID-BACKID-WINGID-FACEID-RRGGBB   (e.g.  BS0-H3-K1-W2-F4-FF00AA)")]
    [FormerlySerializedAs("_loadIDInput")]
    [SerializeField, LabelText("DNA String")]
    private string loadIDInput = "";

    [Button("Generate Random Creature", ButtonSizes.Large), GUIColor(0.4f, 0.85f, 0.4f)]
    [BoxGroup("Current Creature")]
    private void GenerateRandomCreature()
    {
        if (gameManager == null) { Debug.LogWarning("[GeneticsLabPreview] No GameManager assigned."); return; }
        currentDNA       = CreatureGenerator.GenerateRandom(gameManager.Database);
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

    private void RefreshRarityBreakdown()
    {
        var database = gameManager.Database;
        if (database == null) return;

        var bodyShape = database.GetBodyShape(currentDNA.BodyShapeID);
        var horn      = database.GetHorn(currentDNA.HornID);
        var back      = database.GetBack(currentDNA.BackID);
        var wing      = database.GetWing(currentDNA.WingID);
        var face      = database.GetFace(currentDNA.FaceID);

        rarityBodyShape = bodyShape?.Rarity ?? Rarity.Common;
        rarityHorn      = horn?.Rarity      ?? Rarity.Common;
        rarityBack      = back?.Rarity      ?? Rarity.Common;
        rarityWing      = wing?.Rarity      ?? Rarity.Common;
        rarityFace      = face?.Rarity      ?? Rarity.Common;

        float avg   = ((int)rarityBodyShape + (int)rarityHorn + (int)rarityBack + (int)rarityWing + (int)rarityFace) / 5f;
        rarityScore = $"{(Rarity)Mathf.RoundToInt(avg)}  (avg {avg:F2})";
    }

    private void ValidateDNA(CreatureDNA dna)
    {
        var database = gameManager.Database;
        if (database == null) return;
        LogPart("Body", database.GetBodyShape(dna.BodyShapeID), dna.BodyShapeID);
        LogPart("Horn", database.GetHorn(dna.HornID),           dna.HornID);
        LogPart("Back", database.GetBack(dna.BackID),           dna.BackID);
        LogPart("Wing", database.GetWing(dna.WingID),           dna.WingID);
        LogPart("Face", database.GetFace(dna.FaceID),           dna.FaceID);
    }

    private static void LogPart(string label, BodyPart part, string id)
    {
        if (part != null) Debug.Log($"  [OK] {label,-6} → [{id}] {part.Name}  ({part.Rarity})");
        else              Debug.LogWarning($"  [!!] {label,-6} → ID '{id}' not found in database.");
    }
}
}
