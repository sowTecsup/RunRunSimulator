using System;
using System.Linq;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

// Breeding UI + flow, and the single owner of the breeding services in the scene.
// Attach to the same GameObject as GameManager. Resolves its assets from
// GameManager.Instance in Awake — no serialized cross-references needed.
// Pens (BreedingContainer) don't hold their own AsyncBreedingService / affinity table:
// they ask BreedingController.Instance for them, so there's a single source of truth.
public class BreedingController : MonoBehaviour
{
    public static BreedingController Instance { get; private set; }

    // ── Cached References ─────────────────────────────────────────

    private CreatureRegistrySO     registry;
    private CreatureDatabaseSO     database;
    private InheritanceOddsTableSO inheritanceOdds;

    [BoxGroup("Setup")]
    [SerializeField] private AsyncBreedingService asyncBreedingService;

    [BoxGroup("Setup")]
    [Tooltip("Personality affinity matrix. Falls back to BreedingAffinityTableSO.Current if left empty.")]
    [SerializeField] private BreedingAffinityTableSO affinityTable;

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
        Instance = this;

        var gm      = GameManager.Instance;
        registry       = gm.Registry;
        database       = gm.Database;
        inheritanceOdds = gm.InheritanceOddsTable;

        if (affinityTable == null) affinityTable = BreedingAffinityTableSO.Current;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Breeding services (requested by pens) ─────────────────────

    public float GetAffinity(Personality a, Personality b) =>
        (affinityTable ?? BreedingAffinityTableSO.Current)?.GetAffinity(a, b) ?? 0.5f;

    // Async server-side breeding: a pen requests these instead of owning the service.
    public Task StartBreedingAsync(string motherID, string fatherID) =>
        asyncBreedingService != null
            ? asyncBreedingService.StartBreedingAsync(motherID, fatherID)
            : Fail("AsyncBreedingService not assigned on BreedingController.");

    public Task HatchAsync(string motherID, string fatherID) =>
        asyncBreedingService != null
            ? asyncBreedingService.HatchAsync(motherID, fatherID)
            : Fail("AsyncBreedingService not assigned on BreedingController.");

    public Task CancelBreedingAsync(string motherID, string fatherID) =>
        asyncBreedingService != null
            ? asyncBreedingService.CancelBreedingAsync(motherID, fatherID)
            : Fail("AsyncBreedingService not assigned on BreedingController.");

    public Task CancelAllBreedingAsync() =>
        asyncBreedingService != null
            ? asyncBreedingService.CancelAllBreedingAsync()
            : Fail("AsyncBreedingService not assigned on BreedingController.");

