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
    [SerializeField, Min(0f)] private float departFlushTimeout = 5f;
    [SerializeField] private bool permadeathEnabled = false;

    private bool departing;

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
        if (departing) return;
        departing = true;
        StartCoroutine(DepartRoutine(ids));
    }

    private IEnumerator DepartRoutine(IReadOnlyList<string> ids)
    {
        if (GameManager.Instance != null)
        {
            var task = GameManager.Instance.FlushToCloudAsync();
            float elapsed = 0f;
            while (!task.IsCompleted && elapsed < departFlushTimeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ExpeditionHandoff.GoToArena(ids);
        departing = false;
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

        int material = result.Lost ? 0 : result.PlayerSecured;
        if (material > 0) Wallet.Add(Currency.Minerita, material, "expedition");

        var registry = GameManager.Instance != null ? GameManager.Instance.Registry : null;
        bool touched = false;

        if (permadeathEnabled && registry != null)
        {
            if (result.FallenIds != null)
            {
                foreach (var id in result.FallenIds)
                {
                    if (!registry.TryGet(id, out var dna) || dna == null || dna.IsDead) continue;
                    CreatureLifecycle.Kill(dna);
                    touched = true;
                }
            }

            if (result.Lost && result.TeamIds != null)
            {
                foreach (var id in result.TeamIds)
                {
                    if (!registry.TryGet(id, out var dna) || dna == null || dna.IsDead) continue;
                    CreatureLifecycle.Kill(dna);
                    touched = true;
                }
            }
        }

        if (touched) GameEvents.RegistryChanged(registry);

        GameEvents.ExpeditionReturned(new ExpeditionReturn
        {
            Seed = result.Seed,
            Winner = result.Winner,
            PlayerSecured = material,
            RivalSecured = result.RivalSecured,
            MineritaGained = material,
            Fallen = result.Fallen,
            Floors = result.Floors,
            Lost = result.Lost
        });

        Debug.Log($"[ExpeditionBridge] run {result.Seed}: {result.Floors} pisos, perdida={result.Lost} → +{material} material, {result.Fallen} caídas");
    }
}
}
