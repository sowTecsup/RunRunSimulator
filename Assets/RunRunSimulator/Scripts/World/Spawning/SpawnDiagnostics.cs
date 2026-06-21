using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class SpawnDiagnostics : MonoBehaviour
{
    [Tooltip("Registra cada evento de registro/cría/rebake en la consola con frame + tiempo.")]
    [SerializeField] private bool logToConsole = true;

    [Tooltip("Líneas máximas que conserva el historial del inspector.")]
    [SerializeField, Min(8)] private int historySize = 40;

    [ShowInInspector, ReadOnly, BoxGroup("Contadores")] private int registryChanged;
    [ShowInInspector, ReadOnly, BoxGroup("Contadores")] private int registryReloaded;
    [ShowInInspector, ReadOnly, BoxGroup("Contadores")] private int breedingCompleted;
    [ShowInInspector, ReadOnly, BoxGroup("Contadores")] private int navRebakes;

    [ShowInInspector, ReadOnly, BoxGroup("Historial")]
    private readonly List<string> history = new List<string>();

    private void OnEnable()
    {
        GameEvents.OnRegistryChanged   += OnChanged;
        GameEvents.OnRegistryReloaded  += OnReloaded;
        GameEvents.OnBreedingCompleted += OnBred;
        GameEvents.OnNavMeshWillRebake += OnWillRebake;
        GameEvents.OnNavMeshRebaked    += OnRebaked;
    }

    private void OnDisable()
    {
        GameEvents.OnRegistryChanged   -= OnChanged;
        GameEvents.OnRegistryReloaded  -= OnReloaded;
        GameEvents.OnBreedingCompleted -= OnBred;
        GameEvents.OnNavMeshWillRebake -= OnWillRebake;
        GameEvents.OnNavMeshRebaked    -= OnRebaked;
    }

    private void OnChanged(CreatureRegistrySO r)
    {
        registryChanged++;
        Record($"RegistryChanged ({r?.Count ?? 0}) → Sync quirúrgico (solo encola lo nuevo)");
    }

    private void OnReloaded(CreatureRegistrySO r)
    {
        registryReloaded++;
        Record($"⚠ RegistryReloaded ({r?.Count ?? 0}) → re-vincula TODOS los agentes spawneados", true);
    }

    private void OnBred(CreatureDNA mother, CreatureDNA father, CreatureDNA child)
    {
        breedingCompleted++;
        Record($"BreedingCompleted → cría '{child?.CustomName}'");
    }

    private void OnWillRebake()
    {
        navRebakes++;
        Record("NavMeshWillRebake → agentes pasan a física");
    }

    private void OnRebaked() => Record("NavMeshRebaked → agentes pueden reanclarse");

    private void Record(string message, bool warn = false)
    {
        string line = $"[f{Time.frameCount} | {Time.time:0.00}s] {message}";
        history.Add(line);
        while (history.Count > historySize) history.RemoveAt(0);
        if (!logToConsole) return;
        if (warn) Debug.LogWarning($"[SpawnDiag] {line}");
        else      Debug.Log($"[SpawnDiag] {line}");
    }

    [Button("Limpiar historial")]
    private void ClearHistory()
    {
        history.Clear();
        registryChanged = registryReloaded = breedingCompleted = navRebakes = 0;
    }
}
}
