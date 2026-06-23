using System;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

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
    private Label         personalityLabel;
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

        nameLabel        = root.Q<Label>("name-label");
        priceLabel       = root.Q<Label>("price-label");
        statusLabel      = root.Q<Label>("status-label");
        intentLabel      = root.Q<Label>("intent-label");
        petHintLabel     = root.Q<Label>("pet-hint-label");
        genderLabel      = root.Q<Label>("gender-label");
        personalityLabel = root.Q<Label>("personality-label");
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
        bool penned = agent != null && agent.IsPenned;
        transform.localScale    = penned ? baseLocalScale * penScale         : baseLocalScale;
        transform.localPosition = penned ? baseLocalPos + Vector3.up * penRaise : baseLocalPos;

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
        SetDisplay(personalityLabel, false);
        SetDisplay(stageLabel,       false);
        SetDisplay(breedLabel,       false);
        SetDisplay(heartLabel,       false);
        SetDisplay(timerLabel,       false);

        if (priceLabel != null)
        {
            var svc = CustomerService.Instance;
            int price = svc != null ? svc.EstimateAverage(dna) : 0;
            priceLabel.text = $"{price} D";
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
        if (personalityLabel != null)
        {
            personalityLabel.text = PersonalityText(dna.Personality);
            SetDisplay(personalityLabel, true);
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

    private void RefreshDefault()
    {
        SetDisplay(priceLabel,       false);
        SetDisplay(genderLabel,      false);
        SetDisplay(personalityLabel, false);
        SetDisplay(breedLabel,       false);
        SetDisplay(heartLabel,       false);
        SetDisplay(timerLabel,       false);

        if (stageLabel != null)
        {
            stageLabel.text = StageText(dna.AgeDays);
            SetDisplay(stageLabel, true);
        }

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

    private static void SetDisplay(Label label, bool visible)
    {
        if (label != null) label.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
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

    private static string PersonalityText(Personality p) => p switch
    {
        Personality.Skittish   => "Asustadizo",
        Personality.Aggressive => "Agresivo",
        Personality.Lazy       => "Perezoso",
        Personality.Curious    => "Curioso",
        Personality.Social     => "Sociable",
        Personality.Grumpy     => "Gruñón",
        _                      => p.ToString(),
    };

    private static string StageText(int ageDays)
    {
        var table = BreedingController.Instance != null ? BreedingController.Instance.LifeStageTable : null;
        return table != null
            ? $"{table.Label(table.GetStage(ageDays))} · {ageDays}d"
            : $"{ageDays}d";
    }

    // Live mm:ss until the egg can hatch (server epoch ms); "¡Listo! [E]" once due.
    private static string CountdownText(long readyAtMs)
    {
        long left = readyAtMs - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return left <= 0 ? "¡Listo! [E]" : $"{TimeSpan.FromMilliseconds(left):mm\\:ss}";
    }
}
}
