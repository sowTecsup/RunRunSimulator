using System;
using UnityEngine;
using UnityEngine.UIElements;

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
        public string Key;      // "E", "Rueda", "Tab"…
        public string Action;   // "Interactuar", "Cambiar slot"…
    }

    [SerializeField] private UIDocument document;

    [Tooltip("Control legend shown top-left. Edit to match the current bindings.")]
    [SerializeField] private InputHint[] hints =
    {
        new InputHint { Key = "WASD",  Action = "Mover" },
        new InputHint { Key = "E",     Action = "Interactuar / Agarrar" },
        new InputHint { Key = "Click", Action = "Usar" },
        new InputHint { Key = "Q",     Action = "Soltar" },
        new InputHint { Key = "Rueda", Action = "Cambiar slot" },
        new InputHint { Key = "B",     Action = "Construir" },
        new InputHint { Key = "Tab",   Action = "Catálogo" },
    };

    // Refresh the date once a second — it never changes faster, and rebuilding each
    // frame is wasted work for a label that turns over at midnight.
    private const float DateRefreshInterval = 1f;

    private static readonly string[] DayNames =
        { "Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado" };
    private static readonly string[] MonthNames =
        { "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
          "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };

    private Label dateLabel;
    private Label dabloonsLabel;
    private float refreshTimer;
    private string lastDateText;

    private void OnEnable()
    {
        GameEvents.OnInventoryChanged  += RefreshDabloons;
        GameEvents.OnInventoryReloaded += RefreshDabloons;
    }

    private void OnDisable()
    {
        GameEvents.OnInventoryChanged  -= RefreshDabloons;
        GameEvents.OnInventoryReloaded -= RefreshDabloons;
    }

    private void Start()
    {
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
        string text = $"{DayNames[(int)now.DayOfWeek]} {now.Day} de {MonthNames[now.Month - 1]}, {now.Year}";
        if (!force && text == lastDateText) return;
        lastDateText = text;
        dateLabel.text = text;
    }

    private void RefreshDabloons(PlayerInventorySO inv)
    {
        if (dabloonsLabel == null || inv == null) return;
        dabloonsLabel.text = $"Dabloons: {inv.Dabloons:N0}";
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

            var action = new Label(hint.Action);
            action.AddToClassList("hint-action");
            row.Add(action);

            container.Add(row);
        }
    }
}
