using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

// UI Toolkit twin of CreatureGridUI. Same contract, different rendering tech:
// instead of instantiating uGUI prefabs under a GridLayoutGroup, it clones a
// UXML card template (VisualTreeAsset) into a VisualElement container.
//
// Lives on the always-active UIManager object (NOT on the UIDocument object) and
// references the UIDocument it fills. That decoupling is the whole point: because
// this controller never goes inactive, it stays subscribed and keeps repopulating
// the grid even while the panel is hidden — so opening the panel shows fresh data
// instantly, with no OnDisable gap. (UIManager hides UITK panels via `display`,
// leaving the document active so this populate path always works.)
//
// Keyboard/gamepad: implements IUINavigable, so while this panel is the focused
// top of the UIManager stack, A/D/arrows/stick move a highlighted selection and
// Submit (Enter / gamepad A) opens the detail for the selected card. The grid
// auto-scrolls (ScrollTo) so the selection is always on screen.
//
// Event-driven, NO direct gameplay references: the registry arrives inside the
// event payload, so this never touches GameManager or CreatureRegistrySO statics.
[DisallowMultipleComponent]
public class CreatureGridUITK : MonoBehaviour, IUINavigable
{
    [Header("UI Toolkit setup")]

    // The UIDocument (on a separate panel object) this grid fills. Assigned in the
    // inspector since this script no longer shares its GameObject.
    [SerializeField] private UIDocument document;

    // Which panel this grid is, so the in-UI close button can request the same
    // toggle that tapping E does, and so it registers as that panel's navigable.
    [SerializeField] private UIPanelType panel = UIPanelType.CreatureGrid;

    // The CreatureCardUITK.uxml template, cloned once per MoriMochi. Direct asset
    // reference, so it does NOT need to live in a Resources folder.
    [SerializeField] private VisualTreeAsset cardTemplate;

    [Header("Layout")]

    // Card footprint in px. Cards keep this size and wrap; the grid scrolls when
    // they overflow. Tune here — this is the "decide the card size" knob.
    [SerializeField] private Vector2 cardSize = new Vector2(120f, 150f);

    [Header("Equipment")]

    // Resolves equipped item IDs (dna.Equipped[slot]) to their EquipmentSO.
    [SerializeField] private EquipmentDatabaseSO equipmentDatabase;

    // Slot/rarity accent colors for the equip-row swatches.
    [SerializeField] private EquipmentPaletteSO equipmentPalette;

    // USS class toggled on the currently selected card.
    private const string SelectedClass = "card--selected";

    // The scroll view (named "grid-container") that holds the cards, resolved lazily.
    private ScrollView scroll;

    // The in-UI close button, wired once the document tree is built.
    private Button closeButton;

    // Cards in display order + the registry behind them, so Submit can open the
    // detail for the selected card. selectedIndex is -1 when the grid is empty.
    private readonly List<VisualElement> cards = new List<VisualElement>();
    private CreatureRegistrySO currentRegistry;
    private int selectedIndex = -1;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void OnEnable()
    {
        GameEvents.OnRegistryChanged  += Rebuild;
        GameEvents.OnRegistryReloaded += Rebuild;
    }

    private void OnDisable()
    {
        GameEvents.OnRegistryChanged  -= Rebuild;
        GameEvents.OnRegistryReloaded -= Rebuild;
    }

    // Document trees are built in the UIDocument's OnEnable (before Start), so the
    // close button exists by now. Register as this panel's navigable here too:
    // UIManager subscribes to the registration events in OnEnable (before any Start).
    private void Start()
    {
        WireCloseButton();
        UIManager.RegisterNavigable(panel, this);
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.clicked -= OnCloseClicked;
        UIManager.UnregisterNavigable(panel);
    }

    // ── IUINavigable (routed only while this panel is the focused top) ──

    // Move the selection through the wrapping grid: left/right step by one, up/down
    // by a full row. The row width is measured from the live layout so it matches
    // however many cards actually fit per row.
    public void OnUINavigate(Vector2 dir)
    {
        if (cards.Count == 0) return;
        int idx  = selectedIndex < 0 ? 0 : selectedIndex;
        int cols = ColumnsPerRow();

        if      (dir.x >  0.5f) idx += 1;          // right
        else if (dir.x < -0.5f) idx -= 1;          // left
        else if (dir.y < -0.5f) idx += cols;       // down (Navigate down is -y)
        else if (dir.y >  0.5f) idx -= cols;       // up

        Select(idx);
    }

    // Enter / gamepad A → open the detail window for the selected MoriMochi.
    public void OnUISubmit()
    {
        if (selectedIndex < 0 || selectedIndex >= cards.Count) return;
        if (cards[selectedIndex].userData is CreatureDNA dna)
            UIManager.SelectCreature(dna, currentRegistry);
    }

    // No internal back state — let the UIManager close the grid on ESC.
    public bool OnUICancel() => false;

    // ── Private Methods ───────────────────────────────────────────

