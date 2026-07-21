using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

// Visual prototype for the "Equipo 3v3" tab of the combat panel: a horizontal pool
// carousel of eligible MoriMonchis plus two facing 2-3-2 lineup boards
// (CombatLineupBoard), with pointer-driven drag and drop between pool/board/board.
// Side rosters mirror each board sorted by effective SPD, and a click (as opposed to
// a drag) on a pool/roster card opens a detail overlay with role/element chips,
// base→final stats and equipped items. Once both boards are full, "¡Pelear!" invokes
// CombatController.SimulateLocal with the boards' ids/rows and shows the result in an
// overlay. Sibling component of CombatPanelUITK, sharing the same GameObject and UIDocument.
[DisallowMultipleComponent]
public class CombatLineupUITK : MonoBehaviour
{
    [Header("UI Toolkit setup")]
    [SerializeField] private UIDocument document;

    [Header("Data")]
    [SerializeField] private CreatureDatabaseSO database;

    private enum OriginKind { Pool, Board }

    private struct DragOrigin
    {
        public OriginKind Kind;
        public CombatLineupBoard Board;
        public int Slot;
    }

    private const float DragThreshold = 8f;
    private const float GhostFallbackHalf = 30f;
    private static readonly int[] AutoSlots = { 0, 1, 3 };

    // ── UI refs ──
    private ScrollView pool;
    private VisualElement hostA, hostB;
    private VisualElement rosterA, rosterB;
    private Button btnAuto, btnClear, btnFight;
    private Label countA, countB, hint;

    // ── Detail overlay refs ──
    private VisualElement detailRoot, detailChips, detailStats, detailEquip;
    private Label detailName;
    private Button detailClose;

    // ── Result overlay refs ──
    private VisualElement resultRoot;
    private Label resultOutcome, resultSub;
    private ScrollView resultLog;
    private Button resultClose;

    // ── Boards ──
    private CombatLineupBoard boardA, boardB;

    // ── State ──
    private CreatureRegistrySO registry;
    private EquipmentDatabaseSO equipmentDatabase;
    private bool wired;

    private static readonly EquipmentSlot[] EquipmentSlots =
        { EquipmentSlot.Weapon, EquipmentSlot.Armor, EquipmentSlot.Amulet };

    private CreatureDNA dragDna;
    private DragOrigin dragOrigin;
    private VisualElement dragSource;
    private VisualElement ghost;
    private Vector2 dragDownPos;
    private int dragPointerId = -1;
    private bool dragActive;

    private VisualElement battlefield;
    private float slotSize;

