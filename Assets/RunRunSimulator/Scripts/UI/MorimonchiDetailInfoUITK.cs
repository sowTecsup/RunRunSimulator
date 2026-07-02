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

    [Tooltip("Resolves equipped item IDs to their EquipmentSO (icon, rarity, effects) for the Equipo tab.")]
    [SerializeField] private EquipmentDatabaseSO equipmentDatabase;

    [Tooltip("Colores por rareza (pastel, nombre del ítem) y por slot (acento de la card).")]
    [SerializeField] private EquipmentPaletteSO equipmentPalette;

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

    // Equipo tab: portrait swatch + left card list + right stats breakdown.
    private VisualElement teamPortrait, equipStats;
    private ScrollView equipCards;

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

        teamPortrait = root.Q<VisualElement>("equip-portrait");
        equipCards   = root.Q<ScrollView>("equip-cards");
        equipStats   = root.Q<VisualElement>("equip-stats");

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

        // Final stat with its (base + bonus-from-parts/tier/equipment) breakdown.
        var baseEff = database != null
            ? CombatStats.GetEffectiveStats(dna, database)
            : new EffectiveStats(dna.BaseConstitution, dna.BaseAttack, dna.BaseSpeed, dna.BaseDefense, dna.BaseLuck, dna.BaseEvasion);
        var eff = EquipmentStats.Apply(baseEff, dna, equipmentDatabase);

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
        BuildEquipment(dna);
    }

    // ── Equipo tab (left = item cards, right = portrait + stats) ──

    private void BuildEquipment(CreatureDNA dna)
    {
        if (teamPortrait != null) teamPortrait.style.backgroundColor = dna.BaseColor;
        BuildEquipCards(dna);
        BuildEquipStats(dna);
    }

    private void BuildEquipCards(CreatureDNA dna)
    {
        if (equipCards == null) return;
        equipCards.Clear();
        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            AddEquipCard(dna, slot);
    }

    private void AddEquipCard(CreatureDNA dna, EquipmentSlot slot)
    {
        var item = ResolveEquip(dna, slot);

        var card = new VisualElement();
        card.AddToClassList("equip-card");
        if (item == null) card.AddToClassList("equip-card--empty");
        card.style.borderLeftColor = SlotColor(slot);

        // Diagonal accent (behind content) in the rarity color — added first so the
        // icon/info draw on top. Empty slots get no accent.
        if (item != null)
        {
            var diag = new VisualElement();
            diag.AddToClassList("equip-card__diag");
            diag.pickingMode = PickingMode.Ignore;
            var dc = RarityColor(item.Rarity);
            dc.a = 0.5f;
            diag.generateVisualContent += ctx => PaintDiagonal(ctx, dc);
            card.Add(diag);
        }

        var icon = new VisualElement();
        icon.AddToClassList("equip-card__icon");
        if (item != null && item.Icon != null)
            icon.style.backgroundImage = new StyleBackground(Background.FromSprite(item.Icon));
        else if (item != null)
            icon.style.backgroundColor = item.IconColor;
        card.Add(icon);

        var info = new VisualElement();
        info.AddToClassList("equip-card__info");

        var name = new Label(item != null
            ? (string.IsNullOrEmpty(item.Name) ? item.ID : item.Name)
            : $"{SlotName(slot)}: vacío");
        name.AddToClassList("equip-card__name");
        if (item != null) name.style.color = RarityColor(item.Rarity);
        info.Add(name);

        var meta = new Label(item != null ? $"{SlotName(item.Slot)} · {item.Rarity}" : "Sin equipar");
        meta.AddToClassList("equip-card__meta");
        info.Add(meta);

        if (item != null && !string.IsNullOrEmpty(item.Description))
        {
            var desc = new Label(item.Description);
            desc.AddToClassList("equip-card__desc");
            info.Add(desc);
        }

        if (item != null)
        {
            var effText = EffectsText(item);
            if (!string.IsNullOrEmpty(effText))
            {
                var eff = new Label(effText);
                eff.AddToClassList("equip-card__effects");
                info.Add(eff);
            }
        }

        card.Add(info);

        if (item != null)
        {
            var modsText = ModifiersText(item);
            if (!string.IsNullOrEmpty(modsText))
            {
                var procs = new VisualElement();
                procs.AddToClassList("equip-card__procs");
                procs.pickingMode = PickingMode.Ignore;

                var mods = new Label(modsText);
                mods.AddToClassList("equip-card__mods");
                procs.Add(mods);

                card.Add(procs);
            }
        }
        equipCards.Add(card);
    }

    private void BuildEquipStats(CreatureDNA dna)
    {
        if (equipStats == null) return;
        equipStats.Clear();

        var baseEff = database != null
            ? CombatStats.GetEffectiveStats(dna, database)
            : new EffectiveStats(dna.BaseConstitution, dna.BaseAttack, dna.BaseSpeed, dna.BaseDefense, dna.BaseLuck, dna.BaseEvasion);
        var finalEff = EquipmentStats.Apply(baseEff, dna, equipmentDatabase);

        AddStatRow("CON", baseEff.Constitution, finalEff.Constitution);
        AddStatRow("ATK", baseEff.Attack,       finalEff.Attack);
        AddStatRow("SPD", baseEff.Speed,        finalEff.Speed);
        AddStatRow("DEF", baseEff.Defense,      finalEff.Defense);
        AddStatRow("LCK", baseEff.Luck,         finalEff.Luck);
        AddStatRow("EVA", baseEff.Evasion,      finalEff.Evasion);
    }

    private void AddStatRow(string name, float baseVal, float finalVal)
    {
        var row = new VisualElement();
        row.AddToClassList("equip-stat");

        var n = new Label(name);
        n.AddToClassList("equip-stat__name");
        row.Add(n);

        float d = finalVal - baseVal;
        var v = new Label(Mathf.Approximately(d, 0f) ? $"{finalVal:0.#}" : $"{baseVal:0.#} → {finalVal:0.#}");
        v.AddToClassList("equip-stat__val");
        if (d > 0f)      v.AddToClassList("equip-stat__val--up");
        else if (d < 0f) v.AddToClassList("equip-stat__val--down");
        row.Add(v);

        equipStats.Add(row);
    }

    private EquipmentSO ResolveEquip(CreatureDNA dna, EquipmentSlot slot)
    {
        if (equipmentDatabase == null || dna.Equipped == null) return null;
        return dna.Equipped.TryGetValue(slot, out var id) ? equipmentDatabase.GetByID(id) : null;
    }

    // Draws the right-side diagonal wedge filled with the rarity color. Slants 45°
    // (bottom reaches further left), leaving the left side for the icon/text.
    private static void PaintDiagonal(MeshGenerationContext ctx, Color color)
    {
        var rect = ctx.visualElement.contentRect;
        float w = rect.width, h = rect.height;
        if (w <= 0f || h <= 0f) return;

        // Diagonal crosses the card's center → splits it into two equal halves.
        float topX = w * 0.5f - h * 0.5f;
        float botX = topX + h;

        var p = ctx.painter2D;
        p.fillColor = color;
        p.BeginPath();
        p.MoveTo(new Vector2(w - topX, 0f));
        p.LineTo(new Vector2(w, 0f));
        p.LineTo(new Vector2(w, h));
        p.LineTo(new Vector2(w - botX, h));
        p.ClosePath();
        p.Fill();
    }

    private Color RarityColor(Rarity r) =>
        equipmentPalette != null ? equipmentPalette.RarityColor(r) : BodyPart.RarityColor(r);

    private Color SlotColor(EquipmentSlot s) =>
        equipmentPalette != null ? equipmentPalette.SlotColor(s) : new Color(0.35f, 0.35f, 0.43f);

    private static string EffectsText(EquipmentSO item)
    {
        if (item.Effects == null) return null;
        var sb = new System.Text.StringBuilder();
        foreach (var e in item.Effects)
        {
            if (!(e is StatModifierEffect)) continue;
            if (sb.Length > 0) sb.Append('\n');
            sb.Append("• ").Append(e.Summary());
        }
        return sb.Length == 0 ? null : sb.ToString();
    }

    private static string ModifiersText(EquipmentSO item)
    {
        if (item.Effects == null) return null;
        var sb = new System.Text.StringBuilder();
        foreach (var e in item.Effects)
        {
            if (!(e is CombatProcEffect proc)) continue;
            if (sb.Length > 0) sb.Append('\n');
            string chance = proc.ProcChance >= 100 ? "" : $" · {proc.ProcChance}% proc";
            sb.Append("◆ ").Append(proc.Summary()).Append(chance);
        }
        return sb.Length == 0 ? null : sb.ToString();
    }

    private static string SlotName(EquipmentSlot s) => s switch
    {
        EquipmentSlot.Weapon => "Arma",
        EquipmentSlot.Armor  => "Armadura",
        EquipmentSlot.Amulet => "Amuleto",
        _                    => s.ToString(),
    };

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
