using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

// Facade that wires MoriMochiAgent (behavior brain) and MonchiVisualizer (3D assembly)
// without either knowing about the other. MoriMochiSpawner talks only to this component.
//
// All three components live on the same root GameObject; the serialized refs are set once
// in the prefab and never searched at runtime (no GetComponentInChildren).
[RequireComponent(typeof(MoriMochiAgent))]
[RequireComponent(typeof(MonchiVisualizer))]
public class MoriMonchiController : MonoBehaviour
{
    [Required, SerializeField] private MoriMochiAgent  agent;
    [Required, SerializeField] private MonchiVisualizer visualizer;

    public CreatureDNA DNA => agent.DNA;

    public MoriMochiAgent Agent => agent;

    public MonchiVisualizer Visualizer => visualizer;

    public void Initialize(
        CreatureDNA         dna,
        RoleWorldProfileSO  profileTable,
        Transform           player,
        MonchiVisualBankSO  bank,
        FurTypeDatabaseSO   furDb)
    {
        agent.Initialize(dna, profileTable, player);

        visualizer.SetFurDatabase(furDb);

        if (bank == null)
        {
            visualizer.RefreshLook(dna);
            return;
        }

        visualizer.SetBank(bank);
        visualizer.Assemble(dna);
    }

    public void Rebind(CreatureDNA dna, RoleWorldProfileSO profileTable, FurTypeDatabaseSO furDb)
    {
        agent.Rebind(dna, profileTable);
        visualizer.SetFurDatabase(furDb);

        visualizer.RefreshLook(dna);
    }

    // ── Spawner passthrough ───────────────────────────────────────

    public void Launch(Vector3 launchPos, Vector3 launchVelocity) => agent.Launch(launchPos, launchVelocity);

    public void PrepareForPool() => agent.PrepareForPool();
}
}
