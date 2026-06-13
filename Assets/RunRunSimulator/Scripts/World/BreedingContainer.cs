using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

// A breeding pen. Passively restores its occupants' needs, periodically rolls a dice
// (affinity × diceChance) to pair an available Male + Female, and on success kicks off
// SERVER-SIDE async breeding (the egg incubates on the server timer). Tap E on the pen to
// hatch a ready egg. It owns no breeding services itself — it asks BreedingController.Instance
// for the affinity table + async breeding, so there's a single source of truth in the scene.
public class BreedingContainer : MoriMochiContainer, IInteractable
{
    [BoxGroup("Breeding")]
    [SerializeField, Range(0f, 1f)] private float diceChance = 0.5f;

    [BoxGroup("Breeding")]
    [SerializeField, Min(1f)] private float rollInterval = 10f;

    [BoxGroup("Breeding")]
    [SerializeField, Min(0f)] private float pairCooldown = 60f;

    [BoxGroup("Breeding")]
    [Tooltip("Needs (health/energy/affect) restored per second to every penned occupant.")]
    [SerializeField, Min(0f)] private float restoreRate = 5f;

    [BoxGroup("Breeding")]
    [Tooltip("On scene load, breeders mid-incubation are pulled back into the pen once the cannon spawns them. Stop trying after this many seconds.")]
    [SerializeField, Min(0f)] private float reclaimTimeout = 20f;

    [BoxGroup("Breeding")]
    [Tooltip("Distance between the two MoriMonchis while they court (incubating), in meters.")]
    [SerializeField, Min(0f)] private float courtGap = 0.6f;

    [BoxGroup("Breeding")]
    [SerializeField] private UnityEvent onPairFormed;

    [BoxGroup("Breeding")]
    [ShowInInspector, ReadOnly, LabelText("Último Roll")]
    private string lastRollInfo = "---";

    // Live per-occupant eligibility readout — at a glance: why each one can/can't pair right now.
    [BoxGroup("Breeding")]
    [ShowInInspector, ReadOnly, LabelText("Diagnóstico Pareja")]
    private string PairDiagnostics => Application.isPlaying ? BuildDiagnostics() : "(solo en Play)";

    private float rollTimer;
    private readonly Dictionary<string, float> cooldowns = new Dictionary<string, float>();

    private void Start() => StartCoroutine(ReclaimBreedingOccupants());

    private void Update()
    {
        PassiveRestore(Time.deltaTime);
        ManageCourtship();

        rollTimer += Time.deltaTime;
        if (rollTimer < rollInterval) return;
        rollTimer = 0f;
        TryRollPair(false, false);
    }

    // Penned MoriMonchis recover passively while the pen "cares for" them — mirrors StoreContainer.
    private void PassiveRestore(float dt)
    {
        if (restoreRate <= 0f) return;
        float delta = restoreRate * dt;
        for (int i = 0; i < Occupants.Count; i++)
        {
            var dna = Occupants[i]?.DNA;
            if (dna == null || dna.IsDead) continue;
            dna.Needs.AddHealth(delta);
            dna.Needs.AddEnergy(delta);
            dna.Needs.AddAffect(delta);
        }
    }

    // Poses the incubating pair face-to-face at courtGap distance; exits courtship for any
    // other occupant that is still in the Courting state but is no longer part of an active pair.
    private void ManageCourtship()
    {
        MoriMochiAgent agentA = null;
        MoriMochiAgent agentB = null;

        var breeding = new List<MoriMochiAgent>();
        for (int i = 0; i < Occupants.Count; i++)
        {
            var a = Occupants[i];
            if (a == null || a.DNA == null) continue;
            if (a.DNA.BusyState == BusyReason.Breeding) breeding.Add(a);
        }

        for (int i = 0; i < breeding.Count && agentA == null; i++)
        {
            var candidate = breeding[i];
            if (string.IsNullOrEmpty(candidate.DNA.BreedPartnerID)) continue;
            for (int j = i + 1; j < breeding.Count; j++)
            {
                var other = breeding[j];
                if (candidate.DNA.BreedPartnerID == other.DNA.UniqueID &&
                    other.DNA.BreedPartnerID == candidate.DNA.UniqueID)
                {
                    agentA = candidate;
                    agentB = other;
                    break;
                }
            }
        }

        if (agentA != null && agentB != null)
        {
            Vector3 center = Center;
            Vector3 axis   = transform.right; axis.y = 0f;
            if (axis.sqrMagnitude < 0.001f) axis = Vector3.right;
            axis = axis.normalized;
            Vector3 slotA = center + axis * (courtGap * 0.5f);
            Vector3 slotB = center - axis * (courtGap * 0.5f);

            if (!agentA.IsCourting) agentA.EnterCourtship(slotA, slotB);
            if (!agentB.IsCourting) agentB.EnterCourtship(slotB, slotA);
        }

        for (int i = 0; i < Occupants.Count; i++)
        {
            var a = Occupants[i];
            if (a == null) continue;
            if (ReferenceEquals(a, agentA) || ReferenceEquals(a, agentB)) continue;
            if (a.IsCourting) a.ExitCourtship();
        }
    }

