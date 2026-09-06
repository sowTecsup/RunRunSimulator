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
    [SerializeField] private ArenaCastMode castMode = ArenaCastMode.Roster;
    [SerializeField, Min(1)] private int localCastCount = 3;
    [SerializeField] private bool autoSpawnCast = true;
    [SerializeField, Min(0f)] private float teamSpawnInset = 9f;
    [SerializeField, Min(0.5f)] private float teamSpawnRadius = 2.5f;
    [Required, SerializeField] private ExitZone exitPrefab;
    [SerializeField, Min(0f)] private float exitInset = 4f;

    [Title("Sala")]
    [Required, SerializeField] private MaterialPickup mineralPrefab;
    [SerializeField] private ArenaLayoutBuilder layout;
    [SerializeField] private ArenaPaletteApplier palette;
    [SerializeField] private int paletteIndex = -1;
    [SerializeField, Min(1f)] private float centerMineralScale = 2.5f;
    [SerializeField, Min(1)] private int centerMineralValue = 5;
    [SerializeField, Min(0f)] private float arenaHalfSize = 20f;

    private readonly List<MoriMonchiController> spawned = new();
    private readonly List<MaterialPickup> minerals = new();
    private readonly List<ExitZone> exits = new();
    private readonly List<Perceivable> looseBuffer = new();
    private ArenaCastPlanner planner;
    private int activeSeed;
    private Transform spawnHolder;
    private System.Random rng;
    private NavMeshQueryFilter filter;
    private Vector3 center;
    private bool roomBuilt;

    public IReadOnlyList<MoriMonchiController> Spawned => spawned;
    public IReadOnlyList<ExitZone> Exits => exits;
    public IReadOnlyList<ArenaCastEntry> PlannedCast => Planner.Planned;
    public int ActiveSeed => activeSeed;
    public ArenaCastMode CastMode => Planner.Mode;
    public bool LocalCastAvailable => Planner.LocalAvailable;
    public IReadOnlyList<CreatureDNA> LocalPool => Planner.LocalPool;
    public string EntryName => layout != null && layout.IsBuilt ? layout.EntryName : "diagonal";
    public string PaletteName => palette != null && palette.Current != null ? palette.Current.DisplayName : "";

    private ArenaCastPlanner Planner
    {
        get
        {
            if (planner == null)
            {
                planner = new ArenaCastPlanner(useRoster ? roster : null, MintRandom) { LocalCount = localCastCount };
                planner.SetMode(castMode);
            }
            return planner;
        }
    }

    public ExitZone ExitFor(ExpeditionTeam team)
    {
        foreach (var exit in exits)
            if (exit != null && exit.Team == team) return exit;
        return null;
    }

    private void OnEnable()
    {
        ExpeditionRulesSO.Activate(expeditionRules);
    }

    private void OnDisable()
    {
        ExpeditionRulesSO.Deactivate(expeditionRules);
    }

    private void Start()
    {
        Application.runInBackground = true;
        BuildRoom();
        if (autoSpawnCast) SpawnCast();
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

    public void BuildRoom()
    {
        if (spawnHolder == null)
        {
            spawnHolder = new GameObject("SpawnHolder").transform;
            spawnHolder.SetParent(transform);
            spawnHolder.gameObject.SetActive(false);
        }

        activeSeed = randomizeEachPlay ? System.Environment.TickCount : seed;
        rng = new System.Random(activeSeed);
        center = spawnCenter != null ? spawnCenter.position : transform.position;

        int agentType = creaturePrefab.GetComponent<NavMeshAgent>().agentTypeID;
        filter = new NavMeshQueryFilter { agentTypeID = agentType, areaMask = NavMesh.AllAreas };

        if (layout != null) layout.Build(activeSeed, filter);
        if (palette != null) palette.ApplyIndex(paletteIndex >= 0 ? paletteIndex : palette.IndexForSeed(activeSeed));
        if (Planner.HasRoster) SpawnExits();
        SpawnMinerals();

        roomBuilt = true;
        Planner.Prepare(activeSeed, castSeed, count);

        Debug.Log($"[ArenaSandbox] sala={activeSeed} entrada={EntryName} paleta={PaletteName} minerales={minerals.Count} salidas={exits.Count} elenco={CastMode} planeados={PlannedCast.Count}");
    }

    public void SetPlayerPlan(int index, Occupation occupation, ArenaSite site) => Planner.SetPlayerPlan(index, occupation, site);

    public void SetCastMode(ArenaCastMode mode)
    {
        Planner.SetMode(mode);
        Planner.Prepare(activeSeed, castSeed, count);
    }

    public void ShuffleCast()
    {
        Planner.ClearLocalSelection();
        castSeed++;
        Planner.Prepare(activeSeed, castSeed, count);
    }

    public void SelectLocalCast(IReadOnlyList<CreatureDNA> picks)
    {
        Planner.SelectLocal(picks);
        Planner.Prepare(activeSeed, castSeed, count);
    }

    public void SetPaletteIndex(int index)
    {
        paletteIndex = index;
        if (palette != null) palette.ApplyIndex(index);
    }

    public void CyclePalette()
    {
        if (palette == null || palette.Palettes.Count == 0) return;
        SetPaletteIndex((palette.CurrentIndex + 1) % palette.Palettes.Count);
    }

    public void SpawnCast()
    {
        if (!roomBuilt) BuildRoom();
        if (spawned.Count > 0) ClearCast();

        foreach (var entry in PlannedCast)
        {
            Vector3 around = entry.Team == ExpeditionTeam.None ? center : TeamCorner(entry.Team);
            float radius = entry.Team == ExpeditionTeam.None ? spawnRadius : teamSpawnRadius;
            var controller = SpawnCreature(entry.Dna, around, radius, entry.Team, entry.Occupation, ExitFor(entry.Team));
            controller.Agent.SetGuardPost(ResolveSite(entry));
        }

        Debug.Log($"[ArenaSandbox] elenco={spawned.Count} modo={CastMode} sala={activeSeed} castSeed={castSeed}");
    }

    public void ClearCast()
    {
        foreach (var controller in spawned)
        {
            if (controller == null) continue;
            if (targetGroup != null) targetGroup.RemoveMember(controller.transform);
            Destroy(controller.gameObject);
        }
        spawned.Clear();
    }

    public void ResetRoom(bool newSeed)
    {
        ClearCast();

        foreach (var mineral in minerals)
            if (mineral != null) Destroy(mineral.gameObject);
        minerals.Clear();

        PerceivableRegistry.QueryInRadius(center, 200f, null, looseBuffer);
        foreach (var p in looseBuffer)
            if (p != null && p.Kind == PerceivableKind.Material) Destroy(p.gameObject);

        foreach (var exit in exits)
            if (exit != null) Destroy(exit.gameObject);
        exits.Clear();

        if (layout != null) layout.Clear();

        if (newSeed)
        {
            seed = new System.Random().Next(1, 999999);
            randomizeEachPlay = false;
        }

        roomBuilt = false;
        BuildRoom();
    }

    [Button] public void Respawn()
    {
        ResetRoom(false);
        SpawnCast();
    }

    [Button] public void Reseed()
    {
        ResetRoom(true);
        SpawnCast();
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

    private MoriMonchiController SpawnCreature(CreatureDNA dna, Vector3 around, float radius, ExpeditionTeam team, Occupation occupation, ExitZone home)
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
        spawned.Add(controller);
        if (targetGroup != null) targetGroup.AddMember(controller.transform, 1f, 1.2f);
        foreach (var tag in controller.GetComponentsInChildren<NameTag>(true)) { tag.ShowDistance = tagShowDistance; tag.ScreenSizeReferenceDistance = tagReferenceDistance; }

        return controller;
    }

    private Transform ResolveSite(ArenaCastEntry entry)
    {
        if (minerals.Count == 0 || minerals[0] == null) return null;

        switch (entry.Site)
        {
            case ArenaSite.NearVein:
            {
                var own = ExitFor(entry.Team);
                var vein = NearestVein(own != null ? own.transform.position : center);
                return vein != null ? vein.transform : minerals[0].transform;
            }
            case ArenaSite.FarVein:
            {
                var rival = ExitFor(RivalOf(entry.Team));
                var vein = NearestVein(rival != null ? rival.transform.position : center);
                return vein != null ? vein.transform : minerals[0].transform;
            }
            default:
                return minerals[0].transform;
        }
    }

    private MaterialPickup NearestVein(Vector3 from)
    {
        MaterialPickup best = null;
        float bestSqr = float.PositiveInfinity;
        for (int i = 1; i < minerals.Count; i++)
        {
            var mineral = minerals[i];
            if (mineral == null) continue;
            Vector3 d = mineral.transform.position - from;
            d.y = 0f;
            if (d.sqrMagnitude < bestSqr) { bestSqr = d.sqrMagnitude; best = mineral; }
        }
        return best;
    }

    private static ExpeditionTeam RivalOf(ExpeditionTeam team) =>
        team == ExpeditionTeam.Player ? ExpeditionTeam.Rival :
        team == ExpeditionTeam.Rival ? ExpeditionTeam.Player : ExpeditionTeam.None;

    private Vector3 TeamCorner(ExpeditionTeam team)
    {
        if (layout != null && layout.IsBuilt && team != ExpeditionTeam.None) return layout.SpawnPoint(team);

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

    private void SpawnExits()
    {
        SpawnExit(ExpeditionTeam.Player, new Vector3(-1f, 0f, -1f));
        SpawnExit(ExpeditionTeam.Rival, new Vector3(1f, 0f, 1f));
    }

    private void SpawnExit(ExpeditionTeam team, Vector3 dir)
    {
        Vector3 point = layout != null && layout.IsBuilt ? layout.ExitPoint(team) : center + dir * (arenaHalfSize - exitInset);
        Vector3 pos = NavMesh.SamplePosition(point, out var hit, 4f, filter)
            ? hit.position
            : point;

        var exit = Instantiate(exitPrefab, pos, Quaternion.identity, transform);
        exit.SetTeam(team);
        exits.Add(exit);
    }

    private void SpawnMinerals()
    {
        Vector3 centerPos = NavMesh.SamplePosition(center, out var centerHit, 2f, filter)
            ? centerHit.position
            : center;

        var centerMineral = Instantiate(mineralPrefab, centerPos, Quaternion.Euler(0f, rng.Next(0, 360), 0f), transform);
        centerMineral.transform.localScale *= centerMineralScale;
        centerMineral.SetValue(centerMineralValue);
        minerals.Add(centerMineral);

        if (layout == null) return;

        foreach (var spot in layout.Veins)
        {
            var vein = Instantiate(mineralPrefab, spot.Position, Quaternion.Euler(0f, rng.Next(0, 360), 0f), transform);
            vein.SetValue(spot.Capacity);
            minerals.Add(vein);
        }
    }
}
}
