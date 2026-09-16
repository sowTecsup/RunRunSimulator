using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

[DefaultExecutionOrder(-50)]
public class ArenaRunDirector : MonoBehaviour
{
    [Required, SerializeField] private ArenaSandbox sandbox;
    [Required, SerializeField] private ArenaRound round;

    private ArenaRun run;
    private readonly Dictionary<string, string> names = new();

    public bool Active => run != null;
    public ArenaRun Run => run;
    public ArenaFloorKind CurrentKind => run != null ? run.CurrentKind : ArenaFloorKind.Enemies;
    public bool FloorRecorded { get; private set; }
    public IReadOnlyDictionary<string, string> TeamNames => names;

    public event Action FloorEnded;

    private void Awake()
    {
        if (!ExpeditionHandoff.CameFromStore) return;
        run = new ArenaRun(ExpeditionHandoff.RunSeed, ExpeditionHandoff.SelectedIds);
        run.EnterFloor();
    }

    private void Start()
    {
        if (!Active) return;
        var pool = sandbox.LocalPool;
        if (pool == null) return;
        foreach (var dna in pool)
        {
            if (dna == null || !run.TeamIds.Contains(dna.UniqueID)) continue;
            run.SetStartHealth(dna.UniqueID, dna.Needs.Health);
            names[dna.UniqueID] = dna.CustomName;
        }
    }

    private void Update()
    {
        if (!Active || FloorRecorded || !round.IsOver) return;
        run.RecordFloor(round.Winner, round.PlayerSecured, round.Summary);
        FloorRecorded = true;
        Debug.Log($"[ArenaRunDirector] piso {run.Floor} {run.CurrentKind}: {round.Winner} · llevás {run.Material} · perdida={run.Lost}");
        FloorEnded?.Invoke();
    }

    public void Continue()
    {
        if (!Active || run.Lost || !FloorRecorded) return;
        run.EnterFloor();
        sandbox.SetFloor(run.FloorSeed(run.Floor), run.CurrentKind);
        round.Reset(false);
        FloorRecorded = false;
        Debug.Log($"[ArenaRunDirector] piso {run.Floor} {run.CurrentKind} · semilla {run.FloorSeed(run.Floor)}");
    }

    public void Retreat()
    {
        if (!Active) return;
        ExpeditionHandoff.ReturnToStore(run.ToResult());
    }

    public void GiveUp() => Retreat();
}
}
