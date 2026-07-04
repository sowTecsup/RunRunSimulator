using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

// Tabla única autorable de recetas de sinergia (mismo patrón "un solo asset" que
// BreedingAffinityTableSO). CombatResolver revisa los stacks activos del portador
// contra Rules cada vez que gana un stack; la receta satisfecha quema esos stacks
// (FIFO) y aplica sus Effects sobre el portador.
[CreateAssetMenu(fileName = "SynergyTable", menuName = "RunRunSimulator/Combat/Synergy Table")]
public class SynergyTableSO : SerializedScriptableObject
{
    [Title("Synergy Rules")]
    [OdinSerialize]
    [ListDrawerSettings(DefaultExpandedState = true)]
    public List<SynergyRule> Rules = new List<SynergyRule>();

    [ShowInInspector, ReadOnly, MultiLineProperty(3), LabelText("Resumen")]
    private string RulesSummary =>
        Rules == null || Rules.Count == 0
            ? "—"
            : string.Join("\n", Rules.Where(r => r != null).Select(r => $"• {r.Summary()}"));

    [Button("Receta ejemplo")]
    private void AddExampleRule()
    {
        Rules ??= new List<SynergyRule>();
        Rules.Add(new SynergyRule
        {
            Name = "Explosión tóxica",
            Requirements = new List<SynergyStackRequirement>
            {
                new SynergyStackRequirement { Kind = ModifierEffectKind.Poison, Stacks = 3 },
            },
            Effects = new List<SynergyEffectBase>
            {
                new SynergyDamageEffect { Amount = 10f },
            },
        });
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    [Button("Recetas v1 (elementos)")]
    private void AddElementRecipes()
    {
        Rules ??= new List<SynergyRule>();
        Rules.Add(new SynergyRule
        {
            Name = "Regeneración",
            Requirements = new List<SynergyStackRequirement>
            {
                new SynergyStackRequirement { Kind = ModifierEffectKind.Pulse, Stacks = 3 },
                new SynergyStackRequirement { Kind = ModifierEffectKind.Steel, Stacks = 1 },
            },
            Effects = new List<SynergyEffectBase>
            {
                new SynergyStatusEffect { Kind = ModifierEffectKind.Regen, Turns = 3, Magnitude = 4 },
            },
        });
        Rules.Add(new SynergyRule
        {
            Name = "Cortocircuito",
            Requirements = new List<SynergyStackRequirement>
            {
                new SynergyStackRequirement { Kind = ModifierEffectKind.Static, Stacks = 2 },
                new SynergyStackRequirement { Kind = ModifierEffectKind.Mist,   Stacks = 1 },
            },
            Effects = new List<SynergyEffectBase>
            {
                new SynergyStunEffect { Turns = 1 },
            },
        });
        Rules.Add(new SynergyRule
        {
            Name = "Robo de vida",
            Requirements = new List<SynergyStackRequirement>
            {
                new SynergyStackRequirement { Kind = ModifierEffectKind.Pulse, Stacks = 2 },
                new SynergyStackRequirement { Kind = ModifierEffectKind.Mist,  Stacks = 1 },
            },
            Effects = new List<SynergyEffectBase>
            {
                new SynergyStatusEffect { Kind = ModifierEffectKind.Lifesteal, Turns = 3, Magnitude = 30 },
            },
        });
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
}