    // Wipes the container and re-spawns one card per creature straight from the
    // payload — no lookups. Newest MoriMonchis first, matching the other grids.
    private void Rebuild(CreatureRegistrySO registry)
    {
        var container = ResolveContainer();
        if (container == null || registry == null || cardTemplate == null) return;

        currentRegistry = registry;
        container.Clear();
        cards.Clear();

        foreach (var dna in registry.GetAll().Values.OrderByDescending(d => d.BirthDate))
        {
            // Instantiate() wraps the card in a TemplateContainer that stretches
            // and would break the wrap, so we pull the card out and add it directly.
            var card = cardTemplate.Instantiate().Q<VisualElement>("card");
            if (card == null) continue;
            BindCard(card, dna);
            card.userData = dna;   // so Submit/click can open this exact creature

            // Mouse click → select (keeps keyboard in sync) and open the detail.
            card.RegisterCallback<ClickEvent>(_ =>
            {
                Select(cards.IndexOf(card));
                UIManager.SelectCreature(dna, registry);
            });

            cards.Add(card);
            container.Add(card);
        }

        // Keep a valid highlighted selection across rebuilds (no scroll while the
        // panel may still be hidden — the layout isn't resolved yet).
        Select(selectedIndex < 0 ? 0 : selectedIndex, scrollIntoView: false);
    }

    // Fills one cloned card by querying its named elements and applies the
    // inspector-driven size.
    private void BindCard(VisualElement card, CreatureDNA dna)
    {
        card.style.width  = cardSize.x;
        card.style.height = cardSize.y;

        var nameLabel = card.Q<Label>("name-label");
        if (nameLabel != null)
            nameLabel.text = string.IsNullOrEmpty(dna.CustomName) ? dna.ToStringID() : dna.CustomName;

        // Placeholder until real sprites exist: tint the icon slot with the
        // creature's BaseColor, same as the uGUI card.
        var icon = card.Q<VisualElement>("icon");
        if (icon != null)
            icon.style.backgroundColor = dna.BaseColor;

        var stateLabel = card.Q<Label>("state-label");
        if (stateLabel != null)
            stateLabel.text = StateOf(dna);

        BindEquipSlot(card, dna, "equip-weapon", EquipmentSlot.Weapon);
        BindEquipSlot(card, dna, "equip-armor",  EquipmentSlot.Armor);
        BindEquipSlot(card, dna, "equip-amulet", EquipmentSlot.Amulet);
    }

    // Fills one equip-row swatch with the equipped item's icon/rarity border, or
    // an empty look with the slot's accent color. Display only — a click here
    // behaves like a click anywhere else on the card and opens the detail panel.
    private void BindEquipSlot(VisualElement card, CreatureDNA dna, string elementName, EquipmentSlot slot)
    {
        var el = card.Q<VisualElement>(elementName);
        if (el == null) return;

        EquipmentSO item = dna.Equipped != null && dna.Equipped.TryGetValue(slot, out var id)
            ? equipmentDatabase?.GetByID(id)
            : null;

        if (item != null)
        {
            el.RemoveFromClassList("card__equip-slot--empty");
            if (item.Icon != null)
                el.style.backgroundImage = new StyleBackground(Background.FromSprite(item.Icon));
            else
                el.style.backgroundColor = item.IconColor;

            el.style.borderTopColor = el.style.borderBottomColor =
                el.style.borderLeftColor = el.style.borderRightColor =
                    equipmentPalette != null ? equipmentPalette.RarityColor(item.Rarity) : BodyPart.RarityColor(item.Rarity);
        }
        else
        {
            el.AddToClassList("card__equip-slot--empty");
            el.style.backgroundImage = StyleKeyword.Null;
            el.style.backgroundColor = StyleKeyword.Null;

            el.style.borderTopColor = el.style.borderBottomColor =
                el.style.borderLeftColor = el.style.borderRightColor =
                    equipmentPalette != null ? equipmentPalette.SlotColor(slot) : new Color(0.35f, 0.35f, 0.43f);
        }
    }

    // Highlights card[idx], clamped, and optionally scrolls it into view.
    private void Select(int idx, bool scrollIntoView = true)
    {
        if (cards.Count == 0) { selectedIndex = -1; return; }

        selectedIndex = Mathf.Clamp(idx, 0, cards.Count - 1);
        for (int i = 0; i < cards.Count; i++)
            cards[i].EnableInClassList(SelectedClass, i == selectedIndex);

        if (scrollIntoView && scroll != null)
            scroll.ScrollTo(cards[selectedIndex]);
    }

    // Cards in the first row share the same resolved top (y); counting them gives
    // the live column count for up/down navigation. Falls back to 1.
    private int ColumnsPerRow()
    {
        if (cards.Count <= 1) return Mathf.Max(1, cards.Count);

        float firstTop = cards[0].layout.y;
        int cols = 0;
        foreach (var c in cards)
        {
            if (Mathf.Abs(c.layout.y - firstTop) < 1f) cols++;
            else break;
        }
        return Mathf.Max(1, cols);
    }

    // Wires the close button to request the same toggle as tapping E. Since the
    // panel is only visible when open, toggling from here closes it.
    private void WireCloseButton()
    {
        var root = document != null ? document.rootVisualElement : null;
        if (root == null) return;

        closeButton = root.Q<Button>("close-button");
        if (closeButton != null) closeButton.clicked += OnCloseClicked;
    }

    private void OnCloseClicked() => UIManager.RequestPanelToggle(panel);

    // Re-resolves if the cached scroll view got detached (panel == null), which
    // happens if the document tree was rebuilt.
    private ScrollView ResolveContainer()
    {
        if (scroll != null && scroll.panel != null) return scroll;
        var root = document != null ? document.rootVisualElement : null;
        if (root == null) return null;
        scroll = root.Q<ScrollView>("grid-container");
        return scroll;
    }

    private static string StateOf(CreatureDNA d) =>
        d.IsSold                                  ? "SOLD"     :
        d.IsDead                                  ? "DEAD"     :
        d.BusyState == BusyReason.Breeding        ? "Breeding" :
        d.BusyState == BusyReason.QueuedForCombat ? "In Queue" :
        "Free";
}
}
