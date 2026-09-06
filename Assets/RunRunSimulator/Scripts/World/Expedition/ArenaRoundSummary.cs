using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

public struct ArenaRoundStat
{
    public string Name;
    public ExpeditionTeam Team;
    public Occupation Occupation;
    public Color Color;
    public int Secured;
    public int Collected;
    public int HitsLanded;
    public int TimesKnocked;
    public int Reports;
}

public static class ArenaRoundSummary
{
    public static List<ArenaRoundStat> Capture(IReadOnlyList<MoriMonchiController> spawned)
    {
        var result = new List<ArenaRoundStat>();
        for (int i = 0; i < spawned.Count; i++)
        {
            MoriMonchiController controller = spawned[i];
            if (controller == null) continue;

            MoriMochiAgent agent = controller.Agent;
            if (agent == null || agent.DNA == null) continue;

            Color color = agent.DNA.BaseColor;
            color.a = 1f;

            result.Add(new ArenaRoundStat
            {
                Name = agent.DNA.CustomName,
                Team = agent.Team,
                Occupation = agent.Occupation,
                Color = color,
                Secured = agent.SecuredMaterial,
                Collected = agent.CollectedMaterial,
                HitsLanded = agent.ClashHitsLanded,
                TimesKnocked = agent.ClashTimesKnocked,
                Reports = agent.ScoutReports
            });
        }
        return result;
    }
}
}
