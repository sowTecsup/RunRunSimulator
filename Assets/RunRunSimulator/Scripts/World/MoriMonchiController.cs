using Sirenix.OdinInspector;
using UnityEngine;

// Facade that wires MoriMochiAgent (behavior brain) and MoriMonchiVisualizer (3D assembly)
// without either knowing about the other. MoriMochiSpawner talks only to this component.
//
// All three components live on the same root GameObject; the serialized refs are set once
// in the prefab and never searched at runtime (no GetComponentInChildren).
[RequireComponent(typeof(MoriMochiAgent))]
[RequireComponent(typeof(MoriMonchiVisualizer))]
public class MoriMonchiController : MonoBehaviour
{
    [Required, SerializeField] private MoriMochiAgent       agent;
    [Required, SerializeField] private MoriMonchiVisualizer visualizer;

    public CreatureDNA DNA => agent.DNA;

    public MoriMochiAgent Agent => agent;

    public void Initialize(
        CreatureDNA          dna,
        PersonalityProfileSO profileTable,
        Transform            player,
        PartVisualBankSO     bank)
    {
        agent.Initialize(dna, profileTable, player);

        if (bank == null) return;

        visualizer.Assemble(dna, bank);
    }

    // ── Spawner passthrough ───────────────────────────────────────

    public void Launch(Vector3 launchPos, Vector3 launchVelocity) => agent.Launch(launchPos, launchVelocity);

    public void PrepareForPool() => agent.PrepareForPool();
}
