using System;
using UnityEngine;

// The three mutable runtime needs of a MoriMochi. Lives INSIDE CreatureDNA (which is already the
// persisted save record) so it rides the local + Cloud Save with zero extra plumbing. NOT part of
// the genetic string — ToStringID()/FromID() ignore it.
//
// Mutated only in memory during play (the MoriMochiAgent ticks decay; NeedStations refill; breeding/
// combat spend energy). Stat changes MUST NOT fire GameEvents.RegistryChanged — that would push to
// Cloud Save every frame. They flush on quit/pause/logout via GameManager instead.
[Serializable]
public class NeedsState
{
    public float Health = 100f;   // [0,100]   hunger / wellbeing — decays passively
    public float Energy = 100f;   // [0,100]   drains while active; spent on breeding/combat
    public float Affect = 0f;     // [-100,100] +100 happy ↔ -100 stressed; drifts toward stress

    public void AddHealth(float delta) => Health = Mathf.Clamp(Health + delta, 0f, 100f);
    public void AddEnergy(float delta) => Energy = Mathf.Clamp(Energy + delta, 0f, 100f);
    public void AddAffect(float delta) => Affect = Mathf.Clamp(Affect + delta, -100f, 100f);

    // Endpoint for other systems (breeding/combat). The AMOUNT is configured by those managers.
    public void SpendEnergy(float amount) => AddEnergy(-Mathf.Abs(amount));

    // Restores the stat that matches 'need' (used by NeedStation while in use).
    public void Restore(NeedType need, float amount)
    {
        switch (need)
        {
            case NeedType.Health: AddHealth(amount); break;
            case NeedType.Energy: AddEnergy(amount); break;
            case NeedType.Affect: AddAffect(amount); break;
        }
    }

    public float Get(NeedType need) => need switch
    {
        NeedType.Health => Health,
        NeedType.Energy => Energy,
        _               => Affect,
    };
}
