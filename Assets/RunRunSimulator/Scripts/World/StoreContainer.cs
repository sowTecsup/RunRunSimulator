using Sirenix.OdinInspector;
using UnityEngine;

public class StoreContainer : MoriMochiContainer
{
    [SerializeField, Min(0f)]
    [Title("Store Display")]
    private float restoreRate = 25f;

    private void Update()
    {
        float delta = restoreRate * Time.deltaTime;
        for (int i = 0; i < Occupants.Count; i++)
        {
            var dna = Occupants[i]?.DNA;
            if (dna == null) continue;
            dna.Needs.AddHealth(delta);
            dna.Needs.AddEnergy(delta);
            dna.Needs.AddAffect(delta);
        }
    }
}
