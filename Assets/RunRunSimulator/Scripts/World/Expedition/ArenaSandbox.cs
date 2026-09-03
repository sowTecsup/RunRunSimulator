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
    [Required, SerializeField] private MonchiVisualBankSO visualBank;
    [Required, SerializeField] private FurTypeDatabaseSO furDatabase;
    [Required, SerializeField] private CreatureDatabaseSO creatureDatabase;

    [SerializeField] private Transform observer;
    [SerializeField] private Transform spawnCenter;

    [SerializeField] private int seed = 4242;
    [SerializeField] private bool randomizeEachPlay;
    [SerializeField, Min(1)] private int count = 3;
    [SerializeField, Min(1f)] private float spawnRadius = 4f;
    [SerializeField] private bool keepNeedsFull = true;

    [Required, SerializeField] private MaterialPickup mineralPrefab;
    [SerializeField, Min(0)] private int cornerMinerals = 4;
    [SerializeField, Min(0f)] private float cornerInset = 6f;
    [SerializeField, Min(0f)] private float cornerJitter = 2f;
    [SerializeField, Min(1f)] private float centerMineralScale = 2.5f;
    [SerializeField, Min(1)] private int centerMineralValue = 5;
    [SerializeField, Min(0f)] private float arenaHalfSize = 20f;

    private readonly List<MoriMonchiController> spawned = new();
    private readonly List<MaterialPickup> minerals = new();
    private int activeSeed;
    private Transform spawnHolder;

    public IReadOnlyList<MoriMonchiController> Spawned => spawned;
    public IReadOnlyList<MaterialPickup> Minerals => minerals;
    public int ActiveSeed => activeSeed;

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
        Random.InitState(activeSeed);
        var rng = new System.Random(activeSeed);

        Vector3 center = spawnCenter != null ? spawnCenter.position : transform.position;

        int agentType = creaturePrefab.GetComponent<NavMeshAgent>().agentTypeID;
        var filter = new NavMeshQueryFilter { agentTypeID = agentType, areaMask = NavMesh.AllAreas };

        for (int i = 0; i < count; i++)
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

            float angle = (float)(rng.NextDouble() * Mathf.PI * 2f);
            float dist = (float)(rng.NextDouble() * spawnRadius);
            Vector3 point = center + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);

            Vector3 pos = NavMesh.SamplePosition(point, out var hit, spawnRadius, filter)
                ? hit.position
                : center;

            var controller = Instantiate(creaturePrefab, pos, Quaternion.Euler(0f, rng.Next(0, 360), 0f), spawnHolder);
            controller.GetComponent<NavMeshAgent>().areaMask = NavMesh.AllAreas;
            controller.transform.SetParent(transform, true);
            controller.Initialize(dna, profileTable, observer, visualBank, furDatabase);
            spawned.Add(controller);
        }

        SpawnMinerals(rng, filter, center);

        Debug.Log($"[ArenaSandbox] seed={activeSeed} spawned={spawned.Count} minerals={minerals.Count}");
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
            minerals.Add(mineral);
        }
    }

    [Button] public void Respawn()
    {
        foreach (var controller in spawned)
            if (controller != null) Destroy(controller.gameObject);
        spawned.Clear();

        foreach (var mineral in minerals)
            if (mineral != null) Destroy(mineral.gameObject);
        minerals.Clear();

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
