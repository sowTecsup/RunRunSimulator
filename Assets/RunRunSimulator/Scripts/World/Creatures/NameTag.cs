using System;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

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
        if (Camera.main != null) cam = Camera.main.transform;
        baseLocalPos   = transform.localPosition;
        baseLocalScale = transform.localScale;
    }

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

    private void ResolveElements()
    {
        var docRoot = UiPanels.RootOf(document);
        if (docRoot == null) return;
        if (docRoot == root && nameLabel != null) return;

        root = docRoot;
        root.style.alignItems     = Align.Center;
        root.style.justifyContent = Justify.Center;
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

        float distSqr = (cam.position - transform.position).sqrMagnitude;
        bool  visible = distSqr <= showDistance * showDistance;
        if (visible != shown) SetShown(visible);
        if (!visible) return;

        Refresh();

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

        if      (agent != null && agent.IsForSale) RefreshStore();
        else if (agent != null && agent.IsPenned)  RefreshPenned();
        else                                       RefreshDefault();
    }

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

    private void RefreshDefault()
    {
        SetDisplay(priceLabel,  false);
        SetDisplay(genderLabel, false);
        SetDisplay(roleLabel,   false);
        SetDisplay(breedLabel,  false);
        SetDisplay(heartLabel,  false);
        SetDisplay(timerLabel,  false);
        SetDisplay(stageLabel,  false);

        bool isBeingPetted = agent != null && agent.IsBeingPetted;
        bool showPetHint   = isBeingPetted ||
                              (agent != null && !dna.IsDead &&
                               agent.IsInFriendlyReaction && agent.IsPlayerFacingMe());

        var (statusText, statusColor) = StatusOf(dna);
        bool showStatus = !showPetHint && !string.IsNullOrEmpty(statusText);

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

    private static (string, Color) StatusOf(CreatureDNA dna)
    {
        if (dna.IsDead) return (Loc.Tr("status.dead"), new Color(1f, 0.39f, 0.39f));
        return dna.BusyState switch
        {
            BusyReason.Breeding => (Loc.Tr("status.breeding"), new Color(1f, 0.61f, 0.82f)),
            _                   => ("", Color.clear),
        };
    }

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

    private static string CountdownText(long readyAtMs)
    {
        long left = readyAtMs - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return left <= 0 ? Loc.Tr("nametag.ready") : $"{TimeSpan.FromMilliseconds(left):mm\\:ss}";
    }
}
}
