using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

// Data-driven elemental table (S40): identities, state definitions and the
// 12 mark-reaction recipes that used to be hardcoded in CombatElements. One
// asset, wired via CombatManagerSO — same pairs/log strings as the values
// they replace (see ElementTableSO.PopulateV1).
[Serializable]
public class ElementIdentity
{
    public string DisplayName;
    public Color  UiColor;
}

[Serializable]
public class StateDefinition
{
    public string DisplayName;
    [TextArea] public string Description;
    [PropertyRange(0f, 1f), LabelText("Percent (0-1)"), PropertyTooltip("Magnitud porcentual del estado (0 = sin knob porcentual). Qué representa exactamente lo dice la Description de cada estado.")]
    public float Percent;
    [LabelText("Amount (flat)"), PropertyTooltip("Magnitud plana del estado en HP/daño (0 = sin knob plano). Qué representa exactamente lo dice la Description de cada estado.")]
    public float Amount;
}

[Serializable]
public class ElementReaction
{
    public string  Name;
    public Element A;
    public Element B;
    [LabelText("Fuente aliada")] public bool AllySource;
    public List<ReactionEffectBase> Effects = new List<ReactionEffectBase>();
}

[CreateAssetMenu(fileName = "ElementTable", menuName = "RunRunSimulator/Combat/Element Table")]
public class ElementTableSO : SerializedScriptableObject
{
    [Title("Identidad")]
    [OdinSerialize]
    public Dictionary<Element, ElementIdentity> Identities;

    [Title("Estados")]
    [OdinSerialize]
    public Dictionary<ElementalState, StateDefinition> States;

    [Title("Reacciones")]
    [OdinSerialize]
    public List<ElementReaction> Reactions;

    public StateDefinition GetState(ElementalState s)
    {
        if (States != null && States.TryGetValue(s, out var def) && def != null)
            return def;

        return new StateDefinition();
    }

    public float StatePercent(ElementalState s) => GetState(s).Percent;

    public float StateAmount(ElementalState s) => GetState(s).Amount;

    public ElementIdentity GetIdentity(Element e)
    {
        if (Identities != null && Identities.TryGetValue(e, out var identity) && identity != null)
            return identity;

        return new ElementIdentity { DisplayName = e.ToString() };
    }

    public ElementReaction FindReaction(Element a, Element b, bool allySource)
    {
        if (a == b) return null;
        if (Reactions == null) return null;

        foreach (var reaction in Reactions)
        {
            if (reaction.AllySource != allySource) continue;

            bool matches = (reaction.A == a && reaction.B == b) || (reaction.A == b && reaction.B == a);
            if (matches) return reaction;
        }

        return null;
    }

