using System;

// Central event bus. Publishers and subscribers depend only on this class,
// never on each other — that decoupling is the whole point: gameplay logic
// announces what happened and never reaches into persistence, cloud sync or UI.
//
// Events carry their data. A subscriber receives the registry in the payload and
// works on it directly — it must NOT reach back into GameManager.Instance.Registry.
// That's what makes the bus clean: the data moves with the event, no extra lookups.
//
// IMPORTANT: every MonoBehaviour that subscribes MUST unsubscribe in OnDisable
// (or OnDestroy). A static event holds the handler — and the object behind it —
// alive forever, so a destroyed listener both leaks and throws on the next fire.
public static class GameEvents
{
    // The registry was mutated by gameplay → persist (save + cloud push) and
    // refresh any UI. Carries the registry so subscribers don't look it up.
    public static event Action<CreatureRegistrySO> OnRegistryChanged;
    public static void RegistryChanged(CreatureRegistrySO registry) => OnRegistryChanged?.Invoke(registry);

    // The registry was replaced wholesale from an authoritative external source
    // (cloud pull / reset). UI should refresh, but persistence must NOT run again
    // — the data already came from (or was cleared on) the cloud. UI-only.
    public static event Action<CreatureRegistrySO> OnRegistryReloaded;
    public static void RegistryReloaded(CreatureRegistrySO registry) => OnRegistryReloaded?.Invoke(registry);

    // ── Domain notifications ──────────────────────────────────────
    // For consumers that need "what happened", not just "something changed".
    // These do NOT persist — fire RegistryChanged() alongside them.

    public static event Action<CreatureDNA> OnCreatureMinted;
    public static void CreatureMinted(CreatureDNA creature) => OnCreatureMinted?.Invoke(creature);

    public static event Action<CombatResult> OnCombatCompleted;
    public static void CombatCompleted(CombatResult result) => OnCombatCompleted?.Invoke(result);

    // mother, father, child
    public static event Action<CreatureDNA, CreatureDNA, CreatureDNA> OnBreedingCompleted;
    public static void BreedingCompleted(CreatureDNA mother, CreatureDNA father, CreatureDNA child) =>
        OnBreedingCompleted?.Invoke(mother, father, child);
}
