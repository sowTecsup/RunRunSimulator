using UnityEngine;
namespace MoriMonchiSimulator
{

public static class CombatReplayRequest
{
    public const string CombatSceneName = "CombatVisualizerMM";

    public static bool   Pending    { get; private set; }
    public static string SelfId     { get; private set; }
    public static int    FightIndex { get; private set; } = -1;

    public static bool CanReplay(CreatureDNA self, CombatRecord rec, CreatureRegistrySO registry)
    {
        if (self == null || rec == null || registry == null) return false;
        if (rec.Turns == null || rec.Turns.Count == 0) return false;
        if (rec.SelfTeam == null || rec.SelfTeamIds == null || rec.OpponentTeamIds == null) return false;
        foreach (var id in rec.SelfTeamIds)
            if (!registry.TryGet(id, out _)) return false;
        foreach (var id in rec.OpponentTeamIds)
            if (!registry.TryGet(id, out _)) return false;
        return true;
    }

    public static void Request(CreatureDNA self, CombatRecord rec)
    {
        SelfId     = self.UniqueID;
        FightIndex = self.CombatHistory.IndexOf(rec);
        Pending    = true;

        if (FightIndex < 0)
        {
            Debug.LogWarning("[CombatReplayRequest] El CombatRecord solicitado no está en self.CombatHistory.");
            Pending = false;
            return;
        }

        GameManager.Instance?.FlushForSceneChange();
        UnityEngine.SceneManagement.SceneManager.LoadScene(CombatSceneName);
    }

    public static void Clear()
    {
        Pending    = false;
        SelfId     = null;
        FightIndex = -1;
    }
}
}
