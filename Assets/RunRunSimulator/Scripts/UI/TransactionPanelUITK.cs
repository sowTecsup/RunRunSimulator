using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{
[DisallowMultipleComponent]
public class TransactionPanelUITK : MonoBehaviour
{
    [SerializeField] private UIDocument document;

    private VisualElement root;
    private Label         customerNameLbl;
    private Label         archetypeLbl;
    private VisualElement mmSwatch;
    private Label         targetNameLbl;
    private Label         targetInfoLbl;
    private Label         offerLbl;
    private Button        acceptBtn;
    private Button        counterBtn;
    private Button        rejectBtn;

    private NpcAgent currentCustomer;
    private bool     bound;
    private bool     isOpen;

    private void OnEnable()
    {
        if (CashRegister.Instance != null)
            CashRegister.Instance.OnCurrentCustomerChanged += HandleCustomerChanged;
    }

    private void OnDisable()
    {
        if (CashRegister.Instance != null)
            CashRegister.Instance.OnCurrentCustomerChanged -= HandleCustomerChanged;
        if (isOpen) OnHidden();
    }

    private void Update()
    {
        bool shown = IsShown();
        if (shown == isOpen) return;
        isOpen = shown;
        if (shown) OnShown();
        else       OnHidden();
    }

    private bool IsShown()
    {
        var r = document != null ? document.rootVisualElement : null;
        return r != null && r.resolvedStyle.display != DisplayStyle.None;
    }

    private void OnShown()
    {
        EnsureBound();
        Refresh();
        if (currentCustomer != null) currentCustomer.EnterNegotiating();
    }

    private void OnHidden()
    {
        if (currentCustomer != null && currentCustomer.State == NpcAgent.NpcState.Negotiating)
            currentCustomer.ExitNegotiating();
    }

    private void EnsureBound()
    {
        if (bound) return;
        if (document == null) { Debug.LogWarning("[TransactionPanelUITK] No UIDocument."); return; }
        root = document.rootVisualElement;
        if (root == null) return;

        customerNameLbl = root.Q<Label>("customer-name");
        archetypeLbl    = root.Q<Label>("archetype");
        mmSwatch        = root.Q<VisualElement>("mm-swatch");
        targetNameLbl   = root.Q<Label>("target-name");
        targetInfoLbl   = root.Q<Label>("target-info");
        offerLbl        = root.Q<Label>("offer");
        acceptBtn       = root.Q<Button>("accept");
        counterBtn      = root.Q<Button>("counter");
        rejectBtn       = root.Q<Button>("reject");
        if (acceptBtn  != null) acceptBtn.clicked  += OnAccept;
        if (counterBtn != null) counterBtn.clicked += OnCounter;
        if (rejectBtn  != null) rejectBtn.clicked  += OnReject;

        var titleLabel = root.Q<Label>(className: "panel__title");
        if (titleLabel != null) titleLabel.text = Loc.Tr("ui.transaction.title");

        var customerCol      = root.Q<VisualElement>(className: "col--customer");
        var customerColLabel = customerCol?.Q<Label>(className: "col__label");
        if (customerColLabel != null) customerColLabel.text = Loc.Tr("ui.transaction.customer_label");

        var mmCol      = root.Q<VisualElement>(className: "col--mm");
        var mmColLabel = mmCol?.Q<Label>(className: "col__label");
        if (mmColLabel != null) mmColLabel.text = Loc.Tr("ui.transaction.mm_label");

        var offerCol      = root.Q<VisualElement>(className: "col--value");
        var offerColLabel = offerCol?.Q<Label>(className: "col__label");
        if (offerColLabel != null) offerColLabel.text = Loc.Tr("ui.transaction.offer_label");

        var unitLabel = root.Q<Label>(className: "offer-unit");
        if (unitLabel != null) unitLabel.text = Loc.Tr("ui.transaction.dabloons_unit");

        if (acceptBtn  != null) acceptBtn.text  = Loc.Tr("ui.transaction.accept");
        if (counterBtn != null) counterBtn.text = Loc.Tr("ui.transaction.counter");
        if (rejectBtn  != null) rejectBtn.text  = Loc.Tr("ui.transaction.reject");

        bound = true;
    }

    private void HandleCustomerChanged(NpcAgent next)
    {
        currentCustomer = next;
        if (isOpen) Refresh();
    }

    private void Refresh()
    {
        currentCustomer = CashRegister.Instance != null ? CashRegister.Instance.CurrentCustomer : null;
        if (root == null) return;

        if (currentCustomer == null || currentCustomer.TargetMM == null)
        {
            if (customerNameLbl != null) customerNameLbl.text = Loc.Tr("ui.transaction.no_customer");
            if (archetypeLbl    != null) archetypeLbl.text    = "";
            if (targetNameLbl   != null) targetNameLbl.text   = "";
            if (targetInfoLbl   != null) targetInfoLbl.text   = "";
            if (offerLbl        != null) offerLbl.text        = "";
            if (mmSwatch        != null) mmSwatch.style.backgroundColor = new StyleColor(new Color(0.47f, 0.47f, 0.47f));
            if (acceptBtn  != null) acceptBtn.SetEnabled(false);
            if (counterBtn != null) counterBtn.SetEnabled(false);
            if (rejectBtn  != null) rejectBtn.SetEnabled(false);
            return;
        }

        var arch = currentCustomer.Archetype;
        var mm   = currentCustomer.TargetMM;
        if (customerNameLbl != null) customerNameLbl.text = Loc.Tr("ui.transaction.customer");
        if (archetypeLbl    != null) archetypeLbl.text    = arch != null ? arch.DisplayName : "";
        if (mmSwatch        != null) MonchiPortraitUI.Apply(mmSwatch, mm);
        if (targetNameLbl   != null) targetNameLbl.text   = string.IsNullOrEmpty(mm.CustomName) ? Loc.Tr("ui.transaction.default_mm_name") : mm.CustomName;
        if (targetInfoLbl   != null) targetInfoLbl.text   = Loc.Tr("ui.transaction.target_info", GenderGlyph(mm.Gender), mm.AgeDays);
        if (offerLbl        != null) offerLbl.text        = Loc.Tr("ui.transaction.offer_amount", currentCustomer.CurrentOffer);
        if (acceptBtn  != null) acceptBtn.SetEnabled(true);
        if (counterBtn != null) counterBtn.SetEnabled(!currentCustomer.HasCounteredOnce);
        if (rejectBtn  != null) rejectBtn.SetEnabled(true);
    }

    private static string GenderGlyph(CreatureGender g) => g switch
    {
        CreatureGender.Male   => "♂",
        CreatureGender.Female => "♀",
        _                     => "?",
    };

    private void OnAccept()
    {
        if (currentCustomer == null) return;
        currentCustomer.AcceptCurrentOffer();
        UIManager.RequestPanelToggle(UIPanelType.Transaction);
    }

    private void OnCounter()
    {
        if (currentCustomer == null) return;
        bool ok = currentCustomer.TryCounterOffer();
        if (!ok) { UIManager.RequestPanelToggle(UIPanelType.Transaction); return; }
        Refresh();
    }

    private void OnReject()
    {
        if (currentCustomer == null) return;
        currentCustomer.RejectByPlayer();
        UIManager.RequestPanelToggle(UIPanelType.Transaction);
    }
}
}
