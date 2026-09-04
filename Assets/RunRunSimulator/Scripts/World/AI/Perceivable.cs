using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

public struct Percept
{
    public Perceivable Source;
    public PerceivableKind Kind;
    public float SqrDistance;
    public float Affinity;
    public ExpeditionTeam Team;
}

public class Perceivable : MonoBehaviour
{
    [SerializeField] private PerceivableKind kind;
    [SerializeField] private List<string> tags = new List<string>();
    [SerializeField] private ExpeditionTeam team = ExpeditionTeam.None;

    public PerceivableKind Kind => kind;
    public IReadOnlyList<string> Tags => tags;
    public ExpeditionTeam Team => team;

    public void SetTeam(ExpeditionTeam value) => team = value;

    public Vector3 Position => transform.position;

    public MoriMochiAgent Monchi { get; private set; }

    private void Awake()
    {
        Monchi = GetComponent<MoriMochiAgent>();
        if (Monchi == null) Monchi = GetComponentInParent<MoriMochiAgent>();
    }

    private void OnEnable()  => PerceivableRegistry.Register(this);
    private void OnDisable() => PerceivableRegistry.Unregister(this);
}
}
