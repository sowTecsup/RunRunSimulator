using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
namespace MoriMonchiSimulator
{

public class ArenaSandbox : MonoBehaviour
{
    [Required, SerializeField] private MoriMonchiController creaturePrefab;
    [Required, SerializeField] private RoleWorldProfileSO profileTable;
    [Required, SerializeField] private SocialTuningSO socialTuning;
    [Required, SerializeField] private ExpeditionRulesSO expeditionRules;
    [Required, SerializeField] private ClashTuningSO clashTuning;
    [Required, SerializeField] private MonchiVisualBankSO visualBank;
    [Required, SerializeField] private FurTypeDatabaseSO furDatabase;
    [Required, SerializeField] private CreatureDatabaseSO creatureDatabase;

    [SerializeField] private Transform observer;
    [SerializeField] private Unity.Cinemachine.CinemachineTargetGroup targetGroup;
    [SerializeField] private Transform spawnCenter;

    [SerializeField] private int seed = 4242;
    [SerializeField] private int castSeed = 1;
    [SerializeField] private bool randomizeEachPlay;
    [SerializeField, Min(1)] private int count = 3;
    [SerializeField, Min(1f)] private float spawnRadius = 4f;
    [SerializeField] private bool keepNeedsFull = true;
    [SerializeField, Min(0f)] private float tagShowDistance = 60f;
    [SerializeField, Min(0f)] private float tagReferenceDistance = 14f;

    [Title("Elenco")]
    [SerializeField] private ArenaRosterSO roster;
    [SerializeField] private bool useRoster = true;
    [SerializeField, Min(0f)] private float teamSpawnInset = 9f;
    [SerializeField, Min(0.5f)] private float teamSpawnRadius = 2.5f;
    [Required, SerializeField] private ExitZone exitPrefab;
    [SerializeField, Min(0f)] private float exitInset = 4f;

    [Required, SerializeField] private MaterialPickup mineralPrefab;
    [SerializeField] private ArenaLayoutBuilder layout;
    [SerializeField, Min(0)] private int cornerMinerals = 4;
    [SerializeField, Min(0f)] private float cornerInset = 6f;
    [SerializeField, Min(0f)] private float cornerJitter = 2f;
    [SerializeField, Min(1f)] private float centerMineralScale = 2.5f;
    [SerializeField, Min(1)] private int centerMineralValue = 5;
    [SerializeField, Min(1)] private int cornerMineralValue = 1;
    [SerializeField, Min(0f)] private float arenaHalfSize = 20f;

    private readonly List<MoriMonchiController> spawned = new();
    private readonly List<MaterialPickup> minerals = new();
    private readonly List<ExitZone> exits = new();
    private int activeSeed;
    private Transform spawnHolder;

    public IReadOnlyList<MoriMonchiController> Spawned => spawned;
    public IReadOnlyList<MaterialPickup> Minerals => minerals;
    public IReadOnlyList<ExitZone> Exits => exits;
    public int ActiveSeed => activeSeed;

    public ExitZone ExitFor(ExpeditionTeam team)
    {
        foreach (var exit in exits)
            if (exit != null && exit.Team == team) return exit;
        return null;
    }

    private void Start()
    {
        Application.runInBackground = true;
        Spawn();
    }

    private void Update()
    {
        if (!keepNeedsFull) return;

        foreach (var controller in spawned)
        {
            if (controller == null || controller.DNA == null) continue;
            controller.DNA.Needs.Health = 100f;
            controller.DNA.Needs.Energy = 100f;
            controller.DNA.Needs.Affect = 100f;
        }
    }

