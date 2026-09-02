using UnityEngine;
using UnityEngine.UIElements;
using MoriMonchiSimulator.DragonRps;
namespace MoriMonchiSimulator
{

[DisallowMultipleComponent]
public class CombatPanelUITK : MonoBehaviour, IUINavigable
{
    [SerializeField] private UIDocument document;
    [SerializeField] private CombatTuningSO tuning;
    [SerializeField] private UIPanelType panel = UIPanelType.Combat;

    private enum View { Pick, Duel, Result }

    private View view;
    private VisualElement[] views;

    private CombatPickPresenter pick;
    private CombatDuelPresenter duel;
    private CombatResultPresenter result;

    private CreatureRegistrySO registry;
    private PlayerInventorySO inventory;
    private CreatureDNA player;
    private CreatureDNA rival;
    private DragonRpsSession session;
    private bool wired;

    private void OnEnable()
    {
        UIManager.OnPanelSetRequested    += OnPanelSet;
        UIManager.OnPanelToggleRequested += OnPanelToggle;
    }

    private void OnDisable()
    {
        UIManager.OnPanelSetRequested    -= OnPanelSet;
        UIManager.OnPanelToggleRequested -= OnPanelToggle;
    }

    private void Start()
    {
        var root = UiPanels.RootOf(document);
        if (root == null) return;

        root.Q<Label>("rps-title").text = Loc.Tr("ui.rps.title");
        views = new[] { root.Q("view-pick"), root.Q("view-duel"), root.Q("view-result") };

        pick   = new CombatPickPresenter(views[0]);
        duel   = new CombatDuelPresenter(views[1]);
        result = new CombatResultPresenter(views[2]);

        pick.FightRequested += StartDuel;
        pick.CloseRequested += Close;
        duel.CardPlayed     += PlayCard;
        result.AgainRequested += ShowPick;
        result.CloseRequested += Close;

        wired = true;
        UIManager.RegisterNavigable(panel, this);
        ShowPick();
    }

    private void OnDestroy()
    {
        if (pick != null)
        {
            pick.FightRequested -= StartDuel;
            pick.CloseRequested -= Close;
        }
        if (duel != null) duel.CardPlayed -= PlayCard;
        if (result != null)
        {
            result.AgainRequested -= ShowPick;
            result.CloseRequested -= Close;
        }
        UIManager.UnregisterNavigable(panel);
    }

    private void OnPanelSet(UIPanelType p, bool show)
    {
        if (p == panel && show && wired) ShowPick();
    }

    private void OnPanelToggle(UIPanelType p)
    {
        if (p != panel || !wired) return;
        var root = UiPanels.RootOf(document);
        root?.schedule.Execute(() => { if (root.resolvedStyle.display != DisplayStyle.None) ShowPick(); });
    }

    private void ShowPick()
    {
        session = null;
        player = null;
        rival = null;

        var gm = GameManager.Instance;
        registry  = gm != null ? gm.Registry : null;
        inventory = gm != null ? gm.Inventory : null;

        if (registry == null || tuning == null)
        {
            Debug.LogWarning("[CombatPanelUITK] Missing registry or tuning.");
            return;
        }

        pick.Rebuild(registry, tuning, GameManager.Now);
        SetView(View.Pick);
    }

    private void StartDuel(CreatureDNA chosen)
    {
        var now = GameManager.Now;
        int seed = DragonRpsService.Seed(chosen, now);
        var foe = DragonRpsRival.Generate(registry, chosen, tuning, new System.Random(seed));
        if (foe == null)
        {
            Debug.LogWarning("[CombatPanelUITK] No rival available.");
            return;
        }

        player = chosen;
        rival = foe;
        session = DragonRpsService.Start(player, rival, seed);
        duel.Begin(session, player, rival);
        SetView(View.Duel);
    }

    private void PlayCard(int handIndex)
    {
        if (session == null || session.Finished) return;
        session.Play(handIndex);
        duel.Rebuild(session, duel.Describe(session.LastRound));
        if (session.Finished) FinishDuel();
    }

    private void FinishDuel()
    {
        var outcome = DragonRpsService.Resolve(session, player, registry, inventory, tuning, GameManager.Now);
        result.Show(outcome, player, rival);
        SetView(View.Result);
    }

    private void SetView(View v)
    {
        view = v;
        for (int i = 0; i < views.Length; i++)
            views[i].EnableInClassList("rps-view--active", i == (int)v);
    }

    private void Close() => UIManager.RequestPanelSet(panel, false);

    public void OnUINavigate(Vector2 dir)
    {
        if (!wired) return;
        switch (view)
        {
            case View.Pick:   pick.Move(dir.x);   break;
            case View.Duel:   duel.Move(dir.x);   break;
            case View.Result: result.Move(dir.x); break;
        }
    }

    public void OnUISubmit()
    {
        if (!wired) return;
        switch (view)
        {
            case View.Pick:   pick.Submit();   break;
            case View.Duel:   duel.Submit();   break;
            case View.Result: result.Submit(); break;
        }
    }

    public bool OnUICancel()
    {
        if (!wired) return false;
        if (view == View.Duel)
        {
            ShowPick();
            return true;
        }
        return false;
    }
}
}
