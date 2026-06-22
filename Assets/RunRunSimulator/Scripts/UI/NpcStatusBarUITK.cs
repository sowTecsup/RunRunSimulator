using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{
[DisallowMultipleComponent]
public class NpcStatusBarUITK : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    private VisualElement container;
    private float refreshTimer;
    private const float RefreshInterval = 0.25f;

    private void OnEnable()
    {
        GameEvents.OnCustomerSpawned               += HandleSpawned;
        GameEvents.OnCustomerLeft                  += HandleLeft;
        GameEvents.OnCustomerArrivedAtRegister     += HandleArrived;
    }

    private void OnDisable()
    {
        GameEvents.OnCustomerSpawned               -= HandleSpawned;
        GameEvents.OnCustomerLeft                  -= HandleLeft;
        GameEvents.OnCustomerArrivedAtRegister     -= HandleArrived;
    }

    private void Start()
    {
        if (document == null) { Debug.LogWarning("[NpcStatusBarUITK] No UIDocument."); return; }
        container = document.rootVisualElement?.Q<VisualElement>("npc-list");
        Rebuild();
    }

    private void Update()
    {
        refreshTimer += Time.unscaledDeltaTime;
        if (refreshTimer < RefreshInterval) return;
        refreshTimer = 0f;
        Rebuild();
    }

    private void HandleSpawned(NpcAgent _)           => Rebuild();
    private void HandleLeft(NpcAgent _, bool __)     => Rebuild();
    private void HandleArrived(NpcAgent _)           => Rebuild();

    private void Rebuild()
    {
        if (container == null) return;
        container.Clear();
        var ctrl = NpcController.Instance;
        if (ctrl == null) return;
        foreach (var npc in ctrl.Active)
        {
            if (npc == null) continue;
            var row = new VisualElement();
            row.AddToClassList("npc-row");
            var name = new Label(npc.Archetype != null ? npc.Archetype.DisplayName : "Cliente");
            name.AddToClassList("npc-name");
            var status = new Label(StatusLabel(npc.State));
            status.AddToClassList("npc-status");
            row.Add(name);
            row.Add(status);
            container.Add(row);
        }
    }

    private static string StatusLabel(NpcAgent.NpcState s) => s switch
    {
        NpcAgent.NpcState.Wandering           => "Mirando…",
        NpcAgent.NpcState.InspectingDisplay   => "Pensando…",
        NpcAgent.NpcState.ApproachingRegister => "Yendo a la caja",
        NpcAgent.NpcState.Queueing            => "Haciendo fila",
        NpcAgent.NpcState.WaitingAtRegister   => "Esperando atención",
        NpcAgent.NpcState.Negotiating         => "Negociando",
        NpcAgent.NpcState.Leaving             => "Saliendo",
        _                                     => "…",
    };
}
}
