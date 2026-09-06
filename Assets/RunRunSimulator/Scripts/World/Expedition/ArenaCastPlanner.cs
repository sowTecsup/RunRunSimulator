using System;
using System.Collections.Generic;
namespace MoriMonchiSimulator
{

public class ArenaCastPlanner
{
    private static readonly Occupation[][] RivalPlans =
    {
        new[] { Occupation.Guard, Occupation.Gather, Occupation.Gather },
        new[] { Occupation.Break, Occupation.Gather, Occupation.Gather },
        new[] { Occupation.Decoy, Occupation.Guard, Occupation.Gather },
        new[] { Occupation.Gather, Occupation.Gather, Occupation.Gather },
        new[] { Occupation.Break, Occupation.Decoy, Occupation.Gather },
    };

    private static readonly ArenaSite[] GatherSites = { ArenaSite.Center, ArenaSite.NearVein, ArenaSite.FarVein };

    private readonly ArenaRosterSO roster;
    private readonly Func<CreatureDNA> mint;
    private readonly List<ArenaCastEntry> planned = new();
    private readonly Dictionary<string, ArenaCastEntry> remembered = new();
    private List<CreatureDNA> localPool;

    public ArenaCastPlanner(ArenaRosterSO roster, Func<CreatureDNA> mint)
    {
        this.roster = roster;
        this.mint = mint;
    }

    public IReadOnlyList<ArenaCastEntry> Planned => planned;
    public ArenaCastMode Mode { get; private set; } = ArenaCastMode.Roster;
    public int LocalCount { get; set; } = 3;
    public bool LocalAvailable { get; private set; } = true;
    public bool HasRoster => roster != null && roster.Entries != null && roster.Entries.Count > 0;

    public void SetMode(ArenaCastMode mode) => Mode = mode;

    public void Prepare(int roomSeed, int castSeed, int freeCount)
    {
        planned.Clear();
        UnityEngine.Random.InitState(castSeed);
        LocalAvailable = true;

        if (!HasRoster)
        {
            for (int i = 0; i < freeCount; i++)
                planned.Add(new ArenaCastEntry { Dna = mint(), Team = ExpeditionTeam.None, Occupation = Occupation.Gather, Site = ArenaSite.Center });
            return;
        }

        if (Mode == ArenaCastMode.LocalSave)
        {
            var picked = ArenaCastSource.Pick(LocalPool(), LocalCount, castSeed);
            LocalAvailable = picked.Count > 0;
            foreach (var dna in picked)
                planned.Add(Remembered(new ArenaCastEntry { Dna = dna, Team = ExpeditionTeam.Player, Occupation = Occupation.Gather, Site = ArenaSite.Center }));
        }

        if (Mode == ArenaCastMode.Roster || !LocalAvailable)
        {
            foreach (var entry in roster.Entries)
                if (entry.Team == ExpeditionTeam.Player)
                    planned.Add(Remembered(FromRoster(entry, entry.Occupation, ArenaSite.Center)));
        }

        var plan = RivalPlans[Math.Abs(roomSeed) % RivalPlans.Length];
        int rivalIndex = 0;
        foreach (var entry in roster.Entries)
        {
            if (entry.Team != ExpeditionTeam.Rival) continue;
            var occupation = plan[rivalIndex % plan.Length];
            var site = occupation == Occupation.Gather ? GatherSites[rivalIndex % GatherSites.Length] : ArenaSite.Center;
            planned.Add(FromRoster(entry, occupation, site));
            rivalIndex++;
        }
    }

    public void SetPlayerPlan(int index, Occupation occupation, ArenaSite site)
    {
        if (index < 0 || index >= planned.Count) return;
        var entry = planned[index];
        if (entry.Team != ExpeditionTeam.Player) return;
        entry.Occupation = occupation == Occupation.None ? Occupation.Gather : occupation;
        entry.Site = site;
        planned[index] = entry;
        remembered[PlanKey(entry.Dna)] = entry;
    }

    private List<CreatureDNA> LocalPool() => localPool ??= ArenaCastSource.LoadLocal();

    private ArenaCastEntry FromRoster(ArenaRosterSO.Entry entry, Occupation occupation, ArenaSite site)
    {
        var dna = mint();
        dna.Sociability = entry.Sociability;
        dna.Boldness = entry.Boldness;
        if (!string.IsNullOrEmpty(entry.Name)) dna.CustomName = entry.Name;
        if (!string.IsNullOrEmpty(entry.BodyShapeID)) dna.BodyShapeID = entry.BodyShapeID;
        if (entry.BaseColor.a > 0f) dna.BaseColor = entry.BaseColor;
        dna.Stamp();

        return new ArenaCastEntry { Dna = dna, Team = entry.Team, Occupation = occupation, Site = site };
    }

    private ArenaCastEntry Remembered(ArenaCastEntry entry)
    {
        if (!remembered.TryGetValue(PlanKey(entry.Dna), out var previous)) return entry;
        entry.Occupation = previous.Occupation;
        entry.Site = previous.Site;
        return entry;
    }

    private static string PlanKey(CreatureDNA dna) => dna == null ? "" : dna.CustomName;
}
}
