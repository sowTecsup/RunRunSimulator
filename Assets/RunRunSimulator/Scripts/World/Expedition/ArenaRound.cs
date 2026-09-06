using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MoriMonchiSimulator
{

public class ArenaRound : MonoBehaviour
{
    [Required, SerializeField] private ArenaSandbox sandbox;
    [SerializeField, Min(10f)] private float roundSeconds = 90f;
    [SerializeField] private bool autoStart = true;

    public ArenaSandbox Sandbox => sandbox;
    public float RoundSeconds => roundSeconds;
    public float Elapsed { get; private set; }
    public float Remaining => Mathf.Max(0f, roundSeconds - Elapsed);
    public bool IsRunning { get; private set; }
    public bool IsOver { get; private set; }
    public int PlayerSecured => IsRunning ? SumSecured(ExpeditionTeam.Player) : frozenPlayerSecured;
    public int RivalSecured => IsRunning ? SumSecured(ExpeditionTeam.Rival) : frozenRivalSecured;
    public ExpeditionTeam Winner { get; private set; } = ExpeditionTeam.None;
    public IReadOnlyList<ArenaRoundStat> Summary => summary;

    private int frozenPlayerSecured;
    private int frozenRivalSecured;
    private readonly List<ArenaRoundStat> summary = new();

    private void Start()
    {
        if (autoStart) Begin();
    }

    private void Update()
    {
        if (!IsRunning) return;

        Elapsed += Time.deltaTime;
        if (Elapsed >= roundSeconds) End();
    }

    public void Begin()
    {
        Elapsed = 0f;
        IsRunning = true;
        IsOver = false;
        Winner = ExpeditionTeam.None;
        summary.Clear();
    }

    public void Launch()
    {
        sandbox.SpawnCast();
        Begin();
    }

    public void Reset(bool newSeed)
    {
        sandbox.ResetRoom(newSeed);
        Elapsed = 0f;
        IsRunning = false;
        IsOver = false;
        Winner = ExpeditionTeam.None;
        frozenPlayerSecured = 0;
        frozenRivalSecured = 0;
    }

    public void End()
    {
        frozenPlayerSecured = SumSecured(ExpeditionTeam.Player);
        frozenRivalSecured = SumSecured(ExpeditionTeam.Rival);

        IsRunning = false;
        IsOver = true;

        Winner = frozenPlayerSecured == frozenRivalSecured
            ? ExpeditionTeam.None
            : (frozenPlayerSecured > frozenRivalSecured ? ExpeditionTeam.Player : ExpeditionTeam.Rival);

        summary.Clear();
        summary.AddRange(ArenaRoundSummary.Capture(sandbox.Spawned));

        Debug.Log($"[ArenaRound] fin: Player {frozenPlayerSecured} - Rival {frozenRivalSecured} → {Winner}");
    }

    [Button] public void Restart()
    {
        Reset(false);
        Launch();
    }

    private int SumSecured(ExpeditionTeam team)
    {
        int total = 0;
        IReadOnlyList<ExitZone> exits = sandbox.Exits;
        for (int i = 0; i < exits.Count; i++)
            if (exits[i].Team == team) total += exits[i].Secured;
        return total;
    }
}
}
