using UnityEngine;
using UnityEngine.Events;

namespace MoriMonchiSimulator
{

[RequireComponent(typeof(Perceivable))]
public class ExitZone : MonoBehaviour
{
    [SerializeField, Min(0.5f)] private float radius = 2.5f;
    [SerializeField] private UnityEvent onDeposit;

    private Perceivable perceivable;

    public float Radius => radius;
    public ExpeditionTeam Team => perceivable.Team;
    public int Secured { get; private set; }

    private void Awake()
    {
        perceivable = GetComponent<Perceivable>();
    }

    public void SetTeam(ExpeditionTeam team)
    {
        perceivable.SetTeam(team);
    }

    public bool Contains(Vector3 worldPosition)
    {
        Vector3 delta = worldPosition - transform.position;
        delta.y = 0f;
        return delta.sqrMagnitude <= radius * radius;
    }

    public void Deposit(int units)
    {
        if (units <= 0) return;
        Secured += units;
        onDeposit?.Invoke();
    }
}
}