    private void Spawn()
    {
        if (spawnHolder == null)
        {
            spawnHolder = new GameObject("SpawnHolder").transform;
            spawnHolder.SetParent(transform);
            spawnHolder.gameObject.SetActive(false);
        }

        activeSeed = randomizeEachPlay ? System.Environment.TickCount : seed;
        Random.InitState(castSeed);
        var rng = new System.Random(activeSeed);

        Vector3 center = spawnCenter != null ? spawnCenter.position : transform.position;

        int agentType = creaturePrefab.GetComponent<NavMeshAgent>().agentTypeID;
        var filter = new NavMeshQueryFilter { agentTypeID = agentType, areaMask = NavMesh.AllAreas };

        if (useRoster && roster != null && roster.Entries != null && roster.Entries.Count > 0)
        {
            SpawnExits(filter, center);
            if (layout != null) layout.Build(activeSeed, filter);
            SpawnMinerals(rng, filter, center);

            foreach (var entry in roster.Entries)
            {
                var dna = MintRandom();
                dna.Sociability = entry.Sociability;
                dna.Boldness = entry.Boldness;
                if (!string.IsNullOrEmpty(entry.Name)) dna.CustomName = entry.Name;
                if (!string.IsNullOrEmpty(entry.BodyShapeID)) dna.BodyShapeID = entry.BodyShapeID;
                if (entry.BaseColor.a > 0f) dna.BaseColor = entry.BaseColor;
                dna.Stamp();

                SpawnCreature(dna, TeamCorner(entry.Team, center), teamSpawnRadius, rng, filter, entry.Team, entry.Occupation, ExitFor(entry.Team));
            }
        }
        else
        {
            SpawnMinerals(rng, filter, center);

            for (int i = 0; i < count; i++)
            {
                var dna = MintRandom();
                SpawnCreature(dna, center, spawnRadius, rng, filter, ExpeditionTeam.None, Occupation.Gather, null);
            }
        }

        Debug.Log($"[ArenaSandbox] seed={activeSeed} castSeed={castSeed} spawned={spawned.Count} minerals={minerals.Count} exits={exits.Count} roster={roster != null && useRoster}");
    }

    private CreatureDNA MintRandom()
    {
        var dna = CreatureGenerator.GenerateRandom(creatureDatabase, furDatabase);
        dna.Gender = Random.value < 0.5f ? CreatureGender.Male : CreatureGender.Female;
        dna.Element = CreatureGenerator.RandomElement();
        dna.Role = CreatureGenerator.RandomRole();
        (dna.BaseConstitution, dna.BaseAttack, dna.BaseSpeed) = CreatureGenerator.RandomBaseStats();
        dna.Sociability = CreatureGenerator.RandomDial();
        dna.Boldness = CreatureGenerator.RandomDial();
        dna.CustomName = CreatureNameBank.GetRandomName();
        dna.Stamp();
        return dna;
    }

    private MoriMonchiController SpawnCreature(CreatureDNA dna, Vector3 around, float radius, System.Random rng, NavMeshQueryFilter filter, ExpeditionTeam team, Occupation occupation, ExitZone home)
    {
        float angle = (float)(rng.NextDouble() * Mathf.PI * 2f);
        float dist = (float)(rng.NextDouble() * radius);
        Vector3 point = around + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);

        Vector3 pos = NavMesh.SamplePosition(point, out var hit, radius, filter)
            ? hit.position
            : around;

        var controller = Instantiate(creaturePrefab, pos, Quaternion.Euler(0f, rng.Next(0, 360), 0f), spawnHolder);
        controller.GetComponent<NavMeshAgent>().areaMask = NavMesh.AllAreas;

        var perceivable = controller.GetComponentInChildren<Perceivable>(true);
        if (perceivable != null) perceivable.SetTeam(team);

        controller.transform.SetParent(transform, true);
        controller.Initialize(dna, profileTable, observer, visualBank, furDatabase);
        controller.Agent.SetOccupation(occupation);
        controller.Agent.SetHomeExit(home);
        controller.Agent.SetGuardPost(minerals.Count > 0 && minerals[0] != null ? minerals[0].transform : null);
        spawned.Add(controller);
        if (targetGroup != null) targetGroup.AddMember(controller.transform, 1f, 1.2f);
        foreach (var tag in controller.GetComponentsInChildren<NameTag>(true)) { tag.ShowDistance = tagShowDistance; tag.ScreenSizeReferenceDistance = tagReferenceDistance; }

