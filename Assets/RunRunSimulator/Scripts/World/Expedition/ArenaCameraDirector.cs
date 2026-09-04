using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class ArenaCameraDirector : MonoBehaviour
{
    [Required, SerializeField] private ArenaSandbox sandbox;
    [Required, SerializeField] private CinemachineTargetGroup targetGroup;
    [SerializeField, Min(0f)] private float idleWeight = 0.15f;
    [SerializeField, Min(0f)] private float focusWeight = 1f;
    [SerializeField, Min(0f)] private float focusHoldSeconds = 2.5f;
    [SerializeField, Min(0.01f)] private float blendSpeed = 2f;

    private readonly Dictionary<Transform, float> focusUntil = new();

    private void LateUpdate()
    {
        if (sandbox == null || targetGroup == null) return;

        float now = Time.time;
        foreach (var controller in sandbox.Spawned)
        {
            if (controller == null || controller.Agent == null) continue;
            var agent = controller.Agent;
            bool interesting = agent.IsAirborne || agent.IsRecovering ||
                               agent.Intent == CreatureIntent.Clashing || agent.Intent == CreatureIntent.Dazed;
            if (interesting) focusUntil[controller.transform] = now + focusHoldSeconds;
            var target = agent.ClashTarget;
            if (target != null) focusUntil[target.transform] = now + focusHoldSeconds;
        }

        bool anyFocus = false;
        foreach (var until in focusUntil.Values)
            if (until > now) { anyFocus = true; break; }

        var targets = targetGroup.Targets;
        float blend = 1f - Mathf.Exp(-blendSpeed * Time.deltaTime);
        for (int i = 0; i < targets.Count; i++)
        {
            var t = targets[i];
            if (t.Object == null) continue;
            bool focused = focusUntil.TryGetValue(t.Object, out float until) && until > now;
            float desired = !anyFocus || focused ? focusWeight : idleWeight;
            t.Weight = Mathf.Lerp(t.Weight, desired, blend);
            targets[i] = t;
        }
    }
}
}
