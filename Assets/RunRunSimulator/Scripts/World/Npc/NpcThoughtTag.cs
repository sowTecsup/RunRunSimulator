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

    private NpcAgent.NpcState    lastState;
    private NpcAgent.LeaveReason lastReason;
    private bool                 situationInit;
    private string               pendingLine;
    private string               shownLine = "";
    private float                responseTimer;

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
                           : Loc.Tr("npc.customer");

        UpdateThought();
    }

    private void UpdateThought()
    {
        if (thoughtLabel == null) return;

        var state  = agent.State;
        var reason = agent.Reason;
        if (!situationInit || state != lastState || reason != lastReason)
        {
            situationInit = true;
            lastState     = state;
            lastReason    = reason;
            string name   = agent.TargetMM != null && !string.IsNullOrEmpty(agent.TargetMM.CustomName)
                          ? agent.TargetMM.CustomName : Loc.Tr("npc.unnamed");
            pendingLine   = NpcDialogueBank.Pick(state, reason, name);
            responseTimer = agent.ReactionDelay;
        }

        if (responseTimer > 0f) responseTimer -= Time.deltaTime;
        else                    shownLine = pendingLine;

        thoughtLabel.text          = shownLine;
        thoughtLabel.style.display = string.IsNullOrEmpty(shownLine) ? DisplayStyle.None : DisplayStyle.Flex;
    }
}
}
