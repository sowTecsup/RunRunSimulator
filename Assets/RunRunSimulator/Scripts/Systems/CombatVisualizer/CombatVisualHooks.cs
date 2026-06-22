using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
namespace MoriMonchiSimulator
{

public class CombatVisualHooks : MonoBehaviour
{
    public enum HookKind { Global, SideA, SideB }

    [Title("Kind")]
    [SerializeField] private HookKind kind = HookKind.Global;

    [Title("Global (Kind=Global)"), ShowIf("@kind == HookKind.Global")]
    public UnityEvent              OnCombatStart;
    [ShowIf("@kind == HookKind.Global")] public UnityEvent OnTurnStart;
    [ShowIf("@kind == HookKind.Global")] public UnityEvent OnTurnEnd;
    [ShowIf("@kind == HookKind.Global")] public UnityEvent OnCombatEnd;
    [ShowIf("@kind == HookKind.Global")] public UnityEvent<string> OnLogLine;

    [Title("Per Side (Kind=SideA/SideB)"), HideIf("@kind == HookKind.Global")]
    public UnityEvent              OnAttack;
    [HideIf("@kind == HookKind.Global")] public UnityEvent OnHitTaken;
    [HideIf("@kind == HookKind.Global")] public UnityEvent OnCritTaken;
    [HideIf("@kind == HookKind.Global")] public UnityEvent OnHitDealt;
    [HideIf("@kind == HookKind.Global")] public UnityEvent OnCritDealt;
    [HideIf("@kind == HookKind.Global")] public UnityEvent OnDead;
    [HideIf("@kind == HookKind.Global")] public UnityEvent<float, float> OnHpChanged;

    private CombatVisualSide Side => kind == HookKind.SideA ? CombatVisualSide.A : CombatVisualSide.B;

    private void OnEnable()
    {
        CombatVisualEvents.OnVisualCombatStart += HandleStart;
        CombatVisualEvents.OnVisualCombatEnd   += HandleEnd;
        CombatVisualEvents.OnTurnStart         += HandleTurnStart;
        CombatVisualEvents.OnTurnEnd           += HandleTurnEnd;
        CombatVisualEvents.OnAttack            += HandleAttack;
        CombatVisualEvents.OnHit               += HandleHit;
        CombatVisualEvents.OnCrit              += HandleCrit;
        CombatVisualEvents.OnDead              += HandleDead;
        CombatVisualEvents.OnHpChanged         += HandleHp;
        CombatVisualEvents.OnLog               += HandleLog;
    }

    private void OnDisable()
    {
        CombatVisualEvents.OnVisualCombatStart -= HandleStart;
        CombatVisualEvents.OnVisualCombatEnd   -= HandleEnd;
        CombatVisualEvents.OnTurnStart         -= HandleTurnStart;
        CombatVisualEvents.OnTurnEnd           -= HandleTurnEnd;
        CombatVisualEvents.OnAttack            -= HandleAttack;
        CombatVisualEvents.OnHit               -= HandleHit;
        CombatVisualEvents.OnCrit              -= HandleCrit;
        CombatVisualEvents.OnDead              -= HandleDead;
        CombatVisualEvents.OnHpChanged         -= HandleHp;
        CombatVisualEvents.OnLog               -= HandleLog;
    }

    private void HandleStart(CombatVisualContext _)
    {
        if (kind == HookKind.Global) OnCombatStart?.Invoke();
    }

    private void HandleEnd(CombatVisualSide _, bool __)
    {
        if (kind == HookKind.Global) OnCombatEnd?.Invoke();
    }

    private void HandleTurnStart(CombatTurn _)
    {
        if (kind == HookKind.Global) OnTurnStart?.Invoke();
    }

    private void HandleTurnEnd(CombatTurn _)
    {
        if (kind == HookKind.Global) OnTurnEnd?.Invoke();
    }

    private void HandleAttack(CombatVisualSide side)
    {
        if (kind == HookKind.Global) return;
        if (side == Side) OnAttack?.Invoke();
    }

    private void HandleHit(CombatVisualHit hit)
    {
        if (kind == HookKind.Global) return;
        if (hit.Defender == Side) OnHitTaken?.Invoke();
        if (hit.Attacker == Side) OnHitDealt?.Invoke();
    }

    private void HandleCrit(CombatVisualHit hit)
    {
        if (kind == HookKind.Global) return;
        if (hit.Defender == Side) OnCritTaken?.Invoke();
        if (hit.Attacker == Side) OnCritDealt?.Invoke();
    }

    private void HandleDead(CombatVisualSide side)
    {
        if (kind == HookKind.Global) return;
        if (side == Side) OnDead?.Invoke();
    }

    private void HandleHp(CombatVisualSide side, float current, float max)
    {
        if (kind == HookKind.Global) return;
        if (side == Side) OnHpChanged?.Invoke(current, max);
    }

    private void HandleLog(string line)
    {
        if (kind == HookKind.Global) OnLogLine?.Invoke(line);
    }
}
}
