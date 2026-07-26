using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

// Always-on screen overlay: the current date (top-right) and a small input legend
// (top-left). Generic chrome, not tied to any system — it just reads the clock and a
// designer-authored list of control hints.
//
// Standalone like HotbarHUDUITK (NOT a UIManager panel): it's never focused, so it
// stays out of the panel stack and its UIDocument has picking-mode Ignore so it never
// eats gameplay clicks. Lives on an always-active object referencing its UIDocument.
[DisallowMultipleComponent]
public class InfoOverlayUITK : MonoBehaviour
{
    [Serializable]
    public struct InputHint
    {
        public string Key;
        public string ActionKey;
    }

    [SerializeField] private UIDocument document;

    [Tooltip("Control legend shown top-left. Edit to match the current bindings.")]
    [SerializeField] private InputHint[] hints =
    {
        new InputHint { Key = "WASD",  ActionKey = "ui.overlay.hint.move" },
        new InputHint { Key = "E",     ActionKey = "ui.overlay.hint.interact" },
        new InputHint { Key = "Click", ActionKey = "ui.overlay.hint.use" },
        new InputHint { Key = "Q",     ActionKey = "ui.overlay.hint.drop" },
        new InputHint { Key = "Rueda", ActionKey = "ui.overlay.hint.slot" },
        new InputHint { Key = "B",     ActionKey = "ui.overlay.hint.build" },
        new InputHint { Key = "Tab",   ActionKey = "ui.overlay.hint.catalog" },
    };

    // Refresh the date once a second — it never changes faster, and rebuilding each
    // frame is wasted work for a label that turns over at midnight.
    private const float DateRefreshInterval = 1f;

    private const string DateFormatKey = "ui.overlay.date.format";
    private const string DabloonsKey   = "ui.overlay.dabloons";

    private Label dateLabel;
    private Label dabloonsLabel;
    private float refreshTimer;
    private string lastDateText;

    private void OnEnable()
    {
        GameEvents.OnInventoryChanged  += RefreshDabloons;
        GameEvents.OnInventoryReloaded += RefreshDabloons;
        UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnInventoryChanged  -= RefreshDabloons;
        GameEvents.OnInventoryReloaded -= RefreshDabloons;
        UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
    }

    private void Start()
    {
        Loc.ApplySavedLocale();

        var root = document != null ? document.rootVisualElement : null;
        if (root == null) { Debug.LogWarning("[InfoOverlayUITK] No UIDocument / root."); return; }

        dateLabel     = root.Q<Label>("date");
        dabloonsLabel = root.Q<Label>("dabloons");

        BuildHints(root.Q<VisualElement>("hints"));
        RefreshDate(force: true);

        // Initial dabloons read — inventory is already loaded at this point.
        var inv = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
        if (inv != null) RefreshDabloons(inv);
    }

    private void Update()
    {
        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer < DateRefreshInterval) return;
        refreshTimer = 0f;
        RefreshDate(force: false);
    }

    private void RefreshDate(bool force)
    {
        if (dateLabel == null) return;
        var now = DateTime.Now;
        var culture = Loc.Culture;
        string dayName = Capitalize(culture.DateTimeFormat.GetDayName(now.DayOfWeek), culture);
        string monthName = Capitalize(culture.DateTimeFormat.GetMonthName(now.Month), culture);
        string text = Loc.Tr(DateFormatKey, dayName, now.Day, monthName, now.Year);
        if (!force && text == lastDateText) return;
        lastDateText = text;
        dateLabel.text = text;
    }

    private static string Capitalize(string value, CultureInfo culture)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return char.ToUpper(value[0], culture) + value.Substring(1);
    }

    private void RefreshDabloons(PlayerInventorySO inv)
    {
        if (dabloonsLabel == null || inv == null) return;
        dabloonsLabel.text = Loc.Tr(DabloonsKey, inv.Dabloons);
    }

    private void BuildHints(VisualElement container)
    {
        if (container == null || hints == null) return;
        container.Clear();

        foreach (var hint in hints)
        {
            var row = new VisualElement();
            row.AddToClassList("hint-row");

            var key = new Label(hint.Key);
            key.AddToClassList("hint-key");
            row.Add(key);

            var action = new Label(Loc.Tr(hint.ActionKey));
            action.AddToClassList("hint-action");
            row.Add(action);

            container.Add(row);
        }

        var langRow = new VisualElement();
        langRow.AddToClassList("lang-row");
        langRow.Add(MakeLangButton("en", "EN"));
        langRow.Add(MakeLangButton("es", "ES"));
        container.Add(langRow);
    }

    private Button MakeLangButton(string code, string label)
    {
        var btn = new Button(() => Loc.SetLocale(code)) { text = label };
        btn.AddToClassList("lang-btn");
        if (Loc.CurrentCode == code) btn.AddToClassList("lang-btn--active");
        return btn;
    }

    private void HandleLocaleChanged(UnityEngine.Localization.Locale locale)
    {
        var root = document != null ? document.rootVisualElement : null;
        if (root == null) return;
        BuildHints(root.Q<VisualElement>("hints"));
        RefreshDate(force: true);
        var inv = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
        if (inv != null) RefreshDabloons(inv);
    }
}
}
