using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
namespace MoriMonchiSimulator
{
[DisallowMultipleComponent]
public class CashRegister : MonoBehaviour
{
    public static CashRegister Instance { get; private set; }

    [Title("Queue Layout")]
    [Required] [SerializeField] private Transform queueRoot;
    [Tooltip("La fila se extiende desde el frente hacia este punto (típicamente la salida/puerta) para formarse hacia afuera, como en la vida real. Vacío → usa el exitPoint del NpcController; si tampoco hay, crece alejándose de la caja.")]
    [SerializeField] private Transform queueTowards;
    [Tooltip("Distancia entre cada cliente de la fila (m).")]
    [MinValue(0.1f)] [SerializeField] private float slotSpacing = 1.5f;
    [Tooltip("Radio de búsqueda sobre el NavMesh al validar cada slot.")]
    [MinValue(0.1f)] [SerializeField] private float sampleRadius = 1.5f;
    [Tooltip("Cuánto puede desviarse un slot de su punto ideal al pegarlo al NavMesh. Si el NavMesh más cercano queda más lejos que esto, el slot se descarta (cae sobre un obstáculo) y se prueba el siguiente: izquierda/derecha.")]
    [MinValue(0.05f)] [SerializeField] private float maxSnap = 0.5f;
    [Tooltip("Separación mínima entre clientes para no solaparse.")]
    [MinValue(0.1f)] [SerializeField] private float minSeparation = 0.6f;
    [Tooltip("Largo máximo de la fila. Si está llena, el cliente se va.")]
    [MinValue(1)]    [SerializeField] private int maxQueueDepth = 16;

    private readonly QueueDirectionHandler    directions   = new QueueDirectionHandler();
    private readonly QueueAvailabilityHandler availability = new QueueAvailabilityHandler();

    private struct Link
    {
        public NpcAgent Agent;
        public Vector3  Pos;
    }

    private readonly List<Link>                          chain   = new List<Link>();
    private readonly List<QueueDirectionHandler.Candidate> candBuf = new List<QueueDirectionHandler.Candidate>();
    private readonly List<Vector3>                       occBuf  = new List<Vector3>();

    public event Action<NpcAgent> OnCurrentCustomerChanged;

    public Vector3  QueueRootPos    => queueRoot != null ? queueRoot.position : transform.position;
    public NpcAgent CurrentCustomer => chain.Count > 0 ? chain[0].Agent : null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Public API ────────────────────────────────────────────────

    public Vector3? TryReserveSlot(NpcAgent agent)
    {
        if (agent == null || queueRoot == null) return null;

        int existing = IndexOf(agent);
        if (existing >= 0) return chain[existing].Pos;

        if (chain.Count >= maxQueueDepth) return null;

        if (!TryComputeLink(chain.Count, agent, out var link)) return null;
        chain.Add(link);
        if (chain.Count == 1) OnCurrentCustomerChanged?.Invoke(agent);
        return link.Pos;
    }

    public void ReleaseSlot(NpcAgent agent)
    {
        int idx = IndexOf(agent);
        if (idx < 0) return;

        bool wasFront = idx == 0;
        chain.RemoveAt(idx);
        Recompute();
        if (wasFront) OnCurrentCustomerChanged?.Invoke(chain.Count > 0 ? chain[0].Agent : null);
    }

    public bool IsFrontSlot(NpcAgent agent) => chain.Count > 0 && chain[0].Agent == agent;

    public Vector3? CurrentSlotOf(NpcAgent agent)
    {
        int idx = IndexOf(agent);
        return idx >= 0 ? chain[idx].Pos : (Vector3?)null;
    }

    // ── Chain placement ───────────────────────────────────────────

    private void Recompute()
    {
        for (int i = 0; i < chain.Count; i++)
            if (TryComputeLink(i, chain[i].Agent, out var link)) chain[i] = link;
    }

