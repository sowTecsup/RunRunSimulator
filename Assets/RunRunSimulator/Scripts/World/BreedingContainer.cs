using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class BreedingContainer : MoriMochiContainer
{
    [BoxGroup("Breeding")]
    [SerializeField, Range(0f, 1f)] private float diceChance = 0.5f;

    [BoxGroup("Breeding")]
    [SerializeField, Min(1f)] private float rollInterval = 10f;

    [BoxGroup("Breeding")]
    [SerializeField, Min(0f)] private float pairCooldown = 60f;

    [BoxGroup("Breeding")]
    [SerializeField] private bool useAsyncBreed = false;

    [BoxGroup("Breeding")]
    [SerializeField] private AsyncBreedingService asyncBreedingService;

    [BoxGroup("Breeding")]
    [SerializeField] private BreedingAffinityTableSO affinityTable;

    [BoxGroup("Breeding")]
    [SerializeField] private UnityEvent onPairFormed;

    private CreatureRegistrySO     registry;
    private CreatureDatabaseSO     database;
    private InheritanceOddsTableSO inheritanceOdds;

    private float rollTimer;
    private readonly Dictionary<string, float> cooldowns = new Dictionary<string, float>();

    protected override void Awake()
    {
        base.Awake();

        var gm       = GameManager.Instance;
        registry        = gm.Registry;
        database        = gm.Database;
        inheritanceOdds = gm.InheritanceOddsTable;

        if (affinityTable == null)
            affinityTable = BreedingAffinityTableSO.Current;
    }

    private void Update()
    {
        rollTimer += Time.deltaTime;
        if (rollTimer < rollInterval) return;
        rollTimer = 0f;
        TryRollPair();
    }

    private void TryRollPair()
    {
        if (Occupants.Count < 2) return;

        PruneCooldowns();

        var now = Time.time;

        var females = Occupants
            .Where(a => a.DNA != null
                && !a.DNA.IsDead
                && !a.DNA.IsBusy
                && a.DNA.Gender == CreatureGender.Female
                && a.DNA.BreedCount < BreedingService.MaxBreedCount
                && !cooldowns.ContainsKey(a.DNA.UniqueID))
            .ToList();

        var males = Occupants
            .Where(a => a.DNA != null
                && !a.DNA.IsDead
                && !a.DNA.IsBusy
                && a.DNA.Gender == CreatureGender.Male
                && a.DNA.BreedCount < BreedingService.MaxBreedCount
                && !cooldowns.ContainsKey(a.DNA.UniqueID))
            .ToList();

        if (females.Count == 0 || males.Count == 0) return;

        var motherAgent = females[Random.Range(0, females.Count)];
        var fatherAgent = males[Random.Range(0, males.Count)];
        var motherDNA   = motherAgent.DNA;
        var fatherDNA   = fatherAgent.DNA;

        var table  = affinityTable ?? BreedingAffinityTableSO.Current;
        if (table == null)
        {
            Debug.LogError("[BreedingContainer] No BreedingAffinityTable assigned or registered as Current.");
            return;
        }

        float affinity = table.GetAffinity(motherDNA.Personality, fatherDNA.Personality);
        float chance   = affinity * diceChance;

        if (Random.value >= chance) return;

        cooldowns[motherDNA.UniqueID] = now + pairCooldown;
        cooldowns[fatherDNA.UniqueID] = now + pairCooldown;

        onPairFormed?.Invoke();

        if (useAsyncBreed)
        {
            if (asyncBreedingService == null)
            {
                Debug.LogError("[BreedingContainer] useAsyncBreed is true but AsyncBreedingService is not assigned.");
                return;
            }
            _ = asyncBreedingService.StartBreedingAsync(motherDNA.UniqueID, fatherDNA.UniqueID);
        }
        else
        {
            BreedLocally(motherDNA.UniqueID, fatherDNA.UniqueID);
        }
    }

    private void BreedLocally(string motherID, string fatherID)
    {
        var odds = inheritanceOdds ?? InheritanceOddsTableSO.Current;
        if (odds == null)
        {
            Debug.LogError("[BreedingContainer] No InheritanceOddsTable assigned.");
            return;
        }

        var child = BreedingService.Breed(motherID, fatherID, registry, database, odds);
        if (child == null) return;

        child.CustomName = CreatureNameBank.GetRandomName();
        child.Stamp();
        if (!registry.Register(child)) return;

        if (registry.TryGet(motherID, out var mother)) mother.ChildrenIDs.Add(child.UniqueID);
        if (registry.TryGet(fatherID, out var father)) father.ChildrenIDs.Add(child.UniqueID);

        GameEvents.BreedingCompleted(mother, father, child);
        GameEvents.RegistryChanged(registry);

        Debug.Log($"[BreedingContainer] Bred child: \"{child.CustomName}\"  {child.UniqueID}  ({child.Gender})");
    }

    private void PruneCooldowns()
    {
        float now = Time.time;
        var expired = cooldowns.Where(kv => kv.Value <= now).Select(kv => kv.Key).ToList();
        foreach (var key in expired)
            cooldowns.Remove(key);
    }
}
