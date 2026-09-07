using UnityEngine;
namespace MoriMonchiSimulator
{

internal interface IExpeditionTask
{
    bool Tick(ExpeditionRulesSO rules);
    void Cancel();
    void ResetForReuse();
    CreatureIntent Intent { get; }
    Transform TargetTransform { get; }
}
}
