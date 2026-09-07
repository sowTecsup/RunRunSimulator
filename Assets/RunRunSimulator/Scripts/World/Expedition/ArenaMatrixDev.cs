using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class ArenaMatrixDev : MonoBehaviour
{
    public ArenaSandbox Sandbox;
    public ArenaRound Round;
    public ArenaClockControl Clock;
    [Min(0.1f)] public float Speed = 10f;

    public bool IsRunning { get; private set; }
    public bool Done { get; private set; }
    public int Completed { get; private set; }
    public int Total { get; private set; }
    public string OutputPath { get; private set; }
    public string Progress { get; private set; }

    public void Run(IReadOnlyList<ArenaMatrixTeam> players, IReadOnlyList<ArenaMatrixTeam> rivals, IReadOnlyList<int> seeds, string csvPath)
    {
        if (IsRunning) return;
        if (Sandbox == null || Round == null)
        {
            Debug.LogWarning("[ArenaMatrixDev] Falta Sandbox o Round.");
            return;
        }
        StartCoroutine(Loop(players, rivals, seeds, csvPath));
    }

    public void Stop()
    {
        IsRunning = false;
    }

    private void OnDisable()
    {
        if (IsRunning)
        {
            if (Clock != null) Clock.Set(1f); else Time.timeScale = 1f;
            IsRunning = false;
        }
    }

    private IEnumerator Loop(IReadOnlyList<ArenaMatrixTeam> players, IReadOnlyList<ArenaMatrixTeam> rivals, IReadOnlyList<int> seeds, string csvPath)
    {
        IsRunning = true;
        Done = false;
        Completed = 0;
        Total = seeds.Count * players.Count * rivals.Count;
        OutputPath = csvPath;

        if (!File.Exists(csvPath))
            File.WriteAllText(csvPath, "i;j;player;rival;seed;pScore;rScore;winner;secs;pFled;rFled;pHits;rHits;pKnocked;rKnocked;pCol;rCol;detail\n", Encoding.UTF8);

        if (Clock != null) Clock.Set(Speed); else Time.timeScale = Speed;

        for (int s = 0; s < seeds.Count && IsRunning; s++)
        {
            Sandbox.SetSeed(seeds[s]);
            for (int i = 0; i < players.Count && IsRunning; i++)
            {
                for (int j = 0; j < rivals.Count && IsRunning; j++)
                {
                    Round.Reset(false);
                    yield return null;

                    Apply(players[i], ExpeditionTeam.Player);
                    Apply(rivals[j], ExpeditionTeam.Rival);

                    float started = Time.realtimeSinceStartup;
                    Round.Launch();

                    float limit = Round.RoundSeconds * 3f / Mathf.Max(0.1f, Time.timeScale) + 30f;
                    while (!Round.IsOver && Time.realtimeSinceStartup - started < limit)
                        yield return null;

                    if (!Round.IsOver) Round.End();

                    AppendLine(csvPath, i, j, players[i].Name, rivals[j].Name, seeds[s], started);

                    Completed++;
                    Progress = Completed + "/" + Total + " · " + players[i].Name + " vs " + rivals[j].Name;
                    Debug.Log("[ArenaMatrixDev] " + Progress + " → " + Round.PlayerSecured + "-" + Round.RivalSecured);
                    File.WriteAllText(csvPath + ".progress", Progress, Encoding.UTF8);

                    yield return null;
                }
            }
        }

        if (Clock != null) Clock.Set(1f); else Time.timeScale = 1f;
        IsRunning = false;
        Done = true;
        File.WriteAllText(csvPath + ".done", "ok", Encoding.UTF8);
    }

    private void Apply(ArenaMatrixTeam team, ExpeditionTeam side)
    {
        var planned = Sandbox.PlannedCast;
        int k = 0;
        for (int index = 0; index < planned.Count; index++)
        {
            var entry = planned[index];
            if (entry.Team != side) continue;

            if (k < team.Orders.Length)
            {
                entry.Dna.Boldness = team.Boldness[k];
                entry.Dna.Sociability = team.Sociability[k];
                Sandbox.SetOrders(index, team.Orders[k]);
            }
            k++;
        }
    }

    private void AppendLine(string csvPath, int i, int j, string playerName, string rivalName, int seed, float startedReal)
    {
        int pFled = 0, rFled = 0, pHits = 0, rHits = 0, pKnocked = 0, rKnocked = 0, pCol = 0, rCol = 0;
        var detail = new StringBuilder();
        var summary = Round.Summary;
        for (int k = 0; k < summary.Count; k++)
        {
            var stat = summary[k];
            bool isPlayer = stat.Team == ExpeditionTeam.Player;
            if (isPlayer)
            {
                pFled += stat.Fled;
                pHits += stat.HitsLanded;
                pKnocked += stat.TimesKnocked;
                pCol += stat.Collected;
            }
            else
            {
                rFled += stat.Fled;
                rHits += stat.HitsLanded;
                rKnocked += stat.TimesKnocked;
                rCol += stat.Collected;
            }

            string code = ArenaOrderCatalog.ArchetypeShort(stat.Orders);
            code = code.Length > 3 ? code.Substring(0, 3) : code;
            string site = stat.Orders.Loot == LootChoice.Big ? "C" : "V";
            detail.Append(isPlayer ? "P" : "R").Append(":").Append(code).Append(site).Append("=")
                  .Append(stat.Secured).Append("/").Append(stat.Collected).Append("/").Append(stat.HitsLanded).Append("/")
                  .Append(stat.TimesKnocked).Append("/").Append(stat.Fled).Append(" ");
        }

        int secs = Mathf.RoundToInt(Time.realtimeSinceStartup - startedReal);
        string line = i + ";" + j + ";" + playerName + ";" + rivalName + ";" + seed + ";" +
            Round.PlayerSecured + ";" + Round.RivalSecured + ";" + Round.Winner + ";" + secs + ";" +
            pFled + ";" + rFled + ";" + pHits + ";" + rHits + ";" + pKnocked + ";" + rKnocked + ";" +
            pCol + ";" + rCol + ";" + detail + "\n";

        File.AppendAllText(csvPath, line, Encoding.UTF8);
    }
}
}