    [Button("Poblar v1 (identidad + estados + 12 reacciones)")]
    private void PopulateV1()
    {
        Identities = new Dictionary<Element, ElementIdentity>
        {
            [Element.Agua]         = new ElementIdentity { DisplayName = "Agua", UiColor = new Color(0.2f, 0.6f, 1f) },
            [Element.Fuego]        = new ElementIdentity { DisplayName = "Fuego", UiColor = new Color(1f, 0.35f, 0.1f) },
            [Element.Electricidad] = new ElementIdentity { DisplayName = "Electricidad", UiColor = new Color(1f, 0.9f, 0.1f) },
            [Element.Planta]       = new ElementIdentity { DisplayName = "Planta", UiColor = new Color(0.25f, 0.75f, 0.25f) },
        };

        States = new Dictionary<ElementalState, StateDefinition>
        {
            [ElementalState.Vaporizado]   = new StateDefinition { DisplayName = "Vaporizado", Description = "Suma Percent como bonus plano a la chance de esquivar. Se consume al lograr esquivar un ataque. (Aliado, Agua+Fuego)", Percent = 0.30f },
            [ElementalState.GolpePreciso] = new StateDefinition { DisplayName = "GolpePreciso", Description = "Suma Percent como bonus plano a la chance de crítico. Se consume al conectar un crítico. (Aliado, Agua+Electricidad)", Percent = 0.25f },
            [ElementalState.Boiling]      = new StateDefinition { DisplayName = "Boiling", Description = "El próximo golpe que reciba hace +Percent de daño extra (antes del escudo). Se consume al recibir un golpe conectado. (Ofensivo, Agua+Fuego)", Percent = 0.30f },
            [ElementalState.Charcoal]     = new StateDefinition { DisplayName = "Charcoal", Description = "Devuelve Percent del daño recibido al agresor (puede matarlo). Se consume al recibir un golpe conectado. (Aliado, Fuego+Planta)", Percent = 0.50f },
            [ElementalState.Mareado]      = new StateDefinition { DisplayName = "Mareado", Description = "En su próxima acción: Percent de chance de golpear a un aliado al azar (incluido él mismo) por Amount de daño fijo en vez de atacar; si resiste el roll actúa normal. Se consume al actuar e igual genera afinidad. (Ofensivo, Fuego+Electricidad)", Percent = 0.50f, Amount = 3f },
            [ElementalState.Energizado]   = new StateDefinition { DisplayName = "Energizado", Description = "Actúa PRIMERO en el orden de la siguiente ronda (antes que la velocidad). Se consume al actuar con prioridad. Sin knobs. (Aliado, Fuego+Electricidad)" },
            [ElementalState.Cleanse]      = new StateDefinition { DisplayName = "Cleanse", Description = "Instantáneo: purga el primer estado negativo armado del portador; si no tiene ninguno, cura un % de su vida máxima. El % vive en el leaf CleanseEffect de la reacción, no acá. (Aliado, Agua+Planta)" },
            [ElementalState.OverGrow]     = new StateDefinition { DisplayName = "OverGrow", Description = "Instantáneo: duplica el escudo actual del portador (0 queda 0). Sin knobs. (Aliado, Electricidad+Planta)" },
            [ElementalState.Debilidad]    = new StateDefinition { DisplayName = "Debilidad", Description = "El próximo golpe que reciba IGNORA su DEF (reducción = 0). Se consume al recibir un golpe conectado. Sin knobs. (Ofensivo, Fuego+Planta)" },
            [ElementalState.Confuso]      = new StateDefinition { DisplayName = "Confuso", Description = "Su próxima acción falla COMPLETA (no ataca, no traits, no ítems); igual genera afinidad. Se consume al actuar. Sin knobs. (Ofensivo, Agua+Electricidad)" },
            [ElementalState.Leech]        = new StateDefinition { DisplayName = "Leech", Description = "Instantáneo: drena HP fijo del portador y se lo da a quien detonó la reacción (no baja de 0, no cura sobre el máximo). La cantidad vive en el leaf LeechEffect de la reacción, no acá. (Ofensivo, Agua+Planta)" },
            [ElementalState.PisoTierra]   = new StateDefinition { DisplayName = "PisoTierra", Description = "Instantáneo: remueve UNA marca al azar del portador (de cualquier canal); sin marcas no hace nada. Sin knobs. (Ofensivo, Electricidad+Planta)" },
        };

        Reactions = new List<ElementReaction>
        {
            new ElementReaction { Name = "Vaporizado", A = Element.Agua, B = Element.Fuego, AllySource = true,
                Effects = new List<ReactionEffectBase> { new ArmStateEffect { State = ElementalState.Vaporizado } } },
            new ElementReaction { Name = "GolpePreciso", A = Element.Agua, B = Element.Electricidad, AllySource = true,
                Effects = new List<ReactionEffectBase> { new ArmStateEffect { State = ElementalState.GolpePreciso } } },
            new ElementReaction { Name = "Cleanse", A = Element.Agua, B = Element.Planta, AllySource = true,
                Effects = new List<ReactionEffectBase> { new CleanseEffect { HealPercent = 0.20f } } },
            new ElementReaction { Name = "Energizado", A = Element.Fuego, B = Element.Electricidad, AllySource = true,
                Effects = new List<ReactionEffectBase> { new ArmStateEffect { State = ElementalState.Energizado } } },
            new ElementReaction { Name = "Charcoal", A = Element.Fuego, B = Element.Planta, AllySource = true,
                Effects = new List<ReactionEffectBase> { new ArmStateEffect { State = ElementalState.Charcoal } } },
            new ElementReaction { Name = "OverGrow", A = Element.Electricidad, B = Element.Planta, AllySource = true,
                Effects = new List<ReactionEffectBase> { new DoubleShieldEffect() } },

            new ElementReaction { Name = "Boiling", A = Element.Agua, B = Element.Fuego, AllySource = false,
                Effects = new List<ReactionEffectBase> { new ArmStateEffect { State = ElementalState.Boiling } } },
            new ElementReaction { Name = "Confuso", A = Element.Agua, B = Element.Electricidad, AllySource = false,
                Effects = new List<ReactionEffectBase> { new ArmStateEffect { State = ElementalState.Confuso } } },
            new ElementReaction { Name = "Leech", A = Element.Agua, B = Element.Planta, AllySource = false,
                Effects = new List<ReactionEffectBase> { new LeechEffect { Amount = 4f } } },
            new ElementReaction { Name = "Mareado", A = Element.Fuego, B = Element.Electricidad, AllySource = false,
                Effects = new List<ReactionEffectBase> { new ArmStateEffect { State = ElementalState.Mareado } } },
            new ElementReaction { Name = "Debilidad", A = Element.Fuego, B = Element.Planta, AllySource = false,
                Effects = new List<ReactionEffectBase> { new ArmStateEffect { State = ElementalState.Debilidad } } },
            new ElementReaction { Name = "PisoTierra", A = Element.Electricidad, B = Element.Planta, AllySource = false,
                Effects = new List<ReactionEffectBase> { new RemoveRandomMarkEffect() } },
        };

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
}
