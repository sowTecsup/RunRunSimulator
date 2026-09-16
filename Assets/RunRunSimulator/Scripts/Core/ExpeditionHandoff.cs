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
    public List<ArenaRoundStat> Stats;
}

public static class ExpeditionHandoff
{
    public const string StoreScene = "GameScene";
    public const string ArenaScene = "ArenaSandbox";

    public static bool CameFromStore { get; private set; }
    public static bool HasResult { get; private set; }
    public static ExpeditionResult Result { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        CameFromStore = false;
        HasResult = false;
        Result = default;
    }

    public static void GoToArena()
    {
        CameFromStore = true;
        HasResult = false;
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
        Time.timeScale = 1f;
        SceneManager.LoadScene(StoreScene);
    }

    public static bool TryConsumeResult(out ExpeditionResult result)
    {
        result = Result;
        bool had = HasResult;
        HasResult = false;
        CameFromStore = false;
        return had;
    }
}
}