    [Button("Forzar Roll de Pareja", ButtonSizes.Large), GUIColor(1f, 0.7f, 0.85f), BoxGroup("Breeding")]
    private void ForceRoll()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[BreedingContainer] Entra en Play primero."); return; }
        rollTimer = 0f;
        TryRollPair(true, true);
    }

    // One pairing attempt. verbose → always reports the math (affinity × dice, the roll, outcome) to
    // lastRollInfo + console; the periodic auto-roll stays quiet unless it actually pairs.
    // ignoreCooldown → ForceRoll bypasses the per-creature throttle so a forced roll is never blocked
    // solely by cooldown; the auto-roll always respects it.
    private async void TryRollPair(bool verbose, bool ignoreCooldown)
    {
        if (Occupants.Count < 2) { Report(verbose, "Hacen falta al menos 2 MoriMonchis en el corral."); return; }

        var controller = BreedingController.Instance;
        if (controller == null) { Report(verbose, "No hay BreedingController en escena."); return; }

        PruneCooldowns();

        var females = AvailableOf(CreatureGender.Female, ignoreCooldown);
        var males   = AvailableOf(CreatureGender.Male,   ignoreCooldown);
        if (females.Count == 0 || males.Count == 0)
        {
            Report(verbose, $"Sin pareja válida (hembras disponibles: {females.Count}, machos: {males.Count}). {BuildDiagnostics()}");
            return;
        }

        var motherDNA = females[UnityEngine.Random.Range(0, females.Count)];
        var fatherDNA = males[UnityEngine.Random.Range(0, males.Count)];

        float affinity = controller.GetAffinity(motherDNA.Personality, fatherDNA.Personality);
        float chance   = affinity * diceChance;
        float roll     = UnityEngine.Random.value;

        string pair = $"\"{motherDNA.CustomName}\" ({motherDNA.Personality}) × \"{fatherDNA.CustomName}\" ({fatherDNA.Personality})";
        string math = $"afinidad {affinity:P0} × dado {diceChance:P0} = {chance:P0} | salió {roll:P0}";

        if (roll >= chance)
        {
            Report(verbose, $"Intento: {pair} — {math} → no suficiente.");
            return;
        }

        onPairFormed?.Invoke();

        await controller.StartBreedingAsync(motherDNA.UniqueID, fatherDNA.UniqueID);

        // StartBreedingAsync only marks the parents Breeding if the SERVER accepted the egg.
        // If it didn't (the server still holds a prior egg for one of them → already_breeding),
        // don't lie about it and don't burn a cooldown on a pairing that never happened.
        if (motherDNA.BusyState == BusyReason.Breeding && fatherDNA.BusyState == BusyReason.Breeding)
        {
            cooldowns[motherDNA.UniqueID] = Time.time + pairCooldown;
            cooldowns[fatherDNA.UniqueID] = Time.time + pairCooldown;
            Report(true, $"¡Emparejados! {pair} — {math} → incubando.");
        }
        else
        {
            Report(true, $"Dado OK ({pair} — {math}) pero el servidor NO inició la incubación. " +
                         "Probable huevo previo sin eclosionar/cancelar en el servidor (¿cancel-breeding desplegado?).");
        }
    }

    private List<CreatureDNA> AvailableOf(CreatureGender gender, bool ignoreCooldown) => Occupants
        .Select(a => a?.DNA)
        .Where(d => d != null
            && !d.IsDead
            && !d.IsBusy
            && d.Gender == gender
            && d.BreedCount < BreedingService.MaxBreedCount
            && (ignoreCooldown || !cooldowns.ContainsKey(d.UniqueID)))
        .ToList();

    // ── Hatch (IInteractable: tap E on the pen) ───────────────────

    // Tap E while looking at the pen → hatch the egg incubating here once its server timer is up.
    public void Interact()
    {
        var controller = BreedingController.Instance;
        if (controller == null) { Debug.LogWarning("[BreedingContainer] No hay BreedingController en escena."); return; }

        var mother = Occupants
            .Select(a => a?.DNA)
            .FirstOrDefault(d => d != null
                && d.BusyState == BusyReason.Breeding
                && d.Gender == CreatureGender.Female
                && d.BreedReadyAt > 0);

        if (mother == null)
        {
            lastRollInfo = "No hay parejas incubando aquí.";
            Debug.Log("[BreedingContainer] No hay huevos incubando en este corral.");
            return;
        }

        long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (nowMs < mother.BreedReadyAt)
        {
            var left = TimeSpan.FromMilliseconds(mother.BreedReadyAt - nowMs);
            lastRollInfo = $"Huevo de \"{mother.CustomName}\" aún no listo — faltan {left:mm\\:ss}.";
            Debug.Log($"[BreedingContainer] {lastRollInfo}");
            return;
        }

        lastRollInfo = $"Eclosionando huevo de \"{mother.CustomName}\"...";
        _ = controller.HatchAsync(mother.UniqueID, mother.BreedPartnerID);
    }

    // ── Removal cancels the pairing ───────────────────────────────

    // Taking a creature out of the pen cancels any in-progress pairing (the pen is where breeding
    // happens): both parents revert to a normal, non-breeding state so their tags drop the heart/timer.
    public override void Release(MoriMochiAgent agent)
    {
        base.Release(agent);
        CancelBreeding(agent?.DNA);
    }

    private void CancelBreeding(CreatureDNA dna)
    {
        if (dna == null || dna.BusyState != BusyReason.Breeding) return;

        string partnerId = dna.BreedPartnerID;

        string motherId = dna.Gender == CreatureGender.Female ? dna.UniqueID : partnerId;
        string fatherId = dna.Gender == CreatureGender.Female ? partnerId   : dna.UniqueID;

        var registry = GameManager.Instance?.Registry;

        ClearBreed(dna);
        if (registry != null && !string.IsNullOrEmpty(partnerId) && registry.TryGet(partnerId, out var partner))
            ClearBreed(partner);

        _ = BreedingController.Instance?.CancelBreedingAsync(motherId, fatherId);

        Debug.Log($"[BreedingContainer] Emparejamiento cancelado al retirar \"{dna.CustomName}\" del corral.");
    }

    private static void ClearBreed(CreatureDNA d)
    {
        d.BusyState      = BusyReason.None;
        d.BreedReadyAt   = 0;
        d.BreedPartnerID = "";
    }

    // ── Persistence: re-pen breeders after a scene reload ─────────

    // On (re)load the cannon drops every creature in the open, not inside the pen. We query the
    // registry for creatures still mid-breed (BusyState==Breeding) and, once their agents spawn and
    // settle, teleport them back in (up to capacity) so a paired couple keeps incubating in the pen.
    private IEnumerator ReclaimBreedingOccupants()
    {
        while (GameManager.Instance == null || GameManager.Instance.Registry == null) yield return null;

        var targets = new HashSet<string>(GameManager.Instance.Registry.GetAll().Values
            .Where(d => d.BusyState == BusyReason.Breeding)
            .Select(d => d.UniqueID));
        if (targets.Count == 0) yield break;

        float deadline = Time.time + reclaimTimeout;
        var   wait     = new WaitForSeconds(0.5f);

        while (Time.time < deadline && !IsFull)
        {
            foreach (var a in FindObjectsByType<MoriMochiAgent>(FindObjectsSortMode.None))
            {
                if (IsFull) break;
                if (a == null || a.DNA == null || a.IsPenned) continue;
                if (a.IsHeld || a.IsAirborne) continue;            // wait until it lands and settles
                if (!targets.Contains(a.DNA.UniqueID)) continue;
                if (Claim(a)) Debug.Log($"[BreedingContainer] Recuperado al corral: \"{a.DNA.CustomName}\".");
            }
            yield return wait;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────

    // Per-occupant eligibility, for the inspector + the "sin pareja válida" message.
    private string BuildDiagnostics()
    {
        if (Occupants.Count == 0) return "Corral vacío.";
        float now = Time.time;
        var lines = Occupants
            .Select(a => a?.DNA)
            .Where(d => d != null)
            .Select(d =>
            {
                string why =
                    d.IsDead                                      ? "muerto" :
                    d.BusyState == BusyReason.Breeding            ? "incubando" :
                    d.IsBusy                                      ? d.BusyState.ToString().ToLower() :
                    d.BreedCount >= BreedingService.MaxBreedCount ? "máx. crías" :
                    cooldowns.ContainsKey(d.UniqueID)             ? $"cooldown {Mathf.CeilToInt(cooldowns[d.UniqueID] - now)}s" :
                                                                    "DISPONIBLE";
                return $"\"{d.CustomName}\" ({d.Gender}, {d.Personality}): {why}";
            });
        return "Detalle → " + string.Join(" | ", lines);
    }

    private void Report(bool log, string message)
    {
        lastRollInfo = message;
        if (log) Debug.Log($"[BreedingContainer] {message}");
    }

    private void PruneCooldowns()
    {
        float now = Time.time;
        var expired = cooldowns.Where(kv => kv.Value <= now).Select(kv => kv.Key).ToList();
        foreach (var key in expired)
            cooldowns.Remove(key);
    }
}
