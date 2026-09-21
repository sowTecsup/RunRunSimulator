using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace MoriMonchiSimulator
{

public struct ExpeditionResult
{
    public int Seed;
    public ExpeditionTeam Winner;
    public int PlayerSecured;
    public int RivalSecured;
    public int Floors;
    public bool Lost;
    public List<string> FallenIds;
    public int Fallen;
    public List<string> TeamIds;
}

public struct ExpeditionReturn
{
    public int Seed;
    public ExpeditionTeam Winner;
    public int PlayerSecured;
    public int RivalSecured;
    public int MineritaGained;
    public int Fallen;
    public int Floors;
    public bool Lost;
}

public static class ExpeditionHandoff
{
    public const string StoreScene = "GameScene";
    public const string ArenaScene = "ArenaSandbox";

    public static bool CameFromStore { get; private set; }
    public static bool HasResult { get; private set; }
    public static ExpeditionResult Result { get; private set; }
    public static int RunSeed { get; private set; }

    private static readonly List<string> selectedIds = new();
    public static IReadOnlyList<string> SelectedIds => selectedIds;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        CameFromStore = false;
        HasResult = false;
        Result = default;
        RunSeed = 0;
        selectedIds.Clear();
    }

    public static void GoToArena(IReadOnlyList<string> ids = null)
    {
        selectedIds.Clear();
        if (ids != null) selectedIds.AddRange(ids);
        CameFromStore = true;
        HasResult = false;
        RunSeed = System.Environment.TickCount & 0x7fffffff;
        SceneManager.LoadScene(ArenaScene);
    }

    public static void ReturnToStore(ExpeditionResult? result)
    {
        if (result.HasValue)
        {
            Result = result.Value;
            HasResult = true;
        }
        else
        {
            CameFromStore = false;
        }
        selectedIds.Clear();
        Time.timeScale = 1f;
        SceneManager.LoadScene(StoreScene);
    }

    public static bool TryConsumeResult(out ExpeditionResult result)
    {
        result = Result;
        bool had = HasResult;
        HasResult = false;
        CameFromStore = false;
        selectedIds.Clear();
        return had;
    }
}
}
