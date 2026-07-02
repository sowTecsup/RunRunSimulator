using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
namespace MoriMonchiSimulator
{

// Receta de sinergia: al acumular la VARIEDAD de stacks pedida (ej. Poison x3, o
// Poison x2 + Burn x2) la regla detona sobre el portador — quema esos stacks y aplica
// sus Effects. Se autora por regla dentro de un único SynergyTableSO.
public class SynergyStackRequirement
{
    public ModifierEffectKind Kind = ModifierEffectKind.Poison;

    [MinValue(1)]
    public int Stacks = 3;
}

public class SynergyRule
{
    public string Name = "Nueva sinergia";
    public List<SynergyStackRequirement> Requirements = new List<SynergyStackRequirement>();
    public List<SynergyEffectBase> Effects = new List<SynergyEffectBase>();

    public string Summary()
    {
        string reqs = Requirements != null && Requirements.Count > 0
            ? string.Join(" + ", Requirements.Select(r => $"{r.Stacks}x {r.Kind}"))
            : "sin requisitos";
        string effects = Effects != null && Effects.Count > 0
            ? string.Join(", ", Effects.Select(e => e.Summary()))
            : "sin efectos";
        return $"{Name}: {reqs} → {effects}";
    }
}
}
