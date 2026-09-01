using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class CreatureGridUI : MonoBehaviour
{
    [Header("Spawn setup")]

    [SerializeField] private CreatureVisualUI cardPrefab;

    [SerializeField] private Transform gridContainer;

    private readonly List<CreatureVisualUI> spawned = new List<CreatureVisualUI>();

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
}
