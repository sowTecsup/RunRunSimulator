using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MoriMonchiSimulator
{
[CreateAssetMenu(fileName = "CustomerArchetypeDatabase", menuName = "RunRunSimulator/Customers/Customer Archetype Database")]
public class CustomerArchetypeDatabaseSO : SerializedScriptableObject
{
    [Title("Archetypes")]
    public List<CustomerArchetypeSO> Archetypes = new();

    public CustomerArchetypeSO RandomArchetype()
    {
        if (Archetypes == null || Archetypes.Count == 0)
        {
            Debug.LogWarning("[CustomerArchetypeDatabaseSO] No archetypes configured.");
            return null;
        }
        return Archetypes[Random.Range(0, Archetypes.Count)];
    }
}
}
