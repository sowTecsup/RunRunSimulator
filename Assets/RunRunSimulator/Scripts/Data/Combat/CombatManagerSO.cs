using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "CombatManager", menuName = "RunRunSimulator/Combat/Combat Manager")]
public class CombatManagerSO : SerializedScriptableObject
{
    [Title("Combat Settings")]
    [InfoBox("DeathChance es un valor 0–1 (ej: 0.3 = 30%).")]

    [LabelWidth(160)] public float DeathChance     = 0.05f;

    [Title("Hit Settings")]
    [LabelWidth(160)] public float CritChance     = 0.10f;
    [LabelWidth(160)] public float CritMultiplier = 3f;

    [Title("Derived Stat Coefficients")]
    [InfoBox("Por punto de stat (0–1). LCK: +crit. DEF: -daño recibido. EVA: chance de esquivar.")]
    [LabelWidth(160)] public float LuckCritPerPoint      = 0.03f;
    [LabelWidth(160)] public float DefenseReductionPerPoint = 0.08f;
    [LabelWidth(160)] public float EvasionPerPoint       = 0.10f;

    [Title("Safety")]
    [LabelWidth(160)] public int MaxRounds    = 50;
    [LabelWidth(160)] public int MaxFightCount = 5;

    [Title("Status / Balance")]
    [InfoBox("Anti-permastun: un stun activo nunca se re-aplica, y al despertar el MoriMochi es inmune a nuevos stuns por N turnos propios.")]
    [LabelWidth(160)] public int StunImmunityTurns = 1;

    [InfoBox("Tabla de roles 3v3: mods de stats natos + tuning de traits (escudo/backline/cura). Sin tabla = roles sin efecto.")]
    [LabelWidth(160)] public RoleTableSO Roles;

    [Title("Elemental")]
    [InfoBox("Tabla elemental: identidad de elementos, estados (con sus magnitudes) y reacciones por fuente. Sin tabla = sin reacciones ni bonus de estados.")]
    [LabelWidth(160)] public ElementTableSO Elements;

    [Title("Needs")]
    [InfoBox("Energía que gasta un MoriMochi al encolarse para combate (NeedsState).")]
    [LabelWidth(160)] public float EnergyCostToQueue = 15f;
}
}
