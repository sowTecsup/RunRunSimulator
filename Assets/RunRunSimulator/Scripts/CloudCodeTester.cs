using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using UnityEngine;

// Diagnostic tool for Cloud Code endpoints. Attach to any GameObject; the
// component reuses whatever Unity Services session CloudSyncService established.
public class CloudCodeTester : MonoBehaviour
{
    [ShowInInspector, ReadOnly, BoxGroup("Last Response"), TextArea(5, 20)]
    private string lastResponse = "---";

    [Button("Test: random number (1-4)", ButtonSizes.Large), GUIColor(0.5f, 0.85f, 1f)]
    public async void TestRandom()
    {
        if (!RequireSignedIn()) return;
        try
        {
            lastResponse = "Calling test-random...";
            var raw = await CloudCodeService.Instance.CallEndpointAsync<string>(
                "test-random", new Dictionary<string, object>());
            lastResponse = $"OK → {raw}";
            Debug.Log($"[CloudCodeTester] test-random returned: {raw}");
        }
        catch (Exception e)
        {
            lastResponse = $"ERROR → {e.Message}";
            Debug.LogError($"[CloudCodeTester] test-random failed: {e}");
        }
    }

    [Button("Force Matchmaking Tick (DEV)", ButtonSizes.Large), GUIColor(0.4f, 1f, 0.5f)]
    public async void ForceMatchmakingTick()
    {
        if (!RequireSignedIn()) return;
        try
        {
            lastResponse = "Running process-matchmaking...";
            var raw    = await CloudCodeService.Instance.CallEndpointAsync<string>(
                "process-matchmaking", new Dictionary<string, object>());
            var parsed = JsonConvert.DeserializeObject<MatchmakingResponse>(raw);
            lastResponse = $"Tick OK\nMatched: {parsed?.matched}\nRemaining: {parsed?.remaining}\nDropped: {parsed?.dropped}";
            Debug.Log($"[CloudCodeTester] process-matchmaking: {raw}");
        }
        catch (Exception e)
        {
            lastResponse = $"ERROR → {e.Message}";
            Debug.LogError($"[CloudCodeTester] process-matchmaking failed: {e}");
        }
    }

    [Button("Test: Custom Data write/read", ButtonSizes.Large), GUIColor(1f, 0.7f, 0.3f)]
    public async void TestCustomData()
    {
        if (!RequireSignedIn()) return;
        try
        {
            lastResponse = "Calling test-customdata...";
            var raw = await CloudCodeService.Instance.CallEndpointAsync<string>(
                "test-customdata", new Dictionary<string, object>());

            var parsed = JsonConvert.DeserializeObject<TestResponse>(raw);
            lastResponse = string.Join("\n", parsed?.log ?? new List<string> { raw });
            Debug.Log($"[CloudCodeTester] test-customdata log:\n{lastResponse}");
        }
        catch (Exception e)
        {
            lastResponse = $"ERROR → {e.Message}";
            Debug.LogError($"[CloudCodeTester] test-customdata failed: {e}");
        }
    }

    private bool RequireSignedIn()
    {
        if (AuthenticationService.Instance.IsSignedIn) return true;
        lastResponse = "Not signed in — sign in first with CloudSyncService.";
        Debug.LogError("[CloudCodeTester] Not signed in.");
        return false;
    }

    [Serializable]
    private class TestResponse
    {
        public List<string> log;
    }

    [Serializable]
    private class MatchmakingResponse
    {
        public int matched;
        public int remaining;
        public int dropped;
    }
}
