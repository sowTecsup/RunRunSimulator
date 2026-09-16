using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class ExpeditionBridge : MonoBehaviour
{
    [SerializeField] private CloudSyncService cloudSync;
    [SerializeField, Min(1f)] private float syncTimeoutSeconds = 20f;
    [SerializeField, Min(0)] private int energyPerTrip = 20;
    [SerializeField, Min(0)] private int energyPerKnock = 5;
    [SerializeField, Min(0)] private int maxEnergyPerTrip = 40;

    public static event Action<IReadOnlyList<string>> OnDepartureRequested;
    public static void RequestDeparture(IReadOnlyList<string> ids) => OnDepartureRequested?.Invoke(ids);

    private void OnEnable()
    {
        OnDepartureRequested += Depart;
    }

    private void OnDisable()
    {
        OnDepartureRequested -= Depart;
    }

    private void Start()
    {
        if (ExpeditionHandoff.HasResult) StartCoroutine(ApplyResult());
    }

    [Button("Salir de expedición"), EnableIf("@UnityEngine.Application.isPlaying")]
    public void Depart()
    {
        Depart(null);
    }

    public void Depart(IReadOnlyList<string> ids)
    {
        if (GameManager.Instance != null) GameManager.Instance.FlushToCloud();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ExpeditionHandoff.GoToArena(ids);
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

        int energySpent = 0;
        int creaturesSpent = 0;
        var registry = GameManager.Instance != null ? GameManager.Instance.Registry : null;
        if (registry != null && result.Stats != null)
        {
            foreach (var stat in result.Stats)
            {
                if (stat.Team != ExpeditionTeam.Player || string.IsNullOrEmpty(stat.Id)) continue;
                if (!registry.TryGet(stat.Id, out var dna) || dna == null || dna.IsDead) continue;

                int cost = Mathf.Min(maxEnergyPerTrip, energyPerTrip + energyPerKnock * stat.TimesKnocked);
                dna.Needs.SpendEnergy(cost);
                energySpent += cost;
                creaturesSpent++;
            }
            if (creaturesSpent > 0) GameEvents.RegistryChanged(registry);
        }

        GameEvents.ExpeditionReturned(new ExpeditionReturn
        {
            Seed = result.Seed,
            Winner = result.Winner,
            PlayerSecured = result.PlayerSecured,
            RivalSecured = result.RivalSecured,
            MaterialGained = result.PlayerSecured,
            EnergySpent = energySpent,
            Creatures = creaturesSpent
        });

        Debug.Log($"[ExpeditionBridge] sala {result.Seed}: {result.PlayerSecured}-{result.RivalSecured} {result.Winner} → +{result.PlayerSecured} material, -{energySpent} energía ({creaturesSpent} MoriMonchis)");
    }
}
}
