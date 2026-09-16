using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class ExpeditionBridge : MonoBehaviour
{
    [SerializeField] private CloudSyncService cloudSync;
    [SerializeField, Min(1f)] private float syncTimeoutSeconds = 20f;

    private void Start()
    {
        if (ExpeditionHandoff.HasResult) StartCoroutine(ApplyResult());
    }

    [Button("Salir de expedición"), EnableIf("@UnityEngine.Application.isPlaying")]
    public void Depart()
    {
        if (GameManager.Instance != null) GameManager.Instance.FlushToCloud();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ExpeditionHandoff.GoToArena();
    }

    private IEnumerator ApplyResult()
    {
        float elapsed = 0f;
        while (cloudSync != null && !cloudSync.StartupSyncDone && elapsed < syncTimeoutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!ExpeditionHandoff.TryConsumeResult(out ExpeditionResult result)) yield break;

        var inventory = GameManager.CurrentInventory;
        if (inventory != null && result.PlayerSecured > 0)
        {
            inventory.AddAdventureMaterial(result.PlayerSecured);
            GameEvents.InventoryChanged(inventory);
        }

        Debug.Log($"[ExpeditionBridge] sala {result.Seed}: {result.PlayerSecured}-{result.RivalSecured} {result.Winner} → +{result.PlayerSecured} material");
    }
}
}
