using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class BreedingDevConsole : MonoBehaviour
{
    // ── Setup ─────────────────────────────────────────────────────

    [BoxGroup("Setup"), Required]
    [SerializeField] private GameManager gameManager;

    [BoxGroup("Setup"), Required]
    [SerializeField] private BreedingController breedingController;

    // ── Corrales ──────────────────────────────────────────────────

    [ShowInInspector, ReadOnly, LabelText("Corrales activos"), BoxGroup("Corrales")]
    private int ActivePenCount => Application.isPlaying ? BreedingContainer.All.Count : 0;

    [ShowInInspector, ReadOnly, LabelText("Parejas activas"), BoxGroup("Corrales")]
    private string ActivePairsInfo
    {
        get
        {
            if (!Application.isPlaying) return "(solo en Play)";
            var lines = new List<string>();
            foreach (var pen in BreedingContainer.All)
                foreach (var (mother, father, slot) in pen.ActivePairs())
                    lines.Add($"[{pen.AnchorKey} #{slot}] \"{mother}\" × \"{father}\"");
            return lines.Count == 0 ? "Sin parejas activas." : string.Join("  |  ", lines);
        }
    }

    // ── Breed ─────────────────────────────────────────────────────

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

    [Button("Fill Random Breeders"), GUIColor(0.85f, 0.6f, 1f), BoxGroup("Breed")]
    private void FillRandomBreeders()
    {
        if (gameManager == null) { Debug.LogError("[BreedingDevConsole] GameManager not assigned."); return; }
        var registry = gameManager.Registry;
        var all     = registry.GetAll().Values.ToList();
        var females = all.Where(d => !d.IsDead && !d.IsBusy && d.Gender == CreatureGender.Female && d.BreedCount < BreedingService.MaxBreedCount).ToList();
        var males   = all.Where(d => !d.IsDead && !d.IsBusy && d.Gender == CreatureGender.Male   && d.BreedCount < BreedingService.MaxBreedCount).ToList();

        if (females.Count == 0 || males.Count == 0)
        {
            Debug.LogError("[BreedingDevConsole] Not enough valid breeders — need at least one alive Male and one alive Female under the breed limit.");
            return;
        }

        var mother = females[UnityEngine.Random.Range(0, females.Count)];
        var father = males[UnityEngine.Random.Range(0, males.Count)];
        breedMotherID = mother.UniqueID;
        breedFatherID = father.UniqueID;
        RefreshBreedInfo();
        Debug.Log($"[BreedingDevConsole] Random breeders — Mother: {Clip(mother.UniqueID)} | Father: {Clip(father.UniqueID)}");
    }

    [Button("Breed", ButtonSizes.Large), GUIColor(1f, 0.7f, 0.85f), BoxGroup("Breed")]
    private void BreedButton()
    {
        if (breedingController == null) { Debug.LogError("[BreedingDevConsole] BreedingController not assigned."); return; }
        var childID = breedingController.BreedCreatures(breedMotherID, breedFatherID);
        if (!string.IsNullOrEmpty(childID)) lastChildID = childID;
        RefreshBreedInfo();
    }

    private void RefreshBreedInfo()
    {
        motherBreedInfo = BuildBreedInfo(breedMotherID);
        fatherBreedInfo = BuildBreedInfo(breedFatherID);
    }

    private string BuildBreedInfo(string id)
    {
        if (gameManager == null) return "---";
        var registry = gameManager.Registry;
        if (string.IsNullOrEmpty(id) || !registry.TryGet(id, out var dna)) return "---";
        return dna.IsDead
            ? $"\"{dna.CustomName}\"  {dna.Gender} | DEAD"
            : $"\"{dna.CustomName}\"  {dna.Gender} | Breeds: {dna.BreedCount}/{BreedingService.MaxBreedCount}";
    }

    private static string Clip(string id) => id.Length > 14 ? id[..14] + "…" : id;

    // ── Breed Timer ───────────────────────────────────────────────

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
        if (breedingController == null) { Debug.LogError("[BreedingDevConsole] BreedingController not assigned."); return; }
        if (string.IsNullOrEmpty(breedMotherID) || string.IsNullOrEmpty(breedFatherID))
        {
            Debug.LogWarning("[BreedingDevConsole] Select a Mother and Father first (use Fill Random Breeders).");
            return;
        }
        _ = breedingController.StartBreedingAsync(breedMotherID, breedFatherID);
    }

    [Button("Hatch Egg", ButtonSizes.Large), GUIColor(1f, 0.85f, 0.4f), BoxGroup("Breed Timer")]
    private async void HatchButton()
    {
        if (isHatching) { Debug.Log("[BreedingDevConsole] A hatch is already in progress."); return; }
        if (breedingController == null) { Debug.LogError("[BreedingDevConsole] BreedingController not assigned."); return; }
        if (gameManager == null) { Debug.LogError("[BreedingDevConsole] GameManager not assigned."); return; }

        var eggs = GetEggs();
        if (eggs.Count == 0) { Debug.LogWarning("[BreedingDevConsole] No eggs incubating."); RefreshEggStatus(); return; }
        if (hatchIndex < 0 || hatchIndex >= eggs.Count)
        {
            Debug.LogError($"[BreedingDevConsole] Hatch index {hatchIndex} out of range — {eggs.Count} egg(s) (0–{eggs.Count - 1}). Press 'Show Eggs'.");
            return;
        }

        var mother = eggs[hatchIndex];
        isHatching = true;
        try
        {
            await breedingController.HatchAsync(mother.UniqueID, mother.BreedPartnerID);
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
        if (breedingController == null) { Debug.LogError("[BreedingDevConsole] BreedingController not assigned."); return; }
        await breedingController.CancelAllBreedingAsync();
        RefreshEggStatus();
    }

    [Button("Show Eggs"), GUIColor(0.6f, 0.85f, 1f), BoxGroup("Breed Timer")]
    private void RefreshEggStatus()
    {
        if (gameManager == null) { Debug.LogError("[BreedingDevConsole] GameManager not assigned."); return; }
        var registry = gameManager.Registry;
        var eggs = GetEggs();
        if (eggs.Count == 0) { eggStatus = "No eggs"; Debug.Log("[BreedingDevConsole] No eggs incubating."); return; }

        long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var  lines = new List<string>();
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
        Debug.Log($"[BreedingDevConsole] {eggs.Count} egg(s) incubating:\n  " + string.Join("\n  ", lines));
    }

    // An egg = a Breeding female + her BreedPartnerID. Enumerating females gives
    // one entry per egg (mothers are always Female). Stable order via UniqueID.
    private List<CreatureDNA> GetEggs()
    {
        if (gameManager == null) return new List<CreatureDNA>();
        var registry = gameManager.Registry;
        return registry.GetAll().Values
            .Where(d => d.BusyState == BusyReason.Breeding && d.Gender == CreatureGender.Female && d.BreedReadyAt > 0)
            .OrderBy(d => d.UniqueID)
            .ToList();
    }
}
}
