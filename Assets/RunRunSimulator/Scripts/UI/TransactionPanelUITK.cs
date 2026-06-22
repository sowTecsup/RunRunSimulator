using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{
[DisallowMultipleComponent]
public class TransactionPanelUITK : MonoBehaviour
{
    [SerializeField] private UIDocument document;

    private VisualElement root;
    private Label  customerNameLbl;
    private Label  archetypeLbl;
    private Label  targetNameLbl;
    private Label  offerLbl;
    private Button acceptBtn;
    private Button counterBtn;
    private Button rejectBtn;

    private NpcAgent currentCustomer;

    private void OnEnable()
    {
        if (CashRegister.Instance != null)
            CashRegister.Instance.OnCurrentCustomerChanged += HandleCustomerChanged;
        BindRoot();
        Refresh();
        if (currentCustomer != null) currentCustomer.EnterNegotiating();
    }

    private void OnDisable()
    {
        if (CashRegister.Instance != null)
            CashRegister.Instance.OnCurrentCustomerChanged -= HandleCustomerChanged;
        if (currentCustomer != null && currentCustomer.State == NpcAgent.NpcState.Negotiating)
            currentCustomer.ExitNegotiating();
        currentCustomer = null;
    }

    private void BindRoot()
    {
        if (document == null) { Debug.LogWarning("[TransactionPanelUITK] No UIDocument."); return; }
        root = document.rootVisualElement;
        if (root == null) return;
        customerNameLbl = root.Q<Label>("customer-name");
        archetypeLbl    = root.Q<Label>("archetype");
        targetNameLbl   = root.Q<Label>("target-name");
        offerLbl        = root.Q<Label>("offer");
        acceptBtn       = root.Q<Button>("accept");
        counterBtn      = root.Q<Button>("counter");
        rejectBtn       = root.Q<Button>("reject");
        if (acceptBtn  != null) acceptBtn.clicked  += OnAccept;
        if (counterBtn != null) counterBtn.clicked += OnCounter;
        if (rejectBtn  != null) rejectBtn.clicked  += OnReject;
    }

    private void HandleCustomerChanged(NpcAgent next)
    {
        currentCustomer = next;
        Refresh();
    }

    private void Refresh()
    {
        currentCustomer = CashRegister.Instance != null ? CashRegister.Instance.CurrentCustomer : null;
        if (root == null) return;

        if (currentCustomer == null || currentCustomer.TargetMM == null)
        {
            if (customerNameLbl != null) customerNameLbl.text = "Sin clientes";
            if (archetypeLbl    != null) archetypeLbl.text    = "";
            if (targetNameLbl   != null) targetNameLbl.text   = "";
            if (offerLbl        != null) offerLbl.text        = "";
            if (acceptBtn  != null) acceptBtn.SetEnabled(false);
            if (counterBtn != null) counterBtn.SetEnabled(false);
            if (rejectBtn  != null) rejectBtn.SetEnabled(false);
            return;
        }

        var arch = currentCustomer.Archetype;
        if (customerNameLbl != null) customerNameLbl.text = "Cliente";
        if (archetypeLbl    != null) archetypeLbl.text    = arch != null ? arch.DisplayName : "";
        if (targetNameLbl   != null) targetNameLbl.text   = string.IsNullOrEmpty(currentCustomer.TargetMM.CustomName) ? "MoriMochi" : currentCustomer.TargetMM.CustomName;
        if (offerLbl        != null) offerLbl.text        = $"{currentCustomer.CurrentOffer} Dabloons";
        if (acceptBtn  != null) acceptBtn.SetEnabled(true);
        if (counterBtn != null) counterBtn.SetEnabled(!currentCustomer.HasCounteredOnce);
        if (rejectBtn  != null) rejectBtn.SetEnabled(true);
    }

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
