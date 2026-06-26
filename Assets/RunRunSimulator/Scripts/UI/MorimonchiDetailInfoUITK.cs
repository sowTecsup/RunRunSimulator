using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

// Detailed MoriMochi summary window (UI Toolkit), FireRed-summary inspired.
// Lives on the always-active UIManager object and fills its own UIDocument
// (kept active, hidden via display). Opened when a grid card is clicked.
//
// Modal: its full-screen backdrop sits above the grid (higher sortingOrder) and
// captures clicks, so the panel behind can't be touched until the X closes it.
//
// Event-driven: it never references the grid. It listens to UIManager's static
// OnCreatureSelected — the event carries the creature AND the registry (for
// parent names). Parts/sets/rarity and effective stats are resolved against the
// shared CreatureDatabaseSO, exactly like CreatureDNA.GetDisplayName(db) does.
[DisallowMultipleComponent]
public partial class MorimonchiDetailInfoUITK : MonoBehaviour, IUINavigable
{
    [Header("UI Toolkit setup")]
    [SerializeField] private UIDocument document;
    [SerializeField] private UIPanelType panel = UIPanelType.MorimonchiDetail;

    [Header("Data")]
    [Tooltip("Resolves part names/sets/rarity and effective stats. Shared SO asset.")]
    [SerializeField] private CreatureDatabaseSO database;

    [Tooltip("Draw order; higher keeps this modal above the grid panel.")]
    [SerializeField] private int sortingOrder = 100;

    // Queried once the document tree is built.
    private Label titleLabel, statCon, statAtk, statSpd, statDef, statLck, statEva, identityLabel, progressionLabel, personalityLabel;
    private VisualElement portrait, partsContainer, lineageTree, breedTree;
    private ScrollView combatHistory;
    private Label combatEmpty, lineageEmpty, breedEmpty;
    private TabView tabs;
    private Button closeButton;
    private bool wired;

