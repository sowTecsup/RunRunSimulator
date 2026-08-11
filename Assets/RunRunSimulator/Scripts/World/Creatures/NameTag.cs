using System;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

// A floating world-space label above a MoriMochi, rendered with UI Toolkit (a
// world-space UIDocument driving NameTagUITK.uxml) instead of TextMeshPro. In the
// free-roam layout shows the name plus AT MOST one secondary line, picked by
// priority: pet hint > busy/dead status > "interesting" CreatureIntent (Idle and
// Wandering are the default and stay silent). Billboards toward the camera and
// only appears when the player is near, so the world isn't a wall of text. Pure
// view — it reads live state each frame and never mutates anything.
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

    [Header("Pen layout")]
    [Tooltip("Extra height (m) added while penned, so the compact breeding tag doesn't clip the floor.")]
    [SerializeField] private float penRaise = 0.6f;
    [Tooltip("Uniform scale applied while penned, to make the breeding tag more compact.")]
    [SerializeField] private float penScale = 0.8f;

    private UIDocument    document;
    private VisualElement root;
    private Label         nameLabel;
    private Label         priceLabel;
    private Label         statusLabel;
    private Label         intentLabel;
    private Label         petHintLabel;
    private Label         genderLabel;
    private Label         roleLabel;
    private Label         stageLabel;
    private Label         breedLabel;
    private Label         heartLabel;
    private Label         timerLabel;

    private MoriMochiAgent agent;
    private CreatureDNA    dna;
    private Transform      cam;
    private bool           shown = true;

    private Vector3 baseLocalPos;
    private Vector3 baseLocalScale;

    private void Awake()
    {
        document = GetComponent<UIDocument>();
      //  agent    = GetComponentInParent<MoriMochiAgent>();   // intent source (same World domain → direct ref)
        if (Camera.main != null) cam = Camera.main.transform;
        baseLocalPos   = transform.localPosition;
        baseLocalScale = transform.localScale;
    }

    // Called by the agent right after Initialize(). The document tree is built in the
    // UIDocument's own OnEnable (during Instantiate, before this runs), so the labels
    // resolve here.
    public void Bind(CreatureDNA creature, MoriMochiAgent agent)
    {

        dna = creature;
        this.agent = agent;
        ResolveElements();
        if (nameLabel != null)
        {
            nameLabel.text = creature?.CustomName ?? "";
            if (creature != null) nameLabel.style.color = GenderColor(creature.Gender);
        }
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

        nameLabel        = root.Q<Label>("name-label");
        priceLabel       = root.Q<Label>("price-label");
        statusLabel      = root.Q<Label>("status-label");
        intentLabel      = root.Q<Label>("intent-label");
        petHintLabel     = root.Q<Label>("pet-hint-label");
        genderLabel      = root.Q<Label>("gender-label");
        roleLabel        = root.Q<Label>("role-label");
        stageLabel       = root.Q<Label>("stage-label");
        breedLabel       = root.Q<Label>("breed-label");
        heartLabel       = root.Q<Label>("heart-label");
        timerLabel       = root.Q<Label>("timer-label");
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

        // Penned creatures get a raised, compact tag so the breeding layout clears the floor.
        // Position is driven in WORLD space (parent position + world-up offset) so the tag's
        // height never orbits with the parent's rotation when the body tumbles/rolls.
        bool penned = agent != null && agent.IsPenned;
        transform.localScale = penned ? baseLocalScale * penScale : baseLocalScale;

        if (transform.parent != null)
        {
            float heightWorld = baseLocalPos.y + (penned ? penRaise : 0f);
            transform.position = transform.parent.position + Vector3.up * heightWorld;
        }
        else
        {
            transform.localPosition = penned ? baseLocalPos + Vector3.up * penRaise : baseLocalPos;
        }

        // Billboard: point the panel's front (+Z, the face UITK draws on) at the camera.
        Vector3 toCam = transform.position - cam.position;
        if (uprightOnly) toCam.y = 0f;
        if (toCam.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(toCam, Vector3.up);
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

        // Store creatures show the sale layout (name + price); other penned creatures swap to the
        // breeding pen layout (gender + name + personality, plus heart/timer while breeding); free
        // creatures keep the status/intent/pet-hint readout.
        if      (agent != null && agent.IsForSale) RefreshStore();
        else if (agent != null && agent.IsPenned)  RefreshPenned();
        else                                       RefreshDefault();
    }

    // Store layout: name (kept from Bind) + the sale price under it. Every other line hides.
    private void RefreshStore()
    {
        SetDisplay(statusLabel,      false);
        SetDisplay(intentLabel,      false);
        SetDisplay(petHintLabel,     false);
        SetDisplay(genderLabel,      false);
        SetDisplay(roleLabel, false);
        SetDisplay(stageLabel,       false);
        SetDisplay(breedLabel,       false);
        SetDisplay(heartLabel,       false);
        SetDisplay(timerLabel,       false);

        if (priceLabel != null)
        {
            var svc = CustomerService.Instance;
            int price = svc != null ? svc.EstimateAverage(dna) : 0;
            priceLabel.text = Loc.Tr("nametag.price", price);
            SetDisplay(priceLabel, true);
        }
    }

    // Pen layout: gender glyph + name + personality only. While the creature is breeding, add a
    // heart and the egg's live countdown. The free-roam lines (status/intent/pet-hint) are hidden.
    private void RefreshPenned()
    {
        SetDisplay(priceLabel,   false);
        SetDisplay(statusLabel,  false);
        SetDisplay(intentLabel,  false);
        SetDisplay(petHintLabel, false);

        if (genderLabel != null)
        {
            genderLabel.text        = GenderGlyph(dna.Gender);
            genderLabel.style.color = GenderColor(dna.Gender);
            SetDisplay(genderLabel, true);
        }
        if (roleLabel != null)
        {
            roleLabel.text = LocEnumMaps.RoleName(dna.Role);
            SetDisplay(roleLabel, true);
        }
        if (stageLabel != null)
        {
            stageLabel.text = StageText(dna.AgeDays);
            SetDisplay(stageLabel, true);
        }
        if (breedLabel != null)
        {
            breedLabel.text = $"{dna.BreedCount}/{BreedingService.MaxBreedCount}";
            SetDisplay(breedLabel, true);
        }

        bool breeding = dna.BusyState == BusyReason.Breeding && dna.BreedReadyAt > 0;
        SetDisplay(heartLabel, breeding);
        SetDisplay(timerLabel, breeding);
        if (breeding && timerLabel != null) timerLabel.text = CountdownText(dna.BreedReadyAt);
    }

    // Free-roam layout: name (kept from Bind) + at most ONE secondary line, chosen by priority
    // so the tag stays compact — pet hint beats status beats intent. Stage/age is pen-only.
    private void RefreshDefault()
    {
        SetDisplay(priceLabel,  false);
        SetDisplay(genderLabel, false);
        SetDisplay(roleLabel,   false);
        SetDisplay(breedLabel,  false);
        SetDisplay(heartLabel,  false);
        SetDisplay(timerLabel,  false);
        SetDisplay(stageLabel,  false);

        // Priority 1: pet hint — either the post-pet debug flash or the "[E] Acariciar" prompt
        // shown only when the player is close enough and facing this creature.
        bool isBeingPetted = agent != null && agent.IsBeingPetted;
        bool showPetHint   = isBeingPetted ||
                              (agent != null && !dna.IsDead &&
                               agent.IsInFriendlyReaction && agent.IsPlayerFacingMe());

        // Priority 2: busy/dead status, when it has something to say.
        var (statusText, statusColor) = StatusOf(dna);
        bool showStatus = !showPetHint && !string.IsNullOrEmpty(statusText);

        // Priority 3: intent, but only the "interesting" ones — Idle/Wandering are the default
        // state and carry no information, so they stay silent.
        bool intentInteresting = agent != null && !dna.IsDead &&
                                  agent.Intent != CreatureIntent.Idle &&
                                  agent.Intent != CreatureIntent.Wandering;
        bool showIntent = !showPetHint && !showStatus && intentInteresting;

        SetDisplay(petHintLabel, showPetHint);
        if (showPetHint && petHintLabel != null)
            petHintLabel.text = isBeingPetted ? Loc.Tr("nametag.petting") : Loc.Tr("nametag.pethint");

        SetDisplay(statusLabel, showStatus);
        if (showStatus && statusLabel != null)
        {
            statusLabel.text        = statusText;
            statusLabel.style.color = statusColor;
        }

        SetDisplay(intentLabel, showIntent);
        if (showIntent && intentLabel != null)
            intentLabel.text = LocEnumMaps.IntentName(agent.Intent);
    }

    private static void SetDisplay(Label label, bool visible)
    {
        if (label != null) label.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // (text, color) for the busy/dead status line. Empty text → the line hides.
    private static (string, Color) StatusOf(CreatureDNA dna)
    {
        if (dna.IsDead) return (Loc.Tr("status.dead"), new Color(1f, 0.39f, 0.39f));
        return dna.BusyState switch
        {
            BusyReason.Breeding => (Loc.Tr("status.breeding"), new Color(1f, 0.61f, 0.82f)),
            _                   => ("", Color.clear),
        };
    }

    // ── Pen layout helpers ────────────────────────────────────────

    private static string GenderGlyph(CreatureGender g) => g switch
    {
        CreatureGender.Male   => "♂",
        CreatureGender.Female => "♀",
        _                     => "?",
    };

    private static Color GenderColor(CreatureGender g) => g switch
    {
        CreatureGender.Male   => new Color(0.45f, 0.65f, 1f),
        CreatureGender.Female => new Color(1f, 0.5f, 0.75f),
        _                     => new Color(0.7f, 0.7f, 0.7f),
    };

    private static string StageText(int ageDays)
    {
        var table = BreedingController.Instance != null ? BreedingController.Instance.LifeStageTable : null;
        return table != null
            ? Loc.Tr("nametag.stageage", LocEnumMaps.LifeStageName(table.GetStage(ageDays)), ageDays)
            : $"{ageDays}d";
    }

    // Live mm:ss until the egg can hatch (server epoch ms); ready prompt once due.
    private static string CountdownText(long readyAtMs)
    {
        long left = readyAtMs - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return left <= 0 ? Loc.Tr("nametag.ready") : $"{TimeSpan.FromMilliseconds(left):mm\\:ss}";
    }
}
}
