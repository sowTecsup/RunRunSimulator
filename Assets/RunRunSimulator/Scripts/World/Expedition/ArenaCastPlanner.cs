using System;
using System.Collections.Generic;
namespace MoriMonchiSimulator
{

public class ArenaCastPlanner
{
    private static readonly ArenaOrders[][] RivalPlans =
    {
        new[]
        {
            new ArenaOrders(LootChoice.Big, ContactChoice.Fight, PostureChoice.Protect),
            new ArenaOrders(LootChoice.Big, ContactChoice.Flee, PostureChoice.Protect),
            new ArenaOrders(LootChoice.Big, ContactChoice.Flee, PostureChoice.Protect),
        },
        new[]
        {
            new ArenaOrders(LootChoice.Small, ContactChoice.Flee, PostureChoice.Protect),
            new ArenaOrders(LootChoice.Small, ContactChoice.Flee, PostureChoice.Protect),
            new ArenaOrders(LootChoice.Big, ContactChoice.Flee, PostureChoice.Aggressive),
        },
        new[]
        {
            new ArenaOrders(LootChoice.Big, ContactChoice.Fight, PostureChoice.Aggressive),
            new ArenaOrders(LootChoice.Small, ContactChoice.Fight, PostureChoice.Aggressive),
            new ArenaOrders(LootChoice.Small, ContactChoice.Flee, PostureChoice.Protect),
        },
        new[]
        {
            new ArenaOrders(LootChoice.Small, ContactChoice.Fight, PostureChoice.Protect),
            new ArenaOrders(LootChoice.Small, ContactChoice.Flee, PostureChoice.Protect),
            new ArenaOrders(LootChoice.Small, ContactChoice.Flee, PostureChoice.Protect),
        },
        new[]
        {
            new ArenaOrders(LootChoice.Small, ContactChoice.Fight, PostureChoice.Aggressive),
            new ArenaOrders(LootChoice.Small, ContactChoice.Flee, PostureChoice.Aggressive),
            new ArenaOrders(LootChoice.Big, ContactChoice.Flee, PostureChoice.Protect),
        },
        new[]
        {
            new ArenaOrders(LootChoice.Big, ContactChoice.Fight, PostureChoice.Protect),
            new ArenaOrders(LootChoice.Big, ContactChoice.Fight, PostureChoice.Aggressive),
            new ArenaOrders(LootChoice.Small, ContactChoice.Flee, PostureChoice.Protect),
        },
    };

    private readonly ArenaRosterSO roster;
    private readonly Func<CreatureDNA> mint;
    private readonly ExpeditionRulesSO rules;
    private readonly List<ArenaCastEntry> planned = new();
    private readonly Dictionary<string, ArenaCastEntry> remembered = new();
    private readonly List<CreatureDNA> localSelection = new();
    private List<CreatureDNA> localPool;

    public ArenaCastPlanner(ArenaRosterSO roster, Func<CreatureDNA> mint, ExpeditionRulesSO rules)
    {
        this.roster = roster;
        this.mint = mint;
        this.rules = rules;
    }

    public IReadOnlyList<ArenaCastEntry> Planned => planned;
    public ArenaCastMode Mode { get; private set; } = ArenaCastMode.Roster;
    public int LocalCount { get; set; } = 3;
    public bool LocalAvailable { get; private set; } = true;
    public bool HasRoster => roster != null && roster.Entries != null && roster.Entries.Count > 0;
    public IReadOnlyList<CreatureDNA> LocalPool => Pool();
    public bool HasLocalSelection => localSelection.Count > 0;

    public void SetMode(ArenaCastMode mode) => Mode = mode;

    public void SelectLocal(IReadOnlyList<CreatureDNA> picks)
    {
        localSelection.Clear();
        if (picks == null) return;
        foreach (var dna in picks)
        {
            if (dna == null) continue;
            if (localSelection.Count >= LocalCount) break;
            localSelection.Add(dna);
        }
    }

    public void ClearLocalSelection() => localSelection.Clear();

    public void Prepare(int roomSeed, int castSeed, int freeCount)
    {
        planned.Clear();
        UnityEngine.Random.InitState(castSeed);
        LocalAvailable = true;

        if (!HasRoster)
        {
            for (int i = 0; i < freeCount; i++)
                planned.Add(new ArenaCastEntry { Dna = mint(), Team = ExpeditionTeam.None, Orders = ArenaOrders.Default });
            return;
        }

        if (Mode == ArenaCastMode.LocalSave)
        {
            var picked = localSelection.Count > 0 ? localSelection : ArenaCastSource.Pick(Pool(), LocalCount, castSeed);
            LocalAvailable = picked.Count > 0;
            foreach (var dna in picked)
                planned.Add(Remembered(new ArenaCastEntry { Dna = dna, Team = ExpeditionTeam.Player, Orders = ArenaOrderRules.Clamp(dna, rules, ArenaOrders.Default) }));
        }

        if (Mode == ArenaCastMode.Roster || !LocalAvailable)
        {
            foreach (var entry in roster.Entries)
                if (entry.Team == ExpeditionTeam.Player)
                    planned.Add(Remembered(FromRoster(entry, ArenaOrderRules.FromOccupation(entry.Occupation, ArenaSite.Center))));
        }

        var plan = RivalPlans[Math.Abs(roomSeed) % RivalPlans.Length];
        int rivalIndex = 0;
        foreach (var entry in roster.Entries)
        {
            if (entry.Team != ExpeditionTeam.Rival) continue;
            planned.Add(FromRoster(entry, plan[rivalIndex % plan.Length]));
            rivalIndex++;
        }
    }

    public void SetPlayerOrders(int index, ArenaOrders orders)
    {
        if (index < 0 || index >= planned.Count) return;
        if (planned[index].Team != ExpeditionTeam.Player) return;
        SetOrders(index, orders);
    }

    public void SetOrders(int index, ArenaOrders orders)
    {
        if (index < 0 || index >= planned.Count) return;
        var entry = planned[index];
        entry.Orders = ArenaOrderRules.Clamp(entry.Dna, rules, orders);
        planned[index] = entry;
        if (entry.Team == ExpeditionTeam.Player) remembered[PlanKey(entry.Dna)] = entry;
    }

    private List<CreatureDNA> Pool() => localPool ??= ArenaCastSource.LoadLocal();

    private ArenaCastEntry FromRoster(ArenaRosterSO.Entry entry, ArenaOrders orders)
    {
        var dna = mint();
        dna.Sociability = entry.Sociability;
        dna.Boldness = entry.Boldness;
        if (!string.IsNullOrEmpty(entry.Name)) dna.CustomName = entry.Name;
        if (!string.IsNullOrEmpty(entry.BodyShapeID)) dna.BodyShapeID = entry.BodyShapeID;
        if (entry.BaseColor.a > 0f) dna.BaseColor = entry.BaseColor;
        dna.Stamp();

        return new ArenaCastEntry { Dna = dna, Team = entry.Team, Orders = ArenaOrderRules.Clamp(dna, rules, orders) };
    }

    private ArenaCastEntry Remembered(ArenaCastEntry entry)
    {
        if (!remembered.TryGetValue(PlanKey(entry.Dna), out var previous)) return entry;
        entry.Orders = ArenaOrderRules.Clamp(entry.Dna, rules, previous.Orders);
        return entry;
    }

    private static string PlanKey(CreatureDNA dna) => dna == null ? "" : dna.CustomName;
}
}
