using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

// One spawned combat-visual unit — the instantiated model plus the record/live-state
// refs the visualizer touches every frame (bar, hooks, animator) and the anchor it
// was placed on. Plain data, no behaviour.
public class CombatVisualUnit
{
    public CombatVisualSide Side;
    public int Index;
    public CreatureDNA Dna;
    public CombatFighterSnapshot Snapshot;
    public float MaxHp;
    public float ShownHp;
    public MoriMonchiVisualizer Instance;
    public MoriMonchiCombatVisualizer Hooks;
    public MoriMonchiCombatVisualizerUITK Bar;
    public MoriMonchiProceduralAnimator Anim;
    public Transform Anchor;
    public Unity.Cinemachine.CinemachineCamera VCam;
}

// Owns spawn/lookup/lifecycle of the up-to-3-per-side combat visual units. Collaborator
// of CombatVisualizerService (composition, decision S32/regla 11) — the service stays
// the thin orchestrator driving playback; this class only knows how to place, find and
// tear down the instantiated fighters.
public class CombatVisualUnits
{
    private readonly List<CombatVisualUnit> teamA = new List<CombatVisualUnit>();
    private readonly List<CombatVisualUnit> teamB = new List<CombatVisualUnit>();

    private static readonly string[] FrontNames = { "Front0", "Front1" };
    private static readonly string[] MidNames   = { "Mid0", "Mid1", "Mid2" };
    private static readonly string[] BackNames  = { "Back0", "Back1" };

    public IReadOnlyList<CombatVisualUnit> Team(CombatVisualSide side) => side == CombatVisualSide.A ? teamA : teamB;

    public CombatVisualUnit Get(CombatVisualSide side, int index)
    {
        var team = Team(side);
        if (index < 0 || index >= team.Count) return null;
        return team[index];
    }

    public void Spawn(CombatVisualSide side, List<CreatureDNA> dnas, List<CombatFighterSnapshot> snapshots, Transform board, MoriMonchiVisualizer prefab, ElementTableSO elements)
    {
        var team = side == CombatVisualSide.A ? teamA : teamB;
        team.Clear();
        if (dnas == null || snapshots == null || board == null || prefab == null) return;

        int frontUsed = 0, midUsed = 0, backUsed = 0;
        for (int i = 0; i < dnas.Count; i++)
        {
            var snapshot = snapshots[i];
            var anchor = ResolveAnchor(board, snapshot.Row, ref frontUsed, ref midUsed, ref backUsed);

            var inst = Object.Instantiate(prefab, anchor.position, anchor.rotation, anchor);
            inst.SetFurDatabase(GameManager.Instance.FurTypeDatabase);
            inst.Assemble(dnas[i], GameManager.Instance.PartVisualBank);

            var unit = new CombatVisualUnit
            {
                Side     = side,
                Index    = i,
                Dna      = dnas[i],
                Snapshot = snapshot,
                MaxHp    = snapshot.MaxHp,
                ShownHp  = snapshot.MaxHp,
                Instance = inst,
                Hooks    = inst as MoriMonchiCombatVisualizer,
                Bar      = inst.GetComponentInChildren<MoriMonchiCombatVisualizerUITK>(true),
                Anim     = inst.GetComponent<MoriMonchiProceduralAnimator>(),
                Anchor   = anchor,
            };
            var identity = elements != null ? elements.GetIdentity(dnas[i].Element) : null;
            unit.Bar?.Bind(snapshot.Name, snapshot.Attack, snapshot.Speed, snapshot.Role,
                identity != null ? identity.DisplayName : dnas[i].Element.ToString(),
                identity != null ? identity.UiColor : Color.white);
            team.Add(unit);

            var vcamGo = new GameObject($"VCam_{side}{i}");
            vcamGo.transform.SetParent(inst.transform, false);
            vcamGo.transform.localPosition = new Vector3(0f, 1.5f, -2.6f);
            var vcam = vcamGo.AddComponent<Unity.Cinemachine.CinemachineCamera>();
            vcam.Priority = 0;
            vcam.LookAt = inst.transform;
            vcamGo.AddComponent<Unity.Cinemachine.CinemachineRotationComposer>();
            unit.VCam = vcam;
        }
    }

    private static Transform ResolveAnchor(Transform board, int row, ref int frontUsed, ref int midUsed, ref int backUsed)
    {
        string[] names;
        int used;
        switch (row)
        {
            case 0: names = FrontNames; used = frontUsed; break;
            case 2: names = BackNames;  used = backUsed;  break;
            default: names = MidNames;  used = midUsed;   break;
        }

        Transform anchor = used < names.Length ? board.Find(names[used]) : null;
        if (row == 0) frontUsed++; else if (row == 2) backUsed++; else midUsed++;

        if (anchor == null)
        {
            Debug.LogWarning($"[CombatVisualUnits] No free anchor for row {row} slot {used} under \"{board.name}\" — falling back to board transform.");
            return board;
        }
        return anchor;
    }

    public void DespawnAll()
    {
        DespawnTeam(teamA);
        DespawnTeam(teamB);
    }

    private static void DespawnTeam(List<CombatVisualUnit> team)
    {
        foreach (var unit in team)
            if (unit?.Instance != null) Object.Destroy(unit.Instance.gameObject);
        team.Clear();
    }

    public void SetActive(CombatVisualUnit unit, bool active)
    {
        if (unit?.Instance == null) return;
        if (unit.Instance.gameObject.activeSelf != active) unit.Instance.gameObject.SetActive(active);
    }

    public Transform TransformOf(CombatVisualSide side, int index)
    {
        var unit = Get(side, index);
        if (unit == null) return null;
        if (unit.Instance != null) return unit.Instance.transform;
        return unit.Anchor;
    }

    public Vector3 PosOf(CombatVisualSide side, int index)
    {
        var unit = Get(side, index);
        if (unit == null) return Vector3.zero;
        if (unit.Instance != null) return unit.Instance.transform.position;
        if (unit.Anchor != null) return unit.Anchor.position;
        return Vector3.zero;
    }
}
}