        return controller;
    }

    private Vector3 TeamCorner(ExpeditionTeam team, Vector3 center)
    {
        switch (team)
        {
            case ExpeditionTeam.Player:
                return center + new Vector3(-1f, 0f, -1f) * (arenaHalfSize - teamSpawnInset);
            case ExpeditionTeam.Rival:
                return center + new Vector3(1f, 0f, 1f) * (arenaHalfSize - teamSpawnInset);
            default:
                return center;
        }
    }

    private void SpawnExits(NavMeshQueryFilter filter, Vector3 center)
    {
        SpawnExit(ExpeditionTeam.Player, new Vector3(-1f, 0f, -1f), center, filter);
        SpawnExit(ExpeditionTeam.Rival, new Vector3(1f, 0f, 1f), center, filter);
    }

    private void SpawnExit(ExpeditionTeam team, Vector3 dir, Vector3 center, NavMeshQueryFilter filter)
    {
        Vector3 point = center + dir * (arenaHalfSize - exitInset);
        Vector3 pos = NavMesh.SamplePosition(point, out var hit, 4f, filter)
            ? hit.position
            : point;

        var exit = Instantiate(exitPrefab, pos, Quaternion.identity, transform);
        exit.SetTeam(team);
        exits.Add(exit);
    }

    private void SpawnMinerals(System.Random rng, NavMeshQueryFilter filter, Vector3 center)
    {
        Vector3 centerPos = NavMesh.SamplePosition(center, out var centerHit, 2f, filter)
            ? centerHit.position
            : center;

        var centerMineral = Instantiate(mineralPrefab, centerPos, Quaternion.Euler(0f, rng.Next(0, 360), 0f), transform);
        centerMineral.transform.localScale *= centerMineralScale;
        centerMineral.SetValue(centerMineralValue);
        minerals.Add(centerMineral);

        if (layout != null && layout.Veins.Count > 0)
        {
            foreach (var spot in layout.Veins)
            {
                var vein = Instantiate(mineralPrefab, spot.Position, Quaternion.Euler(0f, rng.Next(0, 360), 0f), transform);
                vein.SetValue(spot.Capacity);
                minerals.Add(vein);
            }

            return;
        }

        for (int i = 0; i < cornerMinerals; i++)
        {
            int k = i % 4;
            float sx = (k & 1) == 0 ? 1f : -1f;
            float sz = (k & 2) == 0 ? 1f : -1f;

            float jitterX = (float)(rng.NextDouble() * 2f - 1f) * cornerJitter;
            float jitterZ = (float)(rng.NextDouble() * 2f - 1f) * cornerJitter;

            float x = sx * (arenaHalfSize - cornerInset) + jitterX;
            float z = sz * (arenaHalfSize - cornerInset) + jitterZ;
            Vector3 point = new Vector3(x, center.y, z);

            if (!NavMesh.SamplePosition(point, out var hit, 4f, filter)) continue;

            var mineral = Instantiate(mineralPrefab, hit.position, Quaternion.Euler(0f, rng.Next(0, 360), 0f), transform);
            mineral.SetValue(cornerMineralValue);
            minerals.Add(mineral);
        }
    }

    [Button] public void Respawn()
    {
        foreach (var controller in spawned)
        {
            if (controller == null) continue;
            if (targetGroup != null) targetGroup.RemoveMember(controller.transform);
            Destroy(controller.gameObject);
        }
        spawned.Clear();

        foreach (var mineral in minerals)
            if (mineral != null) Destroy(mineral.gameObject);
        minerals.Clear();

        foreach (var exit in exits)
            if (exit != null) Destroy(exit.gameObject);
        exits.Clear();

        if (layout != null) layout.Clear();

        Spawn();
    }

    [Button] public void Reseed()
    {
        seed = new System.Random().Next(1, 999999);
        randomizeEachPlay = false;
        Respawn();
    }
}
}
