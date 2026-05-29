using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

// Breeding UI + flow. Attach to the same GameObject as GameManager.
// Resolves its assets from GameManager.Instance in Awake — no serialized
// cross-references needed.
public class BreedingController : MonoBehaviour
{
    // ── Cached References ─────────────────────────────────────────

    private CreatureRegistrySO     registry;
    private CreatureDatabaseSO     database;
    private InheritanceOddsTableSO inheritanceOdds;

    // ── Private Fields ────────────────────────────────────────────

    [BoxGroup("Breed")]
    [SerializeField, LabelText("Mother ID")]
    private string breedMotherID = "";

    [BoxGroup("Breed")]
    [SerializeField, LabelText("Father ID")]
    private string breedFatherID = "";

    [ShowInInspector, ReadOnly, LabelText("Mother Info"), BoxGroup("Breed")]
    private string motherBreedInfo = "---";

    [ShowInInspector, ReadOnly, LabelText("Father Info"), BoxGroup("Breed")]
    private string fatherBreedInfo = "---";

    [ShowInInspector, ReadOnly, LabelText("Last Child ID"), BoxGroup("Breed")]
    private string lastChildID = "---";

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        var gm      = GameManager.Instance;
        registry       = gm.Registry;
        database       = gm.Database;
        inheritanceOdds = gm.InheritanceOddsTable;
    }

    // ── Private Methods ───────────────────────────────────────────

    [Button("Fill Random Breeders"), GUIColor(0.85f, 0.6f, 1f), BoxGroup("Breed")]
    private void FillRandomBreeders()
    {
        var all     = registry.GetAll().Values.ToList();
        var females = all.Where(d => !d.IsDead && !d.IsBusy && d.Gender == CreatureGender.Female && d.BreedCount < BreedingService.MaxBreedCount).ToList();
        var males   = all.Where(d => !d.IsDead && !d.IsBusy && d.Gender == CreatureGender.Male   && d.BreedCount < BreedingService.MaxBreedCount).ToList();

        if (females.Count == 0 || males.Count == 0)
        {
            Debug.LogError("[BreedingController] Not enough valid breeders — need at least one alive Male and one alive Female under the breed limit.");
            return;
        }

        var mother = females[Random.Range(0, females.Count)];
        var father = males[Random.Range(0, males.Count)];
        breedMotherID = mother.UniqueID;
        breedFatherID = father.UniqueID;
        RefreshBreedInfo();
        Debug.Log($"[BreedingController] Random breeders — Mother: {Clip(mother.UniqueID)} | Father: {Clip(father.UniqueID)}");
    }

    [Button("Breed", ButtonSizes.Large), GUIColor(1f, 0.7f, 0.85f), BoxGroup("Breed")]
    private void BreedButton() => BreedCreatures(breedMotherID, breedFatherID);

    private void RefreshBreedInfo()
    {
        motherBreedInfo = BuildBreedInfo(breedMotherID);
        fatherBreedInfo = BuildBreedInfo(breedFatherID);
    }

    private string BuildBreedInfo(string id)
    {
        if (string.IsNullOrEmpty(id) || !registry.TryGet(id, out var dna)) return "---";
        return dna.IsDead
            ? $"\"{dna.CustomName}\"  {dna.Gender} | DEAD"
            : $"\"{dna.CustomName}\"  {dna.Gender} | Breeds: {dna.BreedCount}/{BreedingService.MaxBreedCount}";
    }

    private static string Clip(string id) => id.Length > 14 ? id[..14] + "…" : id;

    // ── Public Methods ────────────────────────────────────────────

    public void BreedCreatures(string motherID, string fatherID)
    {
        var odds = inheritanceOdds ?? InheritanceOddsTableSO.Current;
        if (odds == null) { Debug.LogError("[BreedingController] No InheritanceOddsTable assigned."); return; }

        var child = BreedingService.Breed(motherID, fatherID, registry, database, odds);
        if (child == null) return;

        child.CustomName = CreatureNameBank.GetRandomName();
        child.Stamp();
        if (!registry.Register(child)) return;

        if (registry.TryGet(motherID, out var mother)) mother.ChildrenIDs.Add(child.UniqueID);
        if (registry.TryGet(fatherID, out var father)) father.ChildrenIDs.Add(child.UniqueID);

        SaveSystem.SaveDatabase(registry);
        GameManager.Instance.PushToCloud();
        lastChildID = child.UniqueID;
        RefreshBreedInfo();
        Debug.Log($"[BreedingController] Bred child: \"{child.CustomName}\"  {child.UniqueID}  ({child.Gender})");
    }
}
