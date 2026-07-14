using System;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace MoriMonchiSimulator
{

// Single scene-level owner of the combat replay's MMFeedbacks (S46). Subscribes
// to the visual bus and plays each feedback AT the affected MoriMochi's world
// position via MMFeedbacks.PlayFeedbacks(Vector3) — so one feedback asset per
// event kind is authored once in the scene instead of duplicated inside every
// unit prefab. MMF_Player derives from MMFeedbacks, so both fit these fields.
// Position comes from CombatVisualizerService (PosOf), the same access pattern
// CombatCameraDirector already uses for VCamOf.
//
// The 12 elemental states are keyed by their ElementalState. A reaction is
// matched to its state by parsing the ElementTable reaction's Name — armed and
// instant reactions alike — so renaming a reaction in the asset silently drops
// its feedback (nothing else breaks).
public class CombatFeelDirector : SerializedMonoBehaviour
{
    private const string Group = "Feedbacks";

    [TabGroup(Group, "Soporte")]
    [Tooltip("Sobre el aliado que RECIBE el escudo.")]
    [SerializeField] private MMFeedbacks shieldFeedback;

    [TabGroup(Group, "Soporte")]
    [Tooltip("Sobre el aliado que RECIBE la curación.")]
    [SerializeField] private MMFeedbacks healFeedback;

    [TabGroup(Group, "Marcas")]
    [Tooltip("Sobre quien recibe una marca, según el elemento de la marca.")]
    [OdinSerialize] private Dictionary<Element, MMFeedbacks> markFeedbacks = new Dictionary<Element, MMFeedbacks>();

    [TabGroup(Group, "Estados")]
    [Tooltip("Sobre quien detona la reacción, según el estado resultante.")]
    [OdinSerialize] private Dictionary<ElementalState, MMFeedbacks> stateFeedbacks = new Dictionary<ElementalState, MMFeedbacks>();

    [TabGroup(Group, "Ajustes")]
    [Tooltip("Altura sumada a la posición del MoriMochi antes de reproducir.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, 0f);

    private void OnEnable()
    {
        CombatVisualEvents.OnPopup       += HandlePopup;
        CombatVisualEvents.OnUnitElement += HandleUnitElement;
    }

    private void OnDisable()
    {
        CombatVisualEvents.OnPopup       -= HandlePopup;
        CombatVisualEvents.OnUnitElement -= HandleUnitElement;
    }

    private void HandlePopup(CombatVisualPopup p)
    {
        if (p.Kind == CombatPopupKind.Shield)    Play(shieldFeedback, p.Position);
        else if (p.Kind == CombatPopupKind.Heal) Play(healFeedback, p.Position);
    }

    private void HandleUnitElement(CombatElementEventData d)
    {
        if (d.Kind == ElementEventKind.MarkApplied)
        {
            if (markFeedbacks.TryGetValue(d.Element, out var mark)) PlayOn(mark, d.Side, d.Index);
            return;
        }

        if (d.Kind == ElementEventKind.Reaction
         && Enum.TryParse<ElementalState>(d.ReactionName, out var state)
         && stateFeedbacks.TryGetValue(state, out var feedback))
            PlayOn(feedback, d.Side, d.Index);
    }

    private void PlayOn(MMFeedbacks feedback, CombatVisualSide side, int index)
    {
        var service = CombatVisualizerService.Instance;
        if (service == null) return;

        Play(feedback, service.PosOf(side, index));
    }

    private void Play(MMFeedbacks feedback, Vector3 worldPos)
    {
        if (feedback == null) return;
        feedback.PlayFeedbacks(worldPos + offset);
    }

#if UNITY_EDITOR
    [TabGroup(Group, "Ajustes")]
    [Button("Crear objetos de feedback y wirear"), GUIColor(0.4f, 1f, 0.6f)]
    private void BuildFeedbackObjects()
    {
        shieldFeedback = EnsureChild("Feel_Shield", shieldFeedback);
        healFeedback   = EnsureChild("Feel_Heal", healFeedback);

        foreach (Element e in Enum.GetValues(typeof(Element)))
        {
            markFeedbacks.TryGetValue(e, out var current);
            markFeedbacks[e] = EnsureChild("Feel_Mark_" + e, current);
        }

        foreach (ElementalState s in Enum.GetValues(typeof(ElementalState)))
        {
            stateFeedbacks.TryGetValue(s, out var current);
            stateFeedbacks[s] = EnsureChild("Feel_State_" + s, current);
        }

        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

    private MMFeedbacks EnsureChild(string childName, MMFeedbacks current)
    {
        if (current != null) return current;

        var existing = transform.Find(childName);
        if (existing != null)
        {
            var found = existing.GetComponent<MMF_Player>();
            if (found != null) return found;
            return existing.gameObject.AddComponent<MMF_Player>();
        }

        var go = new GameObject(childName);
        UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Crear feedback");
        go.transform.SetParent(transform, false);
        return go.AddComponent<MMF_Player>();
    }
#endif
}
}
