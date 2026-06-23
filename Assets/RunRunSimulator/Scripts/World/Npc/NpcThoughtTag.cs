using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{
[DisallowMultipleComponent]
[RequireComponent(typeof(UIDocument))]
public class NpcThoughtTag : MonoBehaviour
{
    [Header("Visibility")]
    [SerializeField] private float showDistance = 12f;
    [SerializeField] private bool  uprightOnly = true;

    private UIDocument    document;
    private VisualElement root;
    private Label         nameLabel;
    private Label         thoughtLabel;

    private NpcAgent  agent;
    private Transform cam;
    private bool      shown = true;

    private void Awake()
    {
        document = GetComponent<UIDocument>();
        agent    = GetComponentInParent<NpcAgent>();
        if (Camera.main != null) cam = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (cam == null)
        {
            if (Camera.main == null) return;
            cam = Camera.main.transform;
        }

        float distSqr = (cam.position - transform.position).sqrMagnitude;
        bool  visible = distSqr <= showDistance * showDistance;
        if (visible != shown) SetShown(visible);
        if (!visible) return;

        Refresh();

        Vector3 toCam = transform.position - cam.position;
        if (uprightOnly) toCam.y = 0f;
        if (toCam.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(toCam);
    }

    private void SetShown(bool visible)
    {
        shown = visible;
        ResolveElements();
        if (root != null) root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void ResolveElements()
    {
        var docRoot = document != null ? document.rootVisualElement : null;
        if (docRoot == null) return;
        if (docRoot == root && thoughtLabel != null) return;

        root = docRoot;
        root.style.alignItems     = Align.Center;
        root.style.justifyContent = Justify.Center;
        root.pickingMode          = PickingMode.Ignore;

        nameLabel    = root.Q<Label>("npc-name-label");
        thoughtLabel = root.Q<Label>("thought-label");
    }

    private void Refresh()
    {
        ResolveElements();
        if (agent == null) return;

        if (nameLabel != null)
            nameLabel.text = !string.IsNullOrEmpty(agent.DisplayName) ? agent.DisplayName
                           : agent.Archetype != null ? agent.Archetype.DisplayName
                           : "Cliente";

        if (thoughtLabel != null)
        {
            string thought = ThoughtText(agent.State, agent.TargetMM, agent.QueueWasFull);
            thoughtLabel.text          = thought;
            thoughtLabel.style.display = string.IsNullOrEmpty(thought) ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    private static string ThoughtText(NpcAgent.NpcState state, CreatureDNA target, bool queueWasFull)
    {
        string targetName = target != null && !string.IsNullOrEmpty(target.CustomName) ? target.CustomName : "ese";
        return state switch
        {
            NpcAgent.NpcState.Wandering           => "¿Qué tendrán hoy?",
            NpcAgent.NpcState.InspectingDisplay   => "Mmm, déjame ver…",
            NpcAgent.NpcState.ApproachingRegister => $"¡Me llevo a {targetName}!",
            NpcAgent.NpcState.Queueing            => "Esperaré mi turno…",
            NpcAgent.NpcState.WaitingAtRegister   => "¿Hay alguien en la caja?",
            NpcAgent.NpcState.Negotiating         => $"¿Cuánto por {targetName}?",
            NpcAgent.NpcState.Leaving             => (target != null && target.IsSold) ? $"¡{targetName} se viene conmigo!"
                                                   : queueWasFull                      ? "¡Está muy lleno!"
                                                   : "Será en otra ocasión…",
            _                                     => "",
        };
    }
}
}