    private bool TryComputeLink(int index, NpcAgent agent, out Link link)
    {
        if (index == 0)
        {
            Vector3 pos = NavMesh.SamplePosition(QueueRootPos, out var h, sampleRadius, NavMesh.AllAreas)
                ? h.position : QueueRootPos;
            link = new Link { Agent = agent, Pos = pos };
            return true;
        }

        var prev = chain[index - 1];

        occBuf.Clear();
        for (int i = 0; i < index; i++) occBuf.Add(chain[i].Pos);

        directions.Candidates(prev.Pos, BackDirection(), slotSpacing, candBuf);
        for (int i = 0; i < candBuf.Count; i++)
        {
            if (availability.IsAvailable(prev.Pos, candBuf[i].Pos, NavMesh.AllAreas, sampleRadius, maxSnap, occBuf, minSeparation, out var snapped))
            {
                link = new Link { Agent = agent, Pos = snapped };
                return true;
            }
        }

        link = default;
        return false;
    }

    private int IndexOf(NpcAgent agent)
    {
        for (int i = 0; i < chain.Count; i++)
            if (chain[i].Agent == agent) return i;
        return -1;
    }

    private Vector3 BackDirection() => SnapToOrthogonal(RawBackDirection());

    private Vector3 RawBackDirection()
    {
        var target = QueueTargetPos();
        if (target.HasValue)
        {
            Vector3 toExit = target.Value - QueueRootPos;
            toExit.y = 0f;
            if (toExit.sqrMagnitude > 0.0001f) return toExit.normalized;
        }

        Vector3 away = QueueRootPos - transform.position;
        away.y = 0f;
        if (away.sqrMagnitude > 0.0001f) return away.normalized;

        return transform.forward;
    }

    private Vector3 SnapToOrthogonal(Vector3 dir)
    {
        Vector3 fwd   = transform.forward; fwd.y   = 0f;
        Vector3 right = transform.right;   right.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) { fwd = Vector3.forward; right = Vector3.right; }
        fwd.Normalize(); right.Normalize();

        Vector3 best = fwd;
        float   bestDot = Vector3.Dot(dir, fwd);

        float d = Vector3.Dot(dir, -fwd);   if (d > bestDot) { bestDot = d; best = -fwd; }
        d       = Vector3.Dot(dir,  right); if (d > bestDot) { bestDot = d; best =  right; }
        d       = Vector3.Dot(dir, -right); if (d > bestDot) { bestDot = d; best = -right; }

        return best;
    }

    private Vector3? QueueTargetPos()
    {
        if (queueTowards != null) return queueTowards.position;
        var ctrl = NpcController.Instance;
        if (ctrl != null && ctrl.ExitPoint != null) return ctrl.ExitPoint.position;
        return null;
    }

    private void OnDrawGizmos()
    {
        if (queueRoot != null)
        {
            Vector3 dir   = BackDirection();
            Vector3 start = QueueRootPos;
            Gizmos.color = new Color(0.3f, 0.7f, 1f);

            Gizmos.DrawLine(start, start + dir * (slotSpacing * 4f));

            Vector3 head = start + dir * slotSpacing;
            Vector3 wing = Quaternion.AngleAxis(150f, Vector3.up) * dir;
            Gizmos.DrawLine(head, head + wing * (slotSpacing * 0.25f));
            wing = Quaternion.AngleAxis(-150f, Vector3.up) * dir;
            Gizmos.DrawLine(head, head + wing * (slotSpacing * 0.25f));
        }

        if (chain == null) return;
        for (int i = 0; i < chain.Count; i++)
        {
            Gizmos.color = i == 0 ? new Color(0.3f, 1f, 0.4f) : new Color(0.95f, 0.8f, 0.3f);
            Gizmos.DrawWireSphere(chain[i].Pos, 0.3f);
            if (i > 0) Gizmos.DrawLine(chain[i - 1].Pos, chain[i].Pos);
        }
    }
}
}