    private static Task Fail(string message)
    {
        Debug.LogError($"[BreedingController] {message}");
        return Task.CompletedTask;
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

        var mother = females[UnityEngine.Random.Range(0, females.Count)];
        var father = males[UnityEngine.Random.Range(0, males.Count)];
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

    // ══════════════════════════════════════════════════════════════
    // ASYNC BREEDING (timer) — Etapa 2
    // ══════════════════════════════════════════════════════════════

    [BoxGroup("Breed Timer")]
    [InfoBox("Varias parejas pueden incubar en paralelo (una pareja = un huevo). El timer es server-side (30 min) y los huevos incuban aunque cierres el juego. 'Show Eggs' lista los huevos con índice; pon el índice en 'Hatch Index' y presiona 'Hatch Egg'.")]
    [ShowInInspector, ReadOnly, LabelText("Eggs"), BoxGroup("Breed Timer")]
    private string eggStatus = "No eggs";

    [BoxGroup("Breed Timer"), SerializeField, LabelText("Hatch Index")]
    private int hatchIndex = 0;

    private bool isHatching = false;

    [Button("Breed Timer", ButtonSizes.Large), GUIColor(0.7f, 0.55f, 1f), BoxGroup("Breed Timer")]
    private void BreedTimerButton()
    {
        if (asyncBreedingService == null) { Debug.LogError("[BreedingController] AsyncBreedingService not assigned."); return; }
        if (string.IsNullOrEmpty(breedMotherID) || string.IsNullOrEmpty(breedFatherID))
        {
            Debug.LogWarning("[BreedingController] Select a Mother and Father first (use Fill Random Breeders).");
            return;
        }
        _ = asyncBreedingService.StartBreedingAsync(breedMotherID, breedFatherID);
    }

    [Button("Hatch Egg", ButtonSizes.Large), GUIColor(1f, 0.85f, 0.4f), BoxGroup("Breed Timer")]
    private async void HatchButton()
    {
        if (isHatching) { Debug.Log("[BreedingController] A hatch is already in progress."); return; }
        if (asyncBreedingService == null) { Debug.LogError("[BreedingController] AsyncBreedingService not assigned."); return; }

        var eggs = GetEggs();
        if (eggs.Count == 0) { Debug.LogWarning("[BreedingController] No eggs incubating."); RefreshEggStatus(); return; }
        if (hatchIndex < 0 || hatchIndex >= eggs.Count)
        {
            Debug.LogError($"[BreedingController] Hatch index {hatchIndex} out of range — {eggs.Count} egg(s) (0–{eggs.Count - 1}). Press 'Show Eggs'.");
            return;
        }

        var mother = eggs[hatchIndex];
        isHatching = true;
        try
        {
            await asyncBreedingService.HatchAsync(mother.UniqueID, mother.BreedPartnerID);
        }
        finally
        {
            isHatching = false;
        }
        RefreshEggStatus();
    }

    [Button("Cancel All Eggs", ButtonSizes.Large), GUIColor(1f, 0.5f, 0.45f), BoxGroup("Breed Timer")]
    private async void CancelAllEggsButton()
    {
        if (asyncBreedingService == null) { Debug.LogError("[BreedingController] AsyncBreedingService not assigned."); return; }
        await asyncBreedingService.CancelAllBreedingAsync();
        RefreshEggStatus();
    }

    [Button("Show Eggs"), GUIColor(0.6f, 0.85f, 1f), BoxGroup("Breed Timer")]
    private void RefreshEggStatus()
    {
        var eggs = GetEggs();
        if (eggs.Count == 0) { eggStatus = "No eggs"; Debug.Log("[BreedingController] No eggs incubating."); return; }

        long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var  lines = new System.Collections.Generic.List<string>();
        for (int i = 0; i < eggs.Count; i++)
        {
            var mother  = eggs[i];
            var fatherName = registry.TryGet(mother.BreedPartnerID, out var father) ? father.CustomName : "???";
            string when = nowMs >= mother.BreedReadyAt
                ? "READY (local) — Hatch to confirm"
                : $"{TimeSpan.FromMilliseconds(mother.BreedReadyAt - nowMs):mm\\:ss} left";
            lines.Add($"[{i}] \"{mother.CustomName}\" x \"{fatherName}\" — {when}");
        }
        eggStatus = string.Join("   |   ", lines);
        Debug.Log($"[BreedingController] {eggs.Count} egg(s) incubating:\n  " + string.Join("\n  ", lines));
    }

    // An egg = a Breeding female + her BreedPartnerID. Enumerating females gives
    // one entry per egg (mothers are always Female). Stable order via UniqueID.
    private System.Collections.Generic.List<CreatureDNA> GetEggs() =>
        registry.GetAll().Values
            .Where(d => d.BusyState == BusyReason.Breeding && d.Gender == CreatureGender.Female && d.BreedReadyAt > 0)
            .OrderBy(d => d.UniqueID)
            .ToList();

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

        GameEvents.BreedingCompleted(mother, father, child);
        GameEvents.RegistryChanged(registry);
        lastChildID = child.UniqueID;
        RefreshBreedInfo();
        Debug.Log($"[BreedingController] Bred child: \"{child.CustomName}\"  {child.UniqueID}  ({child.Gender})");
    }
}
