using System;
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

    private const float ClockRefreshInterval = 1f;

    private const string ClockKey      = "ui.overlay.clock";
    private const string DabloonsKey   = "ui.overlay.dabloons";
    private const string MaterialKey   = "ui.overlay.material";
    private const string ExpeditionReturnKey = "ui.overlay.expedition.return";
    private const string ExpeditionLostKey = "ui.overlay.expedition.lost";
    private const string CreatureAdoptedKey = "ui.overlay.creature.adopted";
    private const string CreatureLostKey = "ui.overlay.creature.lost";

    [SerializeField, Min(0f)] private float toastSeconds = 6f;

    private Label dateLabel;
    private Label dabloonsLabel;
    private Label materialLabel;
    private Label expeditionToastLabel;
    private float refreshTimer;
    private float toastTimer;
    private string lastClockText;
    private ExpeditionReturn? pendingToast;

    private void OnEnable()
    {
        GameEvents.OnInventoryChanged  += RefreshDabloons;
        GameEvents.OnInventoryReloaded += RefreshDabloons;
        GameEvents.OnExpeditionReturned += HandleExpeditionReturned;
        GameEvents.OnCreatureDeparted += HandleCreatureDeparted;
        GameEvents.OnDayBlockChanged += HandleDayBlockChanged;
        GameEvents.OnDayStarted += HandleDayStarted;
        UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnInventoryChanged  -= RefreshDabloons;
        GameEvents.OnInventoryReloaded -= RefreshDabloons;
        GameEvents.OnExpeditionReturned -= HandleExpeditionReturned;
        GameEvents.OnCreatureDeparted -= HandleCreatureDeparted;
        GameEvents.OnDayBlockChanged -= HandleDayBlockChanged;
        GameEvents.OnDayStarted -= HandleDayStarted;
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
        RefreshClock(force: true);

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
        if (refreshTimer >= ClockRefreshInterval)
        {
            refreshTimer = 0f;
            RefreshClock(force: false);
        }

        if (toastTimer > 0f)
        {
            toastTimer -= Time.unscaledDeltaTime;
            if (toastTimer <= 0f && expeditionToastLabel != null)
                expeditionToastLabel.style.display = DisplayStyle.None;
        }
    }

    private void RefreshClock(bool force)
    {
        if (dateLabel == null) return;

        var clock = GameClock.Instance;
        if (clock == null)
        {
            dateLabel.style.display = DisplayStyle.None;
            lastClockText = null;
            return;
        }

        dateLabel.style.display = DisplayStyle.Flex;
        var block = clock.Block;
        int hour = Mathf.Clamp((int)(clock.MinuteOfDay / 60f), 0, 23);
        int minute = Mathf.Clamp((int)(clock.MinuteOfDay % 60f), 0, 59);
        string blockName = block != null ? Loc.Tr(block.NameKey) : string.Empty;
        string text = Loc.Tr(ClockKey, clock.Day, hour, minute, blockName);
        if (!force && text == lastClockText) return;
        lastClockText = text;
        dateLabel.text = text;
    }

    private void HandleDayBlockChanged(DayBlockDef block) => RefreshClock(force: true);

    private void HandleDayStarted(int day) => RefreshClock(force: true);

    private void RefreshDabloons(PlayerInventorySO inv)
    {
        if (dabloonsLabel == null || inv == null) return;
        dabloonsLabel.text = Loc.Tr(DabloonsKey, inv.Balance(Currency.Dabloons));
        if (materialLabel != null) materialLabel.text = Loc.Tr(MaterialKey, inv.Balance(Currency.Minerita));
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
        expeditionToastLabel.RemoveFromClassList("toast--win");
        expeditionToastLabel.RemoveFromClassList("toast--lose");
        expeditionToastLabel.RemoveFromClassList("toast--draw");

        if (r.Lost)
        {
            expeditionToastLabel.text = Loc.Tr(ExpeditionLostKey, r.Floors, r.Fallen);
            expeditionToastLabel.AddToClassList("toast--lose");
        }
        else
        {
            expeditionToastLabel.text = Loc.Tr(ExpeditionReturnKey, r.Floors, r.MineritaGained, r.Fallen);
            string resultClass = r.Winner == ExpeditionTeam.Player ? "toast--win"
                : r.Winner == ExpeditionTeam.Rival ? "toast--lose"
                : "toast--draw";
            expeditionToastLabel.AddToClassList(resultClass);
        }

        expeditionToastLabel.style.display = DisplayStyle.Flex;
        toastTimer = toastSeconds;
    }

    private void HandleCreatureDeparted(CreatureDNA dna)
    {
        if (dna == null || expeditionToastLabel == null) return;
        string name = !string.IsNullOrEmpty(dna.CustomName) ? dna.CustomName : dna.ToStringID();

        expeditionToastLabel.RemoveFromClassList("toast--win");
        expeditionToastLabel.RemoveFromClassList("toast--lose");
        expeditionToastLabel.RemoveFromClassList("toast--draw");

        expeditionToastLabel.text = dna.IsSold ? Loc.Tr(CreatureAdoptedKey, name) : Loc.Tr(CreatureLostKey, name);
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
        RefreshClock(force: true);
        var inv = GameManager.CurrentInventory;
        if (inv != null) RefreshDabloons(inv);
    }
}
}
