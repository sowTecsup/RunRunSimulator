using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

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

    private const float DateRefreshInterval = 1f;

    private const string DateFormatKey = "ui.overlay.date.format";
    private const string DabloonsKey   = "ui.overlay.dabloons";
    private const string MaterialKey   = "ui.overlay.material";
    private const string ExpeditionReturnKey = "ui.overlay.expedition.return";

    [SerializeField, Min(0f)] private float toastSeconds = 6f;

    private Label dateLabel;
    private Label dabloonsLabel;
    private Label materialLabel;
    private Label expeditionToastLabel;
    private float refreshTimer;
    private float toastTimer;
    private string lastDateText;
    private ExpeditionReturn? pendingToast;

    private void OnEnable()
    {
        GameEvents.OnInventoryChanged  += RefreshDabloons;
        GameEvents.OnInventoryReloaded += RefreshDabloons;
        GameEvents.OnExpeditionReturned += HandleExpeditionReturned;
        UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnInventoryChanged  -= RefreshDabloons;
        GameEvents.OnInventoryReloaded -= RefreshDabloons;
        GameEvents.OnExpeditionReturned -= HandleExpeditionReturned;
        UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
    }

    private void Start()
    {
        Loc.ApplySavedLocale();

        var root = UiPanels.RootOf(document);
        if (root == null) { Debug.LogWarning("[InfoOverlayUITK] No UIDocument / root."); return; }

        dateLabel     = root.Q<Label>("date");
        dabloonsLabel = root.Q<Label>("dabloons");
        materialLabel = root.Q<Label>("material");
        expeditionToastLabel = root.Q<Label>("expedition-toast");

        BuildHints(root.Q<VisualElement>("hints"));
        RefreshDate(force: true);

        var inv = GameManager.CurrentInventory;
        if (inv != null) RefreshDabloons(inv);

        if (expeditionToastLabel != null)
        {
            expeditionToastLabel.style.display = DisplayStyle.None;
            if (pendingToast.HasValue)
            {
                ShowExpeditionToast(pendingToast.Value);
                pendingToast = null;
            }
        }
    }

    private void Update()
    {
        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer >= DateRefreshInterval)
        {
            refreshTimer = 0f;
            RefreshDate(force: false);
        }

        if (toastTimer > 0f)
        {
            toastTimer -= Time.unscaledDeltaTime;
            if (toastTimer <= 0f && expeditionToastLabel != null)
                expeditionToastLabel.style.display = DisplayStyle.None;
        }
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
        if (materialLabel != null) materialLabel.text = Loc.Tr(MaterialKey, inv.AdventureMaterial);
    }

    private void HandleExpeditionReturned(ExpeditionReturn r)
    {
        if (expeditionToastLabel == null)
        {
            pendingToast = r;
            return;
        }
        ShowExpeditionToast(r);
    }

    private void ShowExpeditionToast(ExpeditionReturn r)
    {
        expeditionToastLabel.text = Loc.Tr(ExpeditionReturnKey, r.Seed, r.PlayerSecured, r.RivalSecured, r.MaterialGained, r.EnergySpent);
        expeditionToastLabel.RemoveFromClassList("toast--win");
        expeditionToastLabel.RemoveFromClassList("toast--lose");
        expeditionToastLabel.RemoveFromClassList("toast--draw");
        string resultClass = r.Winner == ExpeditionTeam.Player ? "toast--win"
            : r.Winner == ExpeditionTeam.Rival ? "toast--lose"
            : "toast--draw";
        expeditionToastLabel.AddToClassList(resultClass);
        expeditionToastLabel.style.display = DisplayStyle.Flex;
        toastTimer = toastSeconds;
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
        var root = UiPanels.RootOf(document);
        if (root == null) return;
        BuildHints(root.Q<VisualElement>("hints"));
        RefreshDate(force: true);
        var inv = GameManager.CurrentInventory;
        if (inv != null) RefreshDabloons(inv);
    }
}
}
