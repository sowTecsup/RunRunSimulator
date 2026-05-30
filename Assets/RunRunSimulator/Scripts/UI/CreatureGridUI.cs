using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// In-game visual grid of MoriMonchis — lives on the Canvas, mirrors the
// read-only data that CreatureGridView shows in the inspector.
//
// Event-driven, NO direct references: the registry always arrives inside the
// event payload, so this script never touches GameManager or CreatureRegistrySO
// statics. For each creature it clones a CreatureVisualUI card under a parent
// that carries a GridLayoutGroup — Unity lays the cards out, we only spawn.
//
// When does it first populate? On login, CloudSyncService fires
// OnRegistryReloaded with the freshly loaded registry → the grid builds itself.
// Afterwards, every gameplay mutation fires OnRegistryChanged → the grid rebuilds.
public class CreatureGridUI : MonoBehaviour
{
    [Header("Spawn setup")]

    // The card prefab (CreatureVisualUI) cloned once per MoriMochi.
    [SerializeField] private CreatureVisualUI cardPrefab;

    // Parent transform the cards are spawned under. Put the GridLayoutGroup
    // on THIS object so the layout is automatic — this script only instantiates
    // and binds, it never positions cards by hand.
    [SerializeField] private Transform gridContainer;

    // Live cards spawned in the last rebuild, kept so we can destroy them
    // before re-spawning. Avoids leaking duplicates on every registry change.
    private readonly List<CreatureVisualUI> spawned = new List<CreatureVisualUI>();

    // ── Lifecycle ─────────────────────────────────────────────────

    private void OnEnable()
    {
        GameEvents.OnRegistryChanged  += Rebuild;
        GameEvents.OnRegistryReloaded += Rebuild;
    }

    private void OnDisable()
    {
        GameEvents.OnRegistryChanged  -= Rebuild;
        GameEvents.OnRegistryReloaded -= Rebuild;
    }

    // ── Private Methods ───────────────────────────────────────────

    // Wipes the grid and re-spawns one card per creature straight from the
    // payload — no lookups. Newest MoriMonchis first, matching the inspector grid.
    private void Rebuild(CreatureRegistrySO registry)
    {
        Clear();
        if (registry == null || cardPrefab == null || gridContainer == null) return;

        foreach (var dna in registry.GetAll().Values.OrderByDescending(d => d.BirthDate))
        {
            var card = Instantiate(cardPrefab, gridContainer);
            card.Bind(dna);
            spawned.Add(card);
        }
    }

    private void Clear()
    {
        foreach (var card in spawned)
            if (card != null) Destroy(card.gameObject);
        spawned.Clear();
    }
}
