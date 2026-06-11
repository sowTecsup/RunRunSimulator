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

    // Full initialization: wires behavior + assembles 3D model + applies personality tint.
    // bank may be null (no visuals assembled) — agent still initializes normally.
    public void Initialize(
        CreatureDNA          dna,
        PersonalityProfileSO profileTable,
        Transform            player,
        PartVisualBankSO     bank)
    {
        agent.Initialize(dna, profileTable, player);

        if (bank == null) return;

        visualizer.Assemble(dna, bank);

        var profile = (profileTable ?? PersonalityProfileSO.Current)?.GetProfile(dna.Personality);
        visualizer.ApplyPersonalityTint(profile?.Tint ?? Color.white);
    }

    // ── Spawner passthrough ───────────────────────────────────────

    public void Launch(Vector3 impulse) => agent.Launch(impulse);

    public void PrepareForLaunch(Vector3 launchPos, Vector3 impulse) => agent.PrepareForLaunch(launchPos, impulse);

    public void PrepareForPool() => agent.PrepareForPool();
}