    // Kept from the latest Show() so the Combate/Linaje tabs can resolve opponents
    // and ancestors by ID (the event carries it — the panel never touches the grid).
    private CreatureRegistrySO registry;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        if (document != null) document.sortingOrder = sortingOrder;
    }

    private void OnEnable()  => UIManager.OnCreatureSelected += Show;
    private void OnDisable() => UIManager.OnCreatureSelected -= Show;

    // Register as the focused-input handler in Start: UIManager subscribes to the
    // registration events in OnEnable, which always runs before any Start.
    private void Start()
    {
        Wire();
        UIManager.RegisterNavigable(panel, this);
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.clicked -= OnClose;
        UIManager.UnregisterNavigable(panel);
    }

    // ── IUINavigable (routed only while this panel is the focused top) ──

    // A/D (or stick/dpad) steps through the tabs.
    public void OnUINavigate(Vector2 dir)
    {
        if (tabs == null) return;
        int last = tabs.Query<Tab>().ToList().Count - 1;
        if (last < 0) return;

        if (dir.x > 0.5f)       tabs.selectedTabIndex = Mathf.Min(tabs.selectedTabIndex + 1, last);
        else if (dir.x < -0.5f) tabs.selectedTabIndex = Mathf.Max(tabs.selectedTabIndex - 1, 0);
    }

    // Nothing to confirm on the summary page yet.
    public void OnUISubmit() { }

    // No internal back state — let the UIManager close the panel on ESC.
    public bool OnUICancel() => false;

    // ── Private Methods ───────────────────────────────────────────

    private void Wire()
    {
        if (wired) return;
        var root = document != null ? document.rootVisualElement : null;
        if (root == null) return;

        titleLabel       = root.Q<Label>("title");
        portrait         = root.Q<VisualElement>("portrait");
        statCon          = root.Q<Label>("stat-con");
        statAtk          = root.Q<Label>("stat-atk");
        statSpd          = root.Q<Label>("stat-spd");
        statDef          = root.Q<Label>("stat-def");
        statLck          = root.Q<Label>("stat-lck");
        statEva          = root.Q<Label>("stat-eva");
        identityLabel    = root.Q<Label>("identity");
        personalityLabel = root.Q<Label>("personality");
        partsContainer   = root.Q<VisualElement>("parts");
        progressionLabel = root.Q<Label>("progression");
        combatHistory    = root.Q<ScrollView>("combat-history");
        combatEmpty      = root.Q<Label>("combat-empty");
        lineageTree      = root.Q<VisualElement>("lineage-tree");
        lineageEmpty     = root.Q<Label>("lineage-empty");
        breedTree        = root.Q<VisualElement>("breed-tree");
        breedEmpty       = root.Q<Label>("breed-empty");
        tabs             = root.Q<TabView>("tabs");

        closeButton = root.Q<Button>("close-button");
        if (closeButton != null) closeButton.clicked += OnClose;

        wired = true;
    }

    // Populate then show. Repopulates if already open (clicking another card).
    // The registry rides along in the event so the Linaje tab can resolve ancestors
    // by ID (kept in a field for the tab builders below).
    private void Show(CreatureDNA dna, CreatureRegistrySO registry)
    {
        this.registry = registry;
        Wire();
        Populate(dna);
        if (tabs != null) tabs.selectedTabIndex = 0; // always open on the Info tab
        UIManager.RequestPanelSet(panel, true);
    }

    private void OnClose() => UIManager.RequestPanelSet(panel, false);

    private void Populate(CreatureDNA dna)
    {
        if (dna == null) return;

        if (titleLabel != null)
            titleLabel.text = string.IsNullOrEmpty(dna.CustomName) ? dna.ToStringID() : dna.CustomName;

        if (portrait != null)
            portrait.style.backgroundColor = dna.BaseColor;

        // Final stat with its (base + bonus-from-parts/tier) breakdown.
        var eff = database != null
            ? CombatService.GetEffectiveStats(dna, database)
            : new CombatService.EffectiveStats(dna.BaseConstitution, dna.BaseAttack, dna.BaseSpeed, dna.BaseDefense, dna.BaseLuck, dna.BaseEvasion);

        SetStat(statCon, "CON", eff.Constitution, dna.BaseConstitution);
        SetStat(statAtk, "ATK", eff.Attack,       dna.BaseAttack);
        SetStat(statSpd, "SPD", eff.Speed,        dna.BaseSpeed);
        SetStat(statDef, "DEF", eff.Defense,      dna.BaseDefense);
        SetStat(statLck, "LCK", eff.Luck,         dna.BaseLuck);
        SetStat(statEva, "EVA", eff.Evasion,      dna.BaseEvasion);

        if (identityLabel != null)
            identityLabel.text = $"Género: {dna.Gender}\nEstado: {StateOf(dna)}\nNacimiento: {Born(dna)}";

        if (personalityLabel != null)
            personalityLabel.text = $"{PersonalityName(dna.Personality)}\n{PersonalityDesc(dna.Personality)}";

        BuildParts(dna);

        if (progressionLabel != null)
            progressionLabel.text = $"Combates: {dna.FightCount} ({dna.WinCount} victorias)\nCrías: {dna.BreedCount}";

        BuildCombatHistory(dna);
        BuildLineage(dna);
        BuildBreed(dna);
    }

    private static void SetStat(Label label, string name, float final, float baseVal)
    {
        if (label == null) return;
        float bonus = final - baseVal;
        label.text = $"{name}  {final:0}   ({baseVal:0} + {bonus:0})";
    }

    private void BuildParts(CreatureDNA dna)
    {
        if (partsContainer == null) return;
        partsContainer.Clear();
        if (database == null) return;

        AddPartRow("Cuerpo", database.GetBodyShape(dna.BodyShapeID), dna.BodyTier);
        AddPartRow("Brazos", database.GetArm(dna.ArmID),             dna.ArmTier);
        AddPartRow("Ojos",   database.GetEye(dna.EyeID),             dna.EyeTier);
        AddPartRow("Boca",   database.GetMouth(dna.MouthID),         dna.MouthTier);
    }

    private void AddPartRow(string slot, BodyPart part, Tier tier)
    {
        var row = new VisualElement();
        row.AddToClassList("part-row");

        var swatch = new VisualElement();
        swatch.AddToClassList("part-swatch");
        swatch.style.backgroundColor = part != null ? BodyPart.SetColor(part.Set) : Color.gray;
        row.Add(swatch);

        var text = new Label();
        text.AddToClassList("part-text");
        text.text = part != null
            ? $"{slot}: {part.Name}  ·  {part.Set} · {part.Rarity} · Tier{(int)tier}"
            : $"{slot}: —";
        row.Add(text);

        partsContainer.Add(row);
    }

    // ── Combat tab ────────────────────────────────────────────────

    // One collapsible foldout per finished fight, newest first. Reads the same
    // stored CombatHistory the combat panel's Historial tab uses.
    private void BuildCombatHistory(CreatureDNA dna)
    {
        if (combatHistory == null) return;
        combatHistory.Clear();

        int count = dna.CombatHistory?.Count ?? 0;
        if (combatEmpty != null) combatEmpty.style.display = count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        if (count == 0) return;

        for (int i = count - 1; i >= 0; i--)   // newest first
        {
            var rec  = dna.CombatHistory[i];
            bool won  = rec.Outcome == CombatOutcome.Won;
            bool draw = rec.Outcome == CombatOutcome.Draw;

            var fold = new Foldout { value = false };
            fold.AddToClassList("combat-fold");
            if (!draw) fold.AddToClassList(won ? "combat-fold--win" : "combat-fold--lose");
            fold.text = $"{OutcomeShort(rec.Outcome, rec.Died)}  ·  vs {rec.OpponentName}";

            string oppPlayer = string.IsNullOrEmpty(rec.OpponentPlayerName) ? "" : $"  ·  {rec.OpponentPlayerName}";
            string evolved   = won && !string.IsNullOrEmpty(rec.EvolvedSlot) ? $"  ·  evolucionó {rec.EvolvedSlot}" : "";
            string died      = rec.Died ? "  ·  murió" : "";
            var meta = new Label($"{rec.Date.ToLocalTime():dd/MM/yyyy HH:mm}{oppPlayer}{evolved}{died}");
            meta.AddToClassList("combat-meta");
            fold.Add(meta);

            if (rec.Turns != null)
                foreach (var t in rec.Turns)
                {
                    var l = new Label(
                        $"R{t.TurnNumber}  {t.AttackerName} → {t.DefenderName}  ·  {t.Damage:0}{(t.WasCrit ? " ¡CRIT!" : "")}  ·  HP {t.DefenderHpAfter:0}");
                    l.AddToClassList("combat-turn");
                    fold.Add(l);
                }

            combatHistory.Add(fold);
        }
    }

    private static string OutcomeShort(CombatOutcome o, bool died) => o switch
    {
        CombatOutcome.Won  => "Ganó",
        CombatOutcome.Lost => died ? "Murió" : "Perdió",
        _                  => "Empate",
    };

    // ── Personality (Info tab) ────────────────────────────────────

    private static string PersonalityName(Personality p) => p switch
    {
        Personality.Skittish   => "Asustadizo",
        Personality.Aggressive => "Agresivo",
        Personality.Lazy       => "Perezoso",
        Personality.Curious    => "Curioso",
        Personality.Social     => "Sociable",
        Personality.Grumpy     => "Gruñón",
        _                      => p.ToString(),
    };

    private static string PersonalityDesc(Personality p) => p switch
    {
        Personality.Skittish   => "Ráfagas nerviosas; huye y se esconde en el Almacén.",
        Personality.Aggressive => "Territorial; se acerca y vive en el bullicio del mostrador.",
        Personality.Lazy       => "Apenas se mueve; descansa mucho en la Trastienda.",
        Personality.Curious    => "Vaga por todos lados; se acerca al jugador y a los objetos.",
        Personality.Social     => "Busca compañía; sigue al jugador por el mostrador.",
        Personality.Grumpy     => "Solitario; mantiene distancia y se retira al Almacén.",
        _                      => "",
    };

    private static string StateOf(CreatureDNA d) =>
        d.IsSold                                  ? "SOLD"     :
        d.IsDead                                  ? "DEAD"     :
        d.BusyState == BusyReason.Breeding        ? "Breeding" :
        d.BusyState == BusyReason.QueuedForCombat ? "In Queue" :
        "Free";

    private static string Born(CreatureDNA d) =>
        d.BirthDate == default ? "—" : d.BirthDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
}
}
