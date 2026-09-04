using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

public enum ClashSlot { Horn = 0, Wings = 1, Back = 2 }

[CreateAssetMenu(fileName = "ClashMove", menuName = "RunRunSimulator/Expedition/Clash Move")]
public class ClashMoveSO : ScriptableObject
{
    [Title("Slot")]
    public ClashSlot Slot = ClashSlot.Horn;

    [Title("Tiempos")]
    [Min(0f)] public float AnticipationSeconds = 0.3f;
    [Min(0.1f)] public float StrikeSeconds = 1.2f;

    [Title("Alcance e impacto")]
    [Min(0.5f)] public float Range = 5f;
    [Min(0.2f)] public float HitRadius = 1.1f;
    [Min(0f)] public float Impulse = 9f;
    [Range(0f, 1f)] public float UpBias = 0.25f;

    [Title("Embestida (Horn)")]
    [Min(0f)] public float DashSpeed = 14f;
    [Min(0f)] public float DashAcceleration = 60f;
    [Min(0f)] public float SelfRecoil = 0f;

    [Title("Picada (Wings)")]
    [Range(5f, 85f)] public float LaunchAngle = 45f;

    [Title("Coletazo (Back)")]
    [Min(0f)] public float SweepRadius = 2.2f;

    [Title("Gestos")]
    public string TellGesture = "Roar";
    public string StrikeGesture = "";

    public string Summary() => $"{Slot}: alcance {Range:0.#} m, impulso {Impulse:0.#}";
}
}
