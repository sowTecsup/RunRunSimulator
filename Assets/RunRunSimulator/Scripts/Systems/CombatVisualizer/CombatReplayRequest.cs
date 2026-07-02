using UnityEngine;
namespace MoriMonchiSimulator
{

public static class CombatReplayRequest
{
    public const string CombatSceneName = "CombatVisualizerMM";

    public static bool   Pending    { get; private set; }
    public static string SelfId     { get; private set; }
    public static int    FightIndex { get; private set; } = -1;

    public static CreatureDNA ResolveOpponent(CombatRecord rec, CreatureDNA self, CreatureRegistrySO registry)
    {
        if (registry == null || rec == null) return null;
        if (!string.IsNullOrEmpty(rec.OpponentDnaId) && registry.TryGet(rec.OpponentDnaId, out var byId) && byId != null && byId != self)
            return byId;
        foreach (var dna in registry.GetAll().Values)
            if (dna != null && dna != self && dna.CustomName == rec.OpponentName) return dna;
        return null;
    }

    public static bool CanReplay(CreatureDNA self, CombatRecord rec, CreatureRegistrySO registry) =>
        self != null && rec != null && rec.Turns != null && rec.Turns.Count > 0 &&
        ResolveOpponent(rec, self, registry) != null;

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
