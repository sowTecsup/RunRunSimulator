using System;
using UnityEngine;
namespace MoriMonchiSimulator
{

[Serializable]
public class NeedsState
{
    public float Health = 100f;
    public float Energy = 100f;
    public float Affect = 0f;

    public void AddHealth(float delta) => Health = Mathf.Clamp(Health + delta, 0f, 100f);
    public void AddEnergy(float delta) => Energy = Mathf.Clamp(Energy + delta, 0f, 100f);
    public void AddAffect(float delta) => Affect = Mathf.Clamp(Affect + delta, -100f, 100f);

    public void SpendEnergy(float amount) => AddEnergy(-Mathf.Abs(amount));

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
}
