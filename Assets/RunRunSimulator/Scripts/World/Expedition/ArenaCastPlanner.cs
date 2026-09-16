using System;
using System.Collections.Generic;
namespace MoriMonchiSimulator
{

public class ArenaCastPlanner
{
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
    public bool HasTeams => Mode == ArenaCastMode.LocalSave || HasRoster;

    private const int RivalCount = 3;

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

        if (!HasTeams)
        {
            for (int i = 0; i < freeCount; i++)
            {
                var dna = mint();
                planned.Add(new ArenaCastEntry { Dna = dna, Team = ExpeditionTeam.None, Orders = ArenaBases.ToOrders(dna.Role, ArenaBases.Default(dna.Role)) });
            }
            return;
        }

        if (Mode == ArenaCastMode.LocalSave)
        {
            var picked = localSelection.Count > 0 ? localSelection : ArenaCastSource.Pick(Pool(), LocalCount, castSeed);
            LocalAvailable = picked.Count > 0;
            foreach (var dna in picked)
                planned.Add(Remembered(new ArenaCastEntry { Dna = dna, Team = ExpeditionTeam.Player, Orders = ArenaOrderRules.Clamp(dna, rules, ArenaBases.ToOrders(dna.Role, ArenaBases.Default(dna.Role))) }));
        }

        if (Mode == ArenaCastMode.Roster || (!LocalAvailable && HasRoster))
        {
            foreach (var entry in roster.Entries)
                if (entry.Team == ExpeditionTeam.Player)
                    planned.Add(Remembered(FromRoster(entry, ArenaBases.ToOrders(entry.Role, ArenaBases.Default(entry.Role)))));
        }
        else if (Mode == ArenaCastMode.LocalSave && !LocalAvailable)
        {
            for (int i = 0; i < LocalCount; i++)
            {
                var dna = mint();
                planned.Add(Remembered(new ArenaCastEntry { Dna = dna, Team = ExpeditionTeam.Player, Orders = ArenaOrderRules.Clamp(dna, rules, ArenaBases.ToOrders(dna.Role, ArenaBases.Default(dna.Role))) }));
            }
        }

        if (Mode == ArenaCastMode.LocalSave)
        {
            UnityEngine.Random.InitState(roomSeed);
            for (int i = 0; i < RivalCount; i++)
            {
                var dna = mint();
                dna.Timestamp += i + 1;
                ArenaBase baseValue = ArenaBases.OpenAt(dna.Role, (Math.Abs(roomSeed) >> i) & 1);
                planned.Add(new ArenaCastEntry { Dna = dna, Team = ExpeditionTeam.Rival, Orders = ArenaBases.ToOrders(dna.Role, baseValue) });
            }
            UnityEngine.Random.InitState(castSeed);
        }
        else
        {
            int rivalIndex = 0;
            foreach (var entry in roster.Entries)
            {
                if (entry.Team != ExpeditionTeam.Rival) continue;
                ArenaBase baseValue = ArenaBases.OpenAt(entry.Role, (Math.Abs(roomSeed) >> rivalIndex) & 1);
                planned.Add(FromRoster(entry, ArenaBases.ToOrders(entry.Role, baseValue)));
                rivalIndex++;
            }
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
        dna.Role = entry.Role;
        dna.Sociability = entry.Sociability;
        dna.Boldness = entry.Boldness;
        if (!string.IsNullOrEmpty(entry.Name)) dna.CustomName = entry.Name;
        if (!string.IsNullOrEmpty(entry.BodyShapeID)) dna.BodyShapeID = entry.BodyShapeID;
        if (!string.IsNullOrEmpty(entry.HornID)) dna.HornID = entry.HornID;
        if (!string.IsNullOrEmpty(entry.BackID)) dna.BackID = entry.BackID;
        if (!string.IsNullOrEmpty(entry.WingID)) dna.WingID = entry.WingID;
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

    private static string PlanKey(CreatureDNA dna)
    {
        if (dna == null) return "";
        var id = dna.UniqueID;
        return string.IsNullOrEmpty(id) ? dna.CustomName : id;
    }
}
}
