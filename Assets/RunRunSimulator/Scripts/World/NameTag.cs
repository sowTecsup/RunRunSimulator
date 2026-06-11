using UnityEngine;
using UnityEngine.UIElements;

// A floating world-space label above a MoriMochi, rendered with UI Toolkit (a
// world-space UIDocument driving NameTagUITK.uxml) instead of TextMeshPro. Shows
// three lines: the creature's name, a busy/dead status, and what it's trying to do
// RIGHT NOW (its CreatureIntent, read live from the MoriMochiAgent — "Te sigue",
// "Busca comida", "Comiendo", …). Billboards toward the camera and only appears
// when the player is near, so the world isn't a wall of text. Pure view — it reads
// live state each frame and never mutates anything.
//
// Setup: a CHILD object of the creature prefab (NOT the body root — billboard
// rotation would spin the mesh) carrying a UIDocument whose Panel Settings is
// WorldUIPanelSettings (render mode World Space) and whose Source Asset is
// NameTagUITK.uxml. Position the child a bit above the cube; the tag centers on
// the panel's pivot. The MoriMochiAgent finds it via GetComponentInChildren and
// calls Bind() once it knows the DNA.
[RequireComponent(typeof(UIDocument))]
public class NameTag : MonoBehaviour
{
    [Header("Visibility")]
    [Tooltip("Show the tag only when the camera is within this distance.")]
    [SerializeField] private float showDistance = 8f;
    [Tooltip("Keep the text upright (ignore camera pitch) instead of fully facing it.")]
    [SerializeField] private bool uprightOnly = true;

    private UIDocument    document;
    private VisualElement root;
    private Label         nameLabel;
    private Label         statusLabel;
    private Label         intentLabel;
    private Label         petHintLabel;

    private MoriMochiAgent agent;
    private CreatureDNA    dna;
    private Transform      cam;
    private bool           shown = true;

    private void Awake()
    {
        document = GetComponent<UIDocument>();
      //  agent    = GetComponentInParent<MoriMochiAgent>();   // intent source (same World domain → direct ref)
        if (Camera.main != null) cam = Camera.main.transform;
    }

    // Called by the agent right after Initialize(). The document tree is built in the
    // UIDocument's own OnEnable (during Instantiate, before this runs), so the labels
    // resolve here.
    public void Bind(CreatureDNA creature, MoriMochiAgent agent)
    {

        dna = creature;
        this.agent = agent;
        ResolveElements();
        if (nameLabel != null) nameLabel.text = creature?.CustomName ?? "";
        Refresh();
    }

    // Queries the named labels and configures the panel root. Re-resolves whenever the
    // UIDocument swaps in a NEW tree: when a pooled creature reactivates (SetActive false→
    // true) the document rebuilds rootVisualElement, orphaning the old Label refs — keeping
    // them would write the name/status/intent into elements no longer on screen.
    private void ResolveElements()
    {
        var docRoot = document != null ? document.rootVisualElement : null;
        if (docRoot == null) return;
        if (docRoot == root && nameLabel != null) return;   // already wired to the current tree

        root = docRoot;
        root.style.alignItems     = Align.Center;     // center the tag inside the world-space panel quad…
        root.style.justifyContent = Justify.Center;   // …so it floats over the pivot, not the panel's corner
        root.pickingMode          = PickingMode.Ignore;

        nameLabel    = root.Q<Label>("name-label");
        statusLabel  = root.Q<Label>("status-label");
        intentLabel  = root.Q<Label>("intent-label");
        petHintLabel = root.Q<Label>("pet-hint-label");
    }

    private void LateUpdate()
    {
        if (cam == null)
        {
            if (Camera.main == null) return;
            cam = Camera.main.transform;
        }

        // Distance-gated visibility — hide the whole panel when far, and skip the rest.
        float distSqr = (cam.position - transform.position).sqrMagnitude;
        bool  visible = distSqr <= showDistance * showDistance;
        if (visible != shown) SetShown(visible);
        if (!visible) return;

        Refresh();

        // Billboard: point the panel's front (+Z, the face UITK draws on) at the camera.
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

    private void Refresh()
    {
        ResolveElements();
        if (dna == null) return;

        if (statusLabel != null)
        {
            var (text, color) = StatusOf(dna);
            statusLabel.text          = text;
            statusLabel.style.color   = color;
            statusLabel.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        if (intentLabel != null)
        {
            // A dead creature has no intent (and is about to despawn) — hide the line.
            bool showIntent = !dna.IsDead && agent != null;
            intentLabel.style.display = showIntent ? DisplayStyle.Flex : DisplayStyle.None;
            if (showIntent) intentLabel.text = IntentText(agent.Intent);
        }

        if (petHintLabel != null)
        {
            if (agent != null && agent.IsBeingPetted)
            {
                // Debug visual: shows briefly after the player pets this creature.
                petHintLabel.text          = "Petting...";
                petHintLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                // Show "[E] Acariciar" only when: friendly reaction AND the player is facing
                // this creature (IsPlayerFacingMe: player.forward · to-creature, XZ, petRadius + petLookAngle).
                // Only one creature at a time ever shows the hint, even when surrounded.
                bool showHint = agent != null && !dna.IsDead &&
                                agent.IsInFriendlyReaction &&
                                agent.IsPlayerFacingMe();
                petHintLabel.text          = "[E] Acariciar";
                petHintLabel.style.display = showHint ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }

    // (text, color) for the busy/dead status line. Empty text → the line hides.
    private static (string, Color) StatusOf(CreatureDNA dna)
    {
        if (dna.IsDead) return ("Muerto", new Color(1f, 0.39f, 0.39f));
        return dna.BusyState switch
        {
            BusyReason.QueuedForCombat => ("En cola",   new Color(1f, 0.71f, 0.39f)),
            BusyReason.Breeding        => ("Incubando", new Color(1f, 0.61f, 0.82f)),
            _                          => ("", Color.clear),
        };
    }

    // Player-facing phrasing of the agent's current intent (Spanish, neutral).
    private static string IntentText(CreatureIntent intent) => intent switch
    {
        CreatureIntent.Idle         => "Quieto",
        CreatureIntent.Wandering    => "Paseando",
        CreatureIntent.Following     => "Te sigue",
        CreatureIntent.Approaching   => "Se acerca",
        CreatureIntent.Fleeing       => "¡Huye!",
        CreatureIntent.Retreating    => "Se aleja",
        CreatureIntent.SeekingFood   => "Busca comida",
        CreatureIntent.SeekingRest   => "Va a descansar",
        CreatureIntent.SeekingPlay   => "Busca jugar",
        CreatureIntent.Eating        => "Comiendo",
        CreatureIntent.Resting       => "Durmiendo",
        CreatureIntent.Playing       => "Jugando",
        CreatureIntent.Held          => "En tus manos",
        CreatureIntent.Tumbling      => "¡Por los aires!",
        _                            => "",
    };
}
