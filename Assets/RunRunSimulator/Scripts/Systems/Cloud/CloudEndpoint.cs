using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.CloudCode;
namespace MoriMonchiSimulator
{

// Single entry point for Cloud Code module calls: call + JSON deserialize. The endpoint→reconciliation
// flow stays per-service (each reconciles its own registry differently); only the call+parse is shared.
public static class CloudEndpoint
{
    public static Task<string> CallAsync(string endpoint, Dictionary<string, object> payload) =>
        CloudCodeService.Instance.CallEndpointAsync<string>(endpoint, payload);

    public static async Task<T> CallAsync<T>(string endpoint, Dictionary<string, object> payload) =>
        JsonConvert.DeserializeObject<T>(await CloudCodeService.Instance.CallEndpointAsync<string>(endpoint, payload));
}
}
