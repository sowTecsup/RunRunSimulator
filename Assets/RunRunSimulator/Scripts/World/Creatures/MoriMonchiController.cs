using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

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
        CreatureDNA        dna,
        RoleWorldProfileSO profileTable,
        Transform          player,
        PartVisualBankSO   bank,
        FurTypeDatabaseSO  furDb)
    {
        agent.Initialize(dna, profileTable, player);

        visualizer.SetFurDatabase(furDb);

        if (bank == null) return;

        visualizer.Assemble(dna, bank);
    }

    public void Rebind(CreatureDNA dna, RoleWorldProfileSO profileTable, FurTypeDatabaseSO furDb)
    {
        agent.Rebind(dna, profileTable);
        visualizer.SetFurDatabase(furDb);
        visualizer.RefreshFur(dna);
    }

    // ── Spawner passthrough ───────────────────────────────────────

    public void Launch(Vector3 launchPos, Vector3 launchVelocity) => agent.Launch(launchPos, launchVelocity);

    public void PrepareForPool() => agent.PrepareForPool();
}
}
