using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
namespace MoriMonchiSimulator
{

public class MoriMonchiCombatVisualizer : MoriMonchiVisualizer
{
    [Serializable] public class HpChangedEvent : UnityEvent<float, float> { }

    private const string Group = "Combat Feedbacks";

    [TabGroup(Group, "Ataque")]  public UnityEvent     OnAttack;
    [TabGroup(Group, "Ataque")]  public UnityEvent     OnHitDealt;
    [TabGroup(Group, "Ataque")]  public UnityEvent     OnCritDealt;

    [TabGroup(Group, "Recibe")]  public UnityEvent     OnHitTaken;
    [TabGroup(Group, "Recibe")]  public UnityEvent     OnCritTaken;
    [TabGroup(Group, "Recibe")]  public HpChangedEvent OnHpChanged;

    [TabGroup(Group, "Estado")]  public UnityEvent     OnCombatStart;
    [TabGroup(Group, "Estado")]  public UnityEvent     OnDead;
    [TabGroup(Group, "Estado")]  public UnityEvent     OnVictory;

    public void PlayCombatStart()                  => OnCombatStart?.Invoke();
    public void PlayAttack()                       => OnAttack?.Invoke();
    public void PlayHitDealt()                     => OnHitDealt?.Invoke();
    public void PlayCritDealt()                    => OnCritDealt?.Invoke();
    public void PlayHitTaken()                     => OnHitTaken?.Invoke();
    public void PlayCritTaken()                    => OnCritTaken?.Invoke();
    public void PlayHpChanged(float current, float max) => OnHpChanged?.Invoke(current, max);
    public void PlayDead()                         => OnDead?.Invoke();
    public void PlayVictory()                      => OnVictory?.Invoke();
}
}
