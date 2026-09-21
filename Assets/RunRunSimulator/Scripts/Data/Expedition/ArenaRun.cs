using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class ArenaRun
{
    public const int HealthPerKnock = 15;
    public const int BuffHealth = 30;
    public const float MaxHealth = 100f;
    public const int BuffEvery = 3;

    private readonly List<string> teamIds = new();
    private readonly Dictionary<string, float> health = new();

    public int BaseSeed { get; }
    public int Floor { get; private set; }
    public int Material { get; private set; }
    public bool Lost { get; private set; }
    public int FloorsCompleted { get; private set; }
    public IReadOnlyList<string> TeamIds => teamIds;

    public ArenaRun(int baseSeed, IReadOnlyList<string> teamIds)
    {
        BaseSeed = baseSeed;
        if (teamIds == null) return;
        for (int i = 0; i < teamIds.Count; i++)
        {
            string id = teamIds[i];
            if (!string.IsNullOrEmpty(id)) this.teamIds.Add(id);
        }
    }

    public static int FloorSeedOf(int baseSeed, int n) => unchecked((baseSeed * 73856093) ^ (n * 19349663)) & 0x7fffffff;

    public int FloorSeed(int n) => FloorSeedOf(BaseSeed, n);

    public static ArenaFloorKind KindOf(int n) => n > 0 && n % BuffEvery == 0 ? ArenaFloorKind.Buff : ArenaFloorKind.Enemies;

    public ArenaFloorKind CurrentKind => KindOf(Floor);
    public int NextFloor => Floor + 1;
    public ArenaFloorKind NextKind => KindOf(NextFloor);

    public float HealthOf(string id) => health.TryGetValue(id, out float value) ? value : MaxHealth;

    public bool IsDown(string id) => HealthOf(id) <= 0f;

    private void Change(string id, float delta)
    {
        if (!health.ContainsKey(id)) health[id] = MaxHealth;
        if (IsDown(id)) return;
        health[id] = Mathf.Clamp(HealthOf(id) + delta, 0f, MaxHealth);
    }

    public void EnterFloor()
    {
        if (Lost) return;
        Floor++;
        if (CurrentKind != ArenaFloorKind.Buff) return;
        for (int i = 0; i < teamIds.Count; i++)
        {
            Change(teamIds[i], BuffHealth);
        }
    }

    public void RecordFloor(ExpeditionTeam winner, int playerSecured, IReadOnlyList<ArenaRoundStat> stats)
    {
        if (Lost) return;
        FloorsCompleted++;

        if (stats != null)
        {
            for (int i = 0; i < stats.Count; i++)
            {
                ArenaRoundStat stat = stats[i];
                if (stat.Team != ExpeditionTeam.Player || string.IsNullOrEmpty(stat.Id)) continue;
                if (!teamIds.Contains(stat.Id)) teamIds.Add(stat.Id);

                if (CurrentKind == ArenaFloorKind.Enemies)
                {
                    Change(stat.Id, -HealthPerKnock * stat.TimesKnocked);
                }
            }
        }

        Material += playerSecured;

        if (CurrentKind == ArenaFloorKind.Enemies && winner == ExpeditionTeam.Rival)
        {
            Lost = true;
            Material = 0;
        }
    }

    public ExpeditionResult ToResult()
    {
        var fallenIds = new List<string>();
        foreach (var pair in health)
        {
            if (pair.Value <= 0f) fallenIds.Add(pair.Key);
        }

        return new ExpeditionResult
        {
            Seed = BaseSeed,
            Winner = Lost ? ExpeditionTeam.Rival : ExpeditionTeam.Player,
            PlayerSecured = Lost ? 0 : Material,
            RivalSecured = 0,
            Floors = FloorsCompleted,
            Lost = Lost,
            FallenIds = fallenIds,
            Fallen = fallenIds.Count,
            TeamIds = new List<string>(teamIds)
        };
    }
}
}