    private CombatManagerSO Config => CombatController.Instance != null ? CombatController.Instance.Config : null;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        var gm = GameManager.Instance;
        registry = gm != null ? gm.Registry : null;
        equipmentDatabase = gm != null ? gm.EquipmentDatabase : null;
    }

    private void OnEnable()
    {
        GameEvents.OnRegistryChanged  += OnRegistry;
        GameEvents.OnRegistryReloaded += OnRegistry;
    }

    private void OnDisable()
    {
        GameEvents.OnRegistryChanged  -= OnRegistry;
        GameEvents.OnRegistryReloaded -= OnRegistry;
    }

    private void Start()
    {
        Wire();
    }

    private void OnDestroy()
    {
        if (btnAuto     != null) btnAuto.clicked     -= OnAuto;
        if (btnClear    != null) btnClear.clicked    -= OnClear;
        if (btnFight    != null) btnFight.clicked    -= OnFight;
        if (detailClose != null) detailClose.clicked -= HideDetail;
        if (resultClose != null) resultClose.clicked -= HideResult;
    }

    // ── Wiring ────────────────────────────────────────────────────

    private void Wire()
    {
        if (wired) return;
        var root = document != null ? document.rootVisualElement : null;
        if (root == null) return;

        pool     = root.Q<ScrollView>("lineup-pool");
        hostA    = root.Q<VisualElement>("lineup-board-a");
        hostB    = root.Q<VisualElement>("lineup-board-b");
        rosterA  = root.Q<VisualElement>("lineup-roster-a");
        rosterB  = root.Q<VisualElement>("lineup-roster-b");
        btnAuto  = root.Q<Button>("lineup-auto");
        btnClear = root.Q<Button>("lineup-clear");
        btnFight = root.Q<Button>("lineup-fight");
        countA   = root.Q<Label>("lineup-count-a");
        countB   = root.Q<Label>("lineup-count-b");
        hint     = root.Q<Label>("lineup-hint");

        detailRoot   = root.Q<VisualElement>("lineup-detail");
        detailName   = root.Q<Label>("lineup-detail-name");
        detailClose  = root.Q<Button>("lineup-detail-close");
        detailChips  = root.Q<VisualElement>("lineup-detail-chips");
        detailStats  = root.Q<VisualElement>("lineup-detail-stats");
        detailEquip  = root.Q<VisualElement>("lineup-detail-equip");

        resultRoot    = root.Q<VisualElement>("lineup-result");
        resultOutcome = root.Q<Label>("lineup-result-outcome");
        resultSub     = root.Q<Label>("lineup-result-sub");
        resultLog     = root.Q<ScrollView>("lineup-result-log");
        resultClose   = root.Q<Button>("lineup-result-close");

        if (hostA == null || hostB == null || pool == null) return;

        battlefield = UQueryExtensions.Q(root, null, "cbt-lu-battlefield");
        battlefield?.RegisterCallback<GeometryChangedEvent>(OnBattlefieldGeometryChanged);

        boardA = new CombatLineupBoard(hostA, mirrored: false);
        boardB = new CombatLineupBoard(hostB, mirrored: true);
        boardA.Changed += OnBoardChanged;
        boardB.Changed += OnBoardChanged;

        for (int i = 0; i < CombatLineupBoard.SlotCount; i++)
        {
            int slot = i;
            boardA.SlotElement(slot).RegisterCallback<PointerDownEvent>(evt => OnPointerDownSlot(evt, boardA, slot));
            boardB.SlotElement(slot).RegisterCallback<PointerDownEvent>(evt => OnPointerDownSlot(evt, boardB, slot));
        }

        if (btnAuto     != null) btnAuto.clicked     += OnAuto;
        if (btnClear    != null) btnClear.clicked    += OnClear;
        if (btnFight    != null) btnFight.clicked    += OnFight;
        if (detailClose != null) detailClose.clicked += HideDetail;
        if (resultClose != null) resultClose.clicked += HideResult;
        detailRoot?.RegisterCallback<ClickEvent>(evt => { if (evt.target == detailRoot) HideDetail(); });
        resultRoot?.RegisterCallback<ClickEvent>(evt => { if (evt.target == resultRoot) HideResult(); });
        pool.RegisterCallback<WheelEvent>(evt =>
        {
            pool.scrollOffset = new Vector2(pool.scrollOffset.x + evt.delta.y * 20f, 0);
            evt.StopPropagation();
        });

        wired = true;
        RebuildPool();
        RefreshCounts();
        RefreshRosters();
    }

    private void OnAuto()
    {
        boardA.Clear();
        boardB.Clear();

        var candidates = Eligible().OrderBy(_ => UnityEngine.Random.value).Take(CombatLineupBoard.TeamSize * 2).ToList();
        int idx = 0;
        foreach (var slot in AutoSlots)
        {
            if (idx >= candidates.Count) break;
            boardA.Place(candidates[idx], slot);
            idx++;
        }
        foreach (var slot in AutoSlots)
        {
            if (idx >= candidates.Count) break;
            boardB.Place(candidates[idx], slot);
            idx++;
        }
    }

    private void OnClear()
    {
        boardA.Clear();
        boardB.Clear();
    }

    private void OnBoardChanged()
    {
        RefreshCounts();
        RebuildPool();
        RefreshRosters();
    }

    private void OnFight()
    {
        if (!boardA.IsFull || !boardB.IsFull) return;
        if (CombatController.Instance == null) { Debug.LogWarning("[CombatLineup] No CombatController.Instance."); return; }

        var idsA = new List<string>();
        var rowsA = new List<int>();
        foreach (var (dna, slot) in boardA.Units)
        {
            idsA.Add(dna.UniqueID);
            rowsA.Add((int)CombatLineupBoard.RowOf(slot));
        }

        var idsB = new List<string>();
        var rowsB = new List<int>();
        foreach (var (dna, slot) in boardB.Units)
        {
            idsB.Add(dna.UniqueID);
            rowsB.Add((int)CombatLineupBoard.RowOf(slot));
        }

        var result = CombatController.Instance.SimulateLocal(idsA, idsB, rowsA, rowsB);
        if (result == null) { Debug.LogWarning("[CombatLineup] SimulateLocal returned null."); return; }

        ShowResult(result);
    }

    private void ShowResult(CombatResult result)
    {
        if (resultRoot == null) return;

        if (resultOutcome != null)
        {
            resultOutcome.text = result.IsDraw ? "Empate" : (result.TeamAWon ? "¡Ganó el Equipo A!" : "¡Ganó el Equipo B!");
            resultOutcome.EnableInClassList("cbt-lu-result-outcome--a", !result.IsDraw && result.TeamAWon);
            resultOutcome.EnableInClassList("cbt-lu-result-outcome--b", !result.IsDraw && !result.TeamAWon);
        }

        if (resultSub != null)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(result.EvolvedUnitName)) parts.Add($"Evolucionó {result.EvolvedUnitName}");
            if (!string.IsNullOrEmpty(result.DiedUnitName)) parts.Add($"Murió {result.DiedUnitName}");
            resultSub.text = string.Join(" · ", parts);
        }

        if (resultLog != null)
        {
            resultLog.Clear();
            foreach (var line in result.Log)
            {
                var l = new Label(line);
                l.AddToClassList("cbt-log-line");
                resultLog.Add(l);
            }
        }

        resultRoot.style.display = DisplayStyle.Flex;
    }

    private void HideResult()
    {
        if (resultRoot != null) resultRoot.style.display = DisplayStyle.None;
    }

    private void OnBattlefieldGeometryChanged(GeometryChangedEvent evt)
    {
        if (boardA == null || boardB == null) return;

        float h = battlefield.resolvedStyle.height;
        float w = battlefield.resolvedStyle.width;
        if (h <= 0f || w <= 0f) return;

        float limH = (h - 24f) / 3f - 10f;
        float limW = (w - 90f) / 6f - 12f;
        float size = Mathf.Clamp(Mathf.Min(limH, limW), 90f, 190f);

        if (Mathf.Abs(size - slotSize) < 1f) return;
        slotSize = size;
        boardA.SetSlotSize(size);
        boardB.SetSlotSize(size);
    }

    // ── Data / events ─────────────────────────────────────────────

    private void OnRegistry(CreatureRegistrySO reg)
    {
        registry = reg;
        if (!wired) { Wire(); return; }
        PruneInvalid();
        RebuildPool();
    }

    private IEnumerable<CreatureDNA> Eligible() =>
        registry == null ? Enumerable.Empty<CreatureDNA>()
        : registry.GetAll().Values
            .Where(d => !d.IsDead && !d.IsBusy && d.FightCount < MaxFights())
            .OrderBy(d => d.CustomName);

    private int MaxFights() => Config != null ? Config.MaxFightCount : 5;

    private void PruneInvalid()
    {
        var eligibleIds = new HashSet<string>(Eligible().Select(d => d.UniqueID));
        PruneBoard(boardA, eligibleIds);
        PruneBoard(boardB, eligibleIds);
    }

    private void PruneBoard(CombatLineupBoard board, HashSet<string> eligibleIds)
    {
        var toRemove = new List<int>();
        foreach (var (dna, slot) in board.Units)
            if (!eligibleIds.Contains(dna.UniqueID)) toRemove.Add(slot);
        foreach (var slot in toRemove) board.RemoveAt(slot);
    }

    private void RebuildPool()
    {
        if (pool == null) return;
        pool.Clear();
        if (registry == null) return;

        var placed = new HashSet<string>(boardA.PlacedIds);
        placed.UnionWith(boardB.PlacedIds);

        foreach (var dna in Eligible())
        {
            if (placed.Contains(dna.UniqueID)) continue;
            pool.Add(BuildCandidateCard(dna));
        }
    }

    private VisualElement BuildCandidateCard(CreatureDNA dna)
    {
        var card = new VisualElement();
        card.AddToClassList("cbt-lu-card");
        card.userData = dna.UniqueID;

        var top = new VisualElement();
        top.AddToClassList("cbt-lu-card-top");
        card.Add(top);

        var swatch = new VisualElement();
        swatch.AddToClassList("cbt-lu-swatch");
        MonchiPortraitUI.Apply(swatch, dna);
        top.Add(swatch);

        var name = new Label(dna.CustomName);
        name.AddToClassList("cbt-lu-card-name");
        top.Add(name);
        top.Add(FightsLabel(dna));

        var chips = new VisualElement();
        chips.AddToClassList("cbt-lu-card-chips");
        card.Add(chips);

        var roleChip = new Label(RoleText(dna.Role));
        roleChip.AddToClassList("cbt-lu-role");
        roleChip.AddToClassList(RoleModifierClass(dna.Role));
        chips.Add(roleChip);

        var elemChip = new Label(ElementText(dna.Element));
        elemChip.AddToClassList("cbt-lu-role");
        elemChip.AddToClassList("cbt-lu-elem");
        chips.Add(elemChip);

        card.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button == 1) { ShowDetail(dna); return; }
            OnPointerDown(evt, card, dna, new DragOrigin { Kind = OriginKind.Pool });
        });
        return card;
    }

    private EffectiveStats StatsOf(CreatureDNA dna)
    {
        var config = Config;
        var baseStats = database != null
            ? (config != null ? CombatStats.GetEffectiveStats(dna, database, config.Roles) : CombatStats.GetEffectiveStats(dna, database))
            : new EffectiveStats(dna.BaseConstitution, dna.BaseAttack, dna.BaseSpeed, dna.BaseDefense, dna.BaseLuck, dna.BaseEvasion);
        return equipmentDatabase != null ? EquipmentStats.Apply(baseStats, dna, equipmentDatabase) : baseStats;
    }

    private Label FightsLabel(CreatureDNA dna)
    {
        int max = MaxFights();
        var l = new Label($"{Mathf.Max(0, max - dna.FightCount)}/{max}");
        l.AddToClassList("cbt-lu-fights");
        return l;
    }

    private static string RoleText(Role role)
    {
        switch (role)
        {
            case Role.Protector: return "PRO";
            case Role.Agresivo:  return "AGR";
            default:             return "EMP";
        }
    }

    private static string RoleModifierClass(Role role)
    {
        switch (role)
        {
            case Role.Protector: return "cbt-lu-role--protector";
            case Role.Agresivo:  return "cbt-lu-role--agresivo";
            default:             return "cbt-lu-role--empatico";
        }
    }

    private static string ElementText(Element element)
    {
        switch (element)
        {
            case Element.Agua:         return "Agua";
            case Element.Fuego:        return "Fuego";
            case Element.Electricidad: return "Electricidad";
            case Element.Planta:       return "Planta";
            default:                   return element.ToString();
        }
    }

    private void RefreshCounts()
    {
        if (countA != null) countA.text = $"{boardA.Count}/{CombatLineupBoard.TeamSize}";
        if (countB != null) countB.text = $"{boardB.Count}/{CombatLineupBoard.TeamSize}";
        bool ready = boardA.IsFull && boardB.IsFull;
        if (hint != null) hint.text = ready ? "¡Listos para pelear!" : "Arrastrá un MoriMochi a un slot";
        btnFight?.SetEnabled(ready);
    }

    private void RefreshRosters()
    {
        BuildRoster(rosterA, boardA);
        BuildRoster(rosterB, boardB);
    }

    private void BuildRoster(VisualElement container, CombatLineupBoard board)
    {
        if (container == null) return;
        container.Clear();

        var ordered = board.Units
            .OrderByDescending(u => StatsOf(u.Dna).Speed)
            .ThenBy(u => u.Dna.CustomName)
            .ToList();

        foreach (var (dna, slot) in ordered)
            container.Add(BuildRosterCard(dna));

        for (int i = ordered.Count; i < CombatLineupBoard.TeamSize; i++)
            container.Add(BuildEmptyRosterCard());
    }

    private VisualElement BuildRosterCard(CreatureDNA dna)
    {
        var card = new VisualElement();
        card.AddToClassList("cbt-lu-roster-card");

        var color = dna.BaseColor;
        card.style.borderTopColor    = color;
        card.style.borderBottomColor = color;
        card.style.borderLeftColor   = color;
        card.style.borderRightColor  = color;

        var nameRow = new VisualElement();
        nameRow.AddToClassList("cbt-lu-name-row");
        card.Add(nameRow);

        var name = new Label(dna.CustomName);
        name.AddToClassList("cbt-lu-roster-name");
        nameRow.Add(name);
        nameRow.Add(FightsLabel(dna));

        var meta = new VisualElement();
        meta.AddToClassList("cbt-lu-roster-meta");
        card.Add(meta);

        var roleChip = new Label(RoleText(dna.Role));
        roleChip.AddToClassList("cbt-lu-role");
        roleChip.AddToClassList(RoleModifierClass(dna.Role));
        meta.Add(roleChip);

        var stats = new Label($"CON {Mathf.RoundToInt(dna.BaseConstitution)}  ATK {Mathf.RoundToInt(dna.BaseAttack)}  SPD {Mathf.RoundToInt(dna.BaseSpeed)}");
        stats.AddToClassList("cbt-lu-roster-stats");
        card.Add(stats);

        card.RegisterCallback<PointerDownEvent>(evt => { if (evt.button == 1) ShowDetail(dna); });
        return card;
    }

    private static VisualElement BuildEmptyRosterCard()
    {
        var card = new VisualElement();
        card.AddToClassList("cbt-lu-roster-card");
        card.AddToClassList("cbt-lu-roster-card--empty");

        var label = new Label("—");
        card.Add(label);
        return card;
    }

    // ── Detail overlay ────────────────────────────────────────────

    private void ShowDetail(CreatureDNA dna)
    {
        if (detailRoot == null || dna == null) return;

        if (detailName != null) detailName.text = dna.CustomName;

        if (detailChips != null)
        {
            detailChips.Clear();

            var roleChip = new Label(RoleText(dna.Role));
            roleChip.AddToClassList("cbt-lu-role");
            roleChip.AddToClassList(RoleModifierClass(dna.Role));
            detailChips.Add(roleChip);

            var elemChip = new Label(ElementText(dna.Element));
            elemChip.AddToClassList("cbt-lu-role");
            elemChip.AddToClassList("cbt-lu-elem");
            detailChips.Add(elemChip);
        }

        BuildDetailStats(dna);
        BuildDetailEquip(dna);

        detailRoot.style.display = DisplayStyle.Flex;
    }

    private void HideDetail()
    {
        if (detailRoot != null) detailRoot.style.display = DisplayStyle.None;
    }

    private void BuildDetailStats(CreatureDNA dna)
    {
        if (detailStats == null) return;
        detailStats.Clear();

        var final = StatsOf(dna);
        AddDetailStatRow(detailStats, "CON", dna.BaseConstitution, final.Constitution);
        AddDetailStatRow(detailStats, "ATK", dna.BaseAttack,       final.Attack);
        AddDetailStatRow(detailStats, "SPD", dna.BaseSpeed,        final.Speed);
        AddDetailStatRow(detailStats, "DEF", dna.BaseDefense,      final.Defense);
        AddDetailStatRow(detailStats, "LCK", dna.BaseLuck,         final.Luck);
        AddDetailStatRow(detailStats, "EVA", dna.BaseEvasion,      final.Evasion);
    }

    private static void AddDetailStatRow(VisualElement container, string label, float baseVal, float finalVal)
    {
        var row = new VisualElement();
        row.AddToClassList("cbt-lu-stat-row");

        var nameLabel = new Label(label);
        nameLabel.AddToClassList("cbt-lu-stat-name");
        row.Add(nameLabel);

        int b = Mathf.RoundToInt(baseVal);
        int f = Mathf.RoundToInt(finalVal);
        var valLabel = new Label($"{b} → {f}");
        valLabel.AddToClassList("cbt-lu-stat-val");
        if (f > b) valLabel.AddToClassList("cbt-lu-stat-val--buff");
        row.Add(valLabel);

        container.Add(row);
    }

    private void BuildDetailEquip(CreatureDNA dna)
    {
        if (detailEquip == null) return;
        detailEquip.Clear();

        foreach (var slot in EquipmentSlots)
        {
            var row = new VisualElement();
            row.AddToClassList("cbt-lu-equip-row");

            var slotLabel = new Label(SlotName(slot));
            slotLabel.AddToClassList("cbt-lu-equip-slot");
            row.Add(slotLabel);

            string itemId = dna.Equipped != null && dna.Equipped.TryGetValue(slot, out var id) ? id : null;
            string itemName = "—";
            if (!string.IsNullOrEmpty(itemId))
            {
                var item = equipmentDatabase != null ? equipmentDatabase.GetByID(itemId) : null;
                itemName = item != null ? (string.IsNullOrEmpty(item.Name) ? item.ID : item.Name) : itemId;
            }

            var nameLabel = new Label(itemName);
            nameLabel.AddToClassList("cbt-lu-equip-name");
            row.Add(nameLabel);

            detailEquip.Add(row);
        }
    }

    private static string SlotName(EquipmentSlot s) => s switch
    {
        EquipmentSlot.Weapon => "Arma",
        EquipmentSlot.Armor  => "Armadura",
        EquipmentSlot.Amulet => "Amuleto",
        _                    => s.ToString(),
    };

    // ── Drag & drop ───────────────────────────────────────────────

    private void OnPointerDownSlot(PointerDownEvent evt, CombatLineupBoard board, int slot)
    {
        var dna = board.GetAt(slot);
        if (dna == null) return;
        if (evt.button == 1) { ShowDetail(dna); return; }
        OnPointerDown(evt, board.SlotElement(slot), dna, new DragOrigin { Kind = OriginKind.Board, Board = board, Slot = slot });
    }

    private void OnPointerDown(PointerDownEvent evt, VisualElement source, CreatureDNA dna, DragOrigin origin)
    {
        if (evt.button != 0 || dragPointerId != -1) return;

        dragDna = dna;
        dragOrigin = origin;
        dragSource = source;
        dragDownPos = evt.position;
        dragPointerId = evt.pointerId;
        dragActive = false;

        source.CapturePointer(evt.pointerId);
        source.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        source.RegisterCallback<PointerUpEvent>(OnPointerUp);
        source.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (evt.pointerId != dragPointerId) return;

        if (!dragActive)
        {
            if (Vector2.Distance(evt.position, dragDownPos) < DragThreshold) return;
            StartDrag();
        }

        PositionGhost(evt.position);
        HandleHover(evt.position);
    }

    private void StartDrag()
    {
        dragActive = true;

        ghost = new VisualElement();
        ghost.AddToClassList("cbt-lu-ghost");
        ghost.pickingMode = PickingMode.Ignore;
        MonchiPortraitUI.Apply(ghost, dragDna);
        if (slotSize > 0f)
        {
            ghost.style.width = slotSize;
            ghost.style.height = slotSize;
        }

        var label = new Label(dragDna.CustomName);
        label.AddToClassList("cbt-lu-unit-name");
        ghost.Add(label);

        document.rootVisualElement.Add(ghost);
        dragSource.AddToClassList("cbt-lu-dragging");
    }

    private void PositionGhost(Vector2 panelPos)
    {
        var root = document.rootVisualElement;
        var local = root.WorldToLocal(panelPos);
        float halfW = ghost.resolvedStyle.width > 0f ? ghost.resolvedStyle.width * 0.5f : GhostFallbackHalf;
        float halfH = ghost.resolvedStyle.height > 0f ? ghost.resolvedStyle.height * 0.5f : GhostFallbackHalf;
        ghost.style.left = local.x - halfW;
        ghost.style.top = local.y - halfH;
    }

    private void HandleHover(Vector2 panelPos)
    {
        int slotA = boardA.SlotAtPosition(panelPos);
        int slotB = boardB.SlotAtPosition(panelPos);

        if (slotA >= 0)
        {
            boardA.SetDropHighlight(slotA, IsValidDrop(boardA, slotA));
            boardB.ClearHighlight();
        }
        else if (slotB >= 0)
        {
            boardB.SetDropHighlight(slotB, IsValidDrop(boardB, slotB));
            boardA.ClearHighlight();
        }
        else
        {
            boardA.ClearHighlight();
            boardB.ClearHighlight();
        }
    }

    private bool IsValidDrop(CombatLineupBoard board, int slot)
    {
        if (board.GetAt(slot) != null) return false;
        if (dragOrigin.Kind == OriginKind.Board && dragOrigin.Board == board) return true;
        return board.CanPlace(slot);
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (evt.pointerId != dragPointerId) return;

        UnregisterDragCallbacks();
        if (dragSource.HasPointerCapture(evt.pointerId)) dragSource.ReleasePointer(evt.pointerId);

        if (dragActive) ResolveDrop(evt.position);
        EndDrag();
    }

    private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
    {
        if (evt.pointerId != dragPointerId) return;
        UnregisterDragCallbacks();
        EndDrag();
    }

    private void UnregisterDragCallbacks()
    {
        if (dragSource == null) return;
        dragSource.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        dragSource.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        dragSource.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
    }

    private void ResolveDrop(Vector2 panelPos)
    {
        int slotA = boardA.SlotAtPosition(panelPos);
        int slotB = boardB.SlotAtPosition(panelPos);

        if (slotA >= 0 && IsValidDrop(boardA, slotA))
        {
            RemoveFromOrigin();
            boardA.Place(dragDna, slotA);
            return;
        }
        if (slotB >= 0 && IsValidDrop(boardB, slotB))
        {
            RemoveFromOrigin();
            boardB.Place(dragDna, slotB);
            return;
        }
        if (slotA < 0 && slotB < 0 && dragOrigin.Kind == OriginKind.Board)
        {
            dragOrigin.Board.RemoveAt(dragOrigin.Slot);
        }
    }

    private void RemoveFromOrigin()
    {
        if (dragOrigin.Kind == OriginKind.Board) dragOrigin.Board.RemoveAt(dragOrigin.Slot);
    }

    private void EndDrag()
    {
        if (ghost != null) { ghost.RemoveFromHierarchy(); ghost = null; }
        if (dragSource != null) dragSource.RemoveFromClassList("cbt-lu-dragging");
        boardA.ClearHighlight();
        boardB.ClearHighlight();

        dragDna = null;
        dragSource = null;
        dragPointerId = -1;
        dragActive = false;
    }
}
}
