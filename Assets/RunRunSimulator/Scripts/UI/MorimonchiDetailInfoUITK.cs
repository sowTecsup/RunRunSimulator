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

    [Tooltip("Popup mochila para equipar desde la tab Equipo.")]
    [SerializeField] private EquipmentBackpackUITK backpack;

    [Tooltip("Draw order; higher keeps this modal above the grid panel.")]
    [SerializeField] private int sortingOrder = 100;

    // Queried once the document tree is built.
    private Label titleLabel, statCon, statAtk, statSpd, statDef, statLck, statEva, identityLabel, progressionLabel, roleElementLabel;
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

    // The creature currently on display, so a registry change (e.g. equipping
    // from the mochila) can re-run Populate and refresh the Equipo tab in place.
    private CreatureDNA current;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        if (document != null) document.sortingOrder = sortingOrder;
    }

    private void OnEnable()
    {
        UIManager.OnCreatureSelected += Show;
        GameEvents.OnRegistryChanged += OnRegistryChanged;
    }

    private void OnDisable()
    {
        UIManager.OnCreatureSelected -= Show;
        GameEvents.OnRegistryChanged -= OnRegistryChanged;
    }

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
        roleElementLabel = root.Q<Label>("role-element");
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
        current = dna;
        Wire();
        Populate(dna);
        if (tabs != null) tabs.selectedTabIndex = 0; // always open on the Info tab
        UIManager.RequestPanelSet(panel, true);
    }

    private void OnClose()
    {
        UIManager.RequestPanelSet(panel, false);
        backpack?.Close();
    }

    // Re-populates in place after any registry mutation (e.g. equipping an item
    // from the mochila), so the Equipo tab's cards and Base→Final stats stay
    // current. Populate never touches the selected tab, so the user stays put.
    private void OnRegistryChanged(CreatureRegistrySO _)
    {
        if (current != null && wired) Populate(current);
    }

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

        if (roleElementLabel != null)
            roleElementLabel.text = $"{RoleName(dna.Role)} · {ElementName(dna.Element)}\n{RoleDesc(dna.Role)}";

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

        card.RegisterCallback<ClickEvent>(_ =>
        {
            if (backpack != null) backpack.Open(dna, slot, card, registry);
        });

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
            if (!(e is ItemUseEffect use)) continue;
            if (sb.Length > 0) sb.Append('\n');
            sb.Append("◆ ").Append(use.Summary());
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

    private void BuildCombatHistory(CreatureDNA dna)
    {
        if (combatHistory == null) return;
        combatHistory.Clear();

        int count = dna.CombatHistory?.Count ?? 0;
        if (combatEmpty != null) combatEmpty.style.display = count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        if (count == 0) return;

        for (int i = count - 1; i >= 0; i--)   // newest first
            combatHistory.Add(BuildCombatCard(dna, dna.CombatHistory[i]));
    }

    private VisualElement BuildCombatCard(CreatureDNA dna, CombatRecord rec)
    {
        bool won  = rec.Outcome == CombatOutcome.Won;
        bool draw = rec.Outcome == CombatOutcome.Draw;

        var card = new VisualElement();
        card.AddToClassList("combat-card");
        card.AddToClassList(draw ? "combat-card--draw" : won ? "combat-card--win" : "combat-card--lose");

        var header = new VisualElement();
        header.AddToClassList("combat-card__header");

        var badge = new Label(BadgeText(rec.Outcome));
        badge.AddToClassList("combat-card__badge");
        badge.AddToClassList(draw ? "combat-card__badge--draw" : won ? "combat-card__badge--win" : "combat-card__badge--lose");
        header.Add(badge);

        var date = new Label($"{rec.Date.ToLocalTime():dd/MM/yyyy HH:mm}");
        date.AddToClassList("combat-card__date");
        header.Add(date);

        card.Add(header);

        string owner = string.IsNullOrEmpty(rec.OpponentPlayerName) ? " · local" : $" · de {rec.OpponentPlayerName}";
        var opponent = new Label($"vs {rec.OpponentName}{owner}");
        opponent.AddToClassList("combat-card__opponent");
        card.Add(opponent);

        if (rec.SelfStats != null && rec.OpponentStats != null)
        {
            var body = new VisualElement();
            body.AddToClassList("combat-card__body");
            body.Add(BuildCombatColumn("Tú", rec.SelfStats, dna.BaseColor));
            body.Add(BuildCombatColumn("Rival", rec.OpponentStats, new Color(0.22f, 0.22f, 0.28f)));
            card.Add(body);
        }
        else
        {
            var noStats = new Label("Combate antiguo — sin stats registradas");
            noStats.AddToClassList("combat-card__nostats");
            card.Add(noStats);
        }

        var comment = new Label(CommentText(rec));
        comment.AddToClassList("combat-card__comment");
        card.Add(comment);

        var footer = new VisualElement();
        footer.AddToClassList("combat-card__footer");

        var play = new Button(() => CombatReplayRequest.Request(dna, rec)) { text = "▶" };
        play.AddToClassList("combat-card__play");
        play.SetEnabled(CombatReplayRequest.CanReplay(dna, rec, registry));
        footer.Add(play);

        card.Add(footer);

        return card;
    }

    private static VisualElement BuildCombatColumn(string title, CombatFighterSnapshot s, Color fallbackColor)
    {
        var col = new VisualElement();
        col.AddToClassList("combat-card__colstats");

        var head = new VisualElement();
        head.AddToClassList("combat-card__colhead");

        var swatch = new VisualElement();
        swatch.AddToClassList("combat-card__swatch");
        swatch.style.backgroundColor = SnapshotColor(s, fallbackColor);
        head.Add(swatch);

        var titleLabel = new Label(title);
        titleLabel.AddToClassList("combat-card__col-title");
        head.Add(titleLabel);

        col.Add(head);

        var line1 = new Label($"HP {s.MaxHp:0} · ATK {s.Attack:0} · SPD {s.Speed:0}");
        line1.AddToClassList("combat-card__stat");
        col.Add(line1);

        var line2 = new Label($"DEF {s.Defense:0} · LCK {s.Luck:0} · EVA {s.Evasion:0}");
        line2.AddToClassList("combat-card__stat");
        col.Add(line2);

        AddTierChips(col, s);

        return col;
    }

    private static Color SnapshotColor(CombatFighterSnapshot s, Color fallback) =>
        !string.IsNullOrEmpty(s.ColorHex) && ColorUtility.TryParseHtmlString("#" + s.ColorHex, out var c)
            ? c
            : fallback;

    private static void AddTierChips(VisualElement col, CombatFighterSnapshot s)
    {
        var chips = new VisualElement();
        chips.AddToClassList("combat-card__chips");

        AddTierChip(chips, "Cuerpo", s.BodyTier);
        AddTierChip(chips, "Brazo",  s.ArmTier);
        AddTierChip(chips, "Ojo",    s.EyeTier);
        AddTierChip(chips, "Boca",   s.MouthTier);

        if (chips.childCount > 0) col.Add(chips);
    }

    private static void AddTierChip(VisualElement chips, string partEs, int tier)
    {
        if (tier <= 1) return;
        var chip = new Label($"{partEs} T{tier}");
        chip.AddToClassList("combat-card__tierchip");
        chips.Add(chip);
    }

    private static string CommentText(CombatRecord rec)
    {
        if (rec.Outcome == CombatOutcome.Won)
            return string.IsNullOrEmpty(rec.EvolvedSlot) ? "Victoria — sin mejora" : $"Se mejoró {PartEs(rec.EvolvedSlot)}";
        if (rec.Outcome == CombatOutcome.Lost)
            return rec.Died ? "Murió en combate" : "Regresó derrotado";
        return "Empate — sin consecuencias";
    }

    private static string PartEs(string slot) => slot switch
    {
        "Body"  => "Cuerpo",
        "Arm"   => "Brazo",
        "Eye"   => "Ojo",
        "Mouth" => "Boca",
        _       => slot,
    };

    private static string BadgeText(CombatOutcome o) => o switch
    {
        CombatOutcome.Won  => "Victoria",
        CombatOutcome.Lost => "Derrota",
        _                  => "Empate",
    };

    // ── Role / Element (Info tab) ───────────────────────────────────

    private static string RoleName(Role r) => r switch
    {
        Role.Protector => "Protector",
        Role.Agresivo  => "Agresivo",
        Role.Empatico  => "Empático",
        _              => r.ToString(),
    };

    private static string RoleDesc(Role r) => r switch
    {
        Role.Protector => "Guardián calmo; escuda a sus aliados y vive tranquilo cerca del almacén.",
        Role.Agresivo  => "Territorial; caza la retaguardia enemiga y vive en el bullicio del mostrador.",
        Role.Empatico  => "Sociable; cura a sus aliados y sigue al jugador por el mostrador.",
        _              => "",
    };

    private static string ElementName(Element e) => e switch
    {
        Element.Agua         => "Agua",
        Element.Fuego        => "Fuego",
        Element.Electricidad => "Electricidad",
        Element.Planta       => "Planta",
        _                    => e.ToString(),
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
