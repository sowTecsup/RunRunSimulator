using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.CloudCode;
using UnityEngine;
namespace MoriMonchiSimulator
{

public static class CloudEndpoint
{
    public static Task<string> CallAsync(string endpoint, Dictionary<string, object> payload) =>
        CloudCodeService.Instance.CallEndpointAsync<string>(endpoint, payload);

    public static async Task<T> CallAsync<T>(string endpoint, Dictionary<string, object> payload) =>
        JsonConvert.DeserializeObject<T>(await CloudCodeService.Instance.CallEndpointAsync<string>(endpoint, payload));

    public static async Task<bool> Guarded(string statusOp, string logOp, Func<Task> op, Action<string> setStatus)
    {
        try
        {
            await op();
            return true;
        }
        catch (Exception e)
        {
            setStatus($"{statusOp} error: {e.Message}");
            Debug.LogError($"[CloudSync] {logOp} failed: {e}");
            return false;
        }
    }
}
}
