using UnityEngine;

namespace MoriMonchiSimulator.Prototype
{
    [CreateAssetMenu(fileName = "SpiderTuning", menuName = "MoriMonchi/Prototype/Spider Tuning")]
    public class SpiderTuningSO : ScriptableObject
    {
        [Header("Cuerpo")]
        [Range(0.1f, 4f)] public float moveSpeed = 0.9f;
        [Range(30f, 270f)] public float turnSpeed = 90f;
        [Range(0.2f, 1.1f)] public float rideHeight = 0.55f;

        [Header("Patas")]
        [Range(0.05f, 0.6f)] public float stepDistance = 0.25f;
        [Range(0.02f, 0.4f)] public float stepHeight = 0.13f;
        [Range(0.04f, 0.5f)] public float stepDuration = 0.16f;
        [Range(0.3f, 2f)] public float footSplay = 1f;
        [Range(0f, 0.3f)] public float stepOvershoot = 0.12f;
        [Range(0f, 0.5f)] public float anticipation = 0.25f;
        [Range(5f, 60f)] public float maxTwist = 22f;

        [Header("Cuerpo vivo")]
        [Range(0f, 1f)] public float idleAmount = 0.6f;
        [Range(0f, 0.06f)] public float bobAmount = 0.02f;
        [Range(0f, 1f)] public float leanAmount = 0.5f;

        [Header("Ragdoll")]
        [Range(1f, 12f)] public float launchImpulse = 5f;
    }
}
