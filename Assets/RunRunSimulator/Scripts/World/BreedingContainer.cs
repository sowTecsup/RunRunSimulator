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

    // Fixed breed points: one slot per pair the pen allows. Each slot = two child anchors the pair
    // stands on, facing each other. Configured in the inspector (drag child empties) — no runtime spacing.
    [System.Serializable]
    private struct BreedingSlot
    {
        public Transform spotA;
        public Transform spotB;
    }

    [BoxGroup("Breeding")]
    [Tooltip("Puntos fijos de cría: un slot por pareja. Cada slot = dos anclas hijas (spotA/spotB) donde se paran los dos MoriMonchis mirándose.")]
    [SerializeField] private BreedingSlot[] breedingSlots;

    [BoxGroup("Breeding")]
    [SerializeField] private UnityEvent onPairFormed;

    [BoxGroup("Breeding")]
    [ShowInInspector, ReadOnly, LabelText("Último Roll")]
    private string lastRollInfo = "---";

    // Live per-occupant eligibility readout — at a glance: why each one can/can't pair right now.
    [BoxGroup("Breeding")]
    [ShowInInspector, ReadOnly, LabelText("Diagnóstico Pareja")]
    private string PairDiagnostics => Application.isPlaying ? BuildDiagnostics() : "(solo en Play)";

    [BoxGroup("Breeding")]
    [Tooltip("Altura sobre el centro del corral desde donde se lanzan las crías recién nacidas.")]
    [SerializeField, Min(0f)] private float launchHeight = 1.5f;

    private float rollTimer;
    private readonly Dictionary<string, float> cooldowns = new Dictionary<string, float>();

    private string penKey;
    private static readonly Dictionary<string, BreedingContainer> byKey = new Dictionary<string, BreedingContainer>();

    // El corral más reciente lanza a sus crías desde aquí; los recién nacidos vuelan al centro + esta altura.
    public Vector3 LaunchPoint => Center + Vector3.up * launchHeight;

    public static bool TryGet(string key, out BreedingContainer pen)
    {
        if (string.IsNullOrEmpty(key)) { pen = null; return false; }
        return byKey.TryGetValue(key, out pen);
    }

    // All active pens in the scene — the BreedingController (manager) reads this to know how many
    // pens exist and which pairs are breeding where, without holding its own references.
    public static IReadOnlyCollection<BreedingContainer> All => byKey.Values;

    public string PenKey => penKey;

    // The active breeding pairs in this pen and the slot each occupies — "qué MM ↔ qué MM, en qué slot".
    public IEnumerable<(string mother, string father, int slot)> ActivePairs()
    {
        for (int i = 0; i < Occupants.Count; i++)
        {
            var d = Occupants[i]?.DNA;
            if (d == null || d.Gender != CreatureGender.Female || d.BusyState != BusyReason.Breeding) continue;
            string fatherName = GameManager.Instance != null && GameManager.Instance.Registry != null &&
                                GameManager.Instance.Registry.TryGet(d.BreedPartnerID, out var f) ? f.CustomName : "???";
            yield return (d.CustomName, fatherName, d.HomePenSlot);
        }
    }

    private void Start()
    {
        // El marker lo estampa FurnitureSpawner en la raíz tras el Instantiate, así que recién está disponible en Start.
        var marker = GetComponentInParent<PlacedFurnitureMarker>();
        if (marker != null)
        {
            penKey = $"{marker.AnchorCell.x}_{marker.AnchorCell.y}";
        }
        else
        {
            penKey = name;
            Debug.LogWarning($"[BreedingContainer] El corral \"{name}\" no tiene PlacedFurnitureMarker; usando el nombre como clave.");
        }
        byKey[penKey] = this;

        StartCoroutine(ReclaimBreedingOccupants());
    }

    private void OnDestroy()
    {
        if (!string.IsNullOrEmpty(penKey) && byKey.TryGetValue(penKey, out var pen) && ReferenceEquals(pen, this))
            byKey.Remove(penKey);
    }

    private void OnEnable()  => GameEvents.OnBreedingCompleted += OnBreedingCompleted;
    private void OnDisable() => GameEvents.OnBreedingCompleted -= OnBreedingCompleted;

    // Una pareja de ESTE corral acaba de tener cría: pedimos al spawner que la lance desde aquí
    // (el corral es quien hace la solicitud) y mandamos a ambos padres de vuelta a deambular dentro
    // del corral (estaban posados en cortejo, pero ya no se están apareando).
    private void OnBreedingCompleted(CreatureDNA mother, CreatureDNA father, CreatureDNA child)
    {
        var motherAgent = FindOccupant(mother);
        var fatherAgent = FindOccupant(father);
        if (motherAgent == null && fatherAgent == null) return;   // no nació en este corral

        if (child != null) MoriMochiSpawner.Instance?.RegisterBirthLaunch(child.UniqueID, LaunchPoint);

        if (motherAgent != null) motherAgent.ExitCourtship();
        if (fatherAgent != null) fatherAgent.ExitCourtship();
    }

    private MoriMochiAgent FindOccupant(CreatureDNA target)
    {
        if (target == null) return null;
        for (int i = 0; i < Occupants.Count; i++)
            if (Occupants[i] != null && ReferenceEquals(Occupants[i].DNA, target)) return Occupants[i];
        return null;
    }

    // El spawner de crías coloca al recién nacido directamente en este corral.
    public bool ReclaimDirect(MoriMochiAgent agent) => Claim(agent);

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

    // Poses each active breeding pair at its assigned fixed slot (female→spotA, male→spotB, facing each
    // other). Exits courtship for any occupant not currently part of a posed pair.
    private void ManageCourtship()
    {
        var posed = new HashSet<MoriMochiAgent>();

        var breeding = new List<MoriMochiAgent>();
        for (int i = 0; i < Occupants.Count; i++)
        {
            var a = Occupants[i];
            if (a != null && a.DNA != null && a.DNA.BusyState == BusyReason.Breeding) breeding.Add(a);
        }

        for (int i = 0; i < breeding.Count; i++)
        {
            var a = breeding[i];
            if (posed.Contains(a) || string.IsNullOrEmpty(a.DNA.BreedPartnerID)) continue;

            MoriMochiAgent partner = null;
            for (int j = i + 1; j < breeding.Count; j++)
            {
                var b = breeding[j];
                if (a.DNA.BreedPartnerID == b.DNA.UniqueID && b.DNA.BreedPartnerID == a.DNA.UniqueID)
                {
                    partner = b;
                    break;
                }
            }
            if (partner == null) continue;

            var female = a.DNA.Gender == CreatureGender.Female ? a : partner;
            var male   = ReferenceEquals(female, a) ? partner : a;

            int idx = female.DNA.HomePenSlot;
            if (breedingSlots == null || idx < 0 || idx >= breedingSlots.Length) continue;

            var slot = breedingSlots[idx];
            if (slot.spotA == null || slot.spotB == null) continue;

            if (!female.IsCourting) female.EnterCourtship(slot.spotA.position, slot.spotB.position);
            if (!male.IsCourting)   male.EnterCourtship(slot.spotB.position, slot.spotA.position);

            posed.Add(female);
            posed.Add(male);
        }

        for (int i = 0; i < Occupants.Count; i++)
        {
            var a = Occupants[i];
            if (a == null || posed.Contains(a)) continue;
            if (a.IsCourting) a.ExitCourtship();
        }
    }

    // First breed slot not already claimed by another breeding pair in this pen. -1 if none free.
    private int FindFreeSlot()
    {
        if (breedingSlots == null || breedingSlots.Length == 0) return -1;

        var used = new HashSet<int>();
        for (int i = 0; i < Occupants.Count; i++)
        {
            var d = Occupants[i]?.DNA;
            if (d != null && d.BusyState == BusyReason.Breeding && d.HomePenSlot >= 0) used.Add(d.HomePenSlot);
        }

        for (int i = 0; i < breedingSlots.Length; i++)
            if (!used.Contains(i)) return i;
        return -1;
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
            int slot = FindFreeSlot();
            motherDNA.HomePenKey = penKey;
            fatherDNA.HomePenKey = penKey;
            motherDNA.HomePenSlot = slot;
            fatherDNA.HomePenSlot = slot;
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
        d.HomePenKey     = "";
        d.HomePenSlot    = -1;
    }

    // ── Persistence: re-pen breeders after a scene reload ─────────

    // On (re)load the cannon drops every creature in the open, not inside the pen. We query the
    // registry for creatures still mid-breed (BusyState==Breeding) and, once their agents spawn and
    // settle, teleport them back in (up to capacity) so a paired couple keeps incubating in the pen.
    private IEnumerator ReclaimBreedingOccupants()
    {
        while (GameManager.Instance == null || GameManager.Instance.Registry == null) yield return null;

        var targets = new HashSet<string>(GameManager.Instance.Registry.GetAll().Values
            .Where(d => d.BusyState == BusyReason.Breeding && d.HomePenKey == penKey)
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
