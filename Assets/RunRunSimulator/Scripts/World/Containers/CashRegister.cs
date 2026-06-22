using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{
[DisallowMultipleComponent]
public class CashRegister : MonoBehaviour
{
    public static CashRegister Instance { get; private set; }

    [Title("Queue Layout")]
    [Required] [SerializeField] private Transform queueRoot;
    [MinValue(0.1f)] [SerializeField] private float slotSpacing = 1.5f;
    [MinValue(1)]    [SerializeField] private int maxQueueDepth = 16;

    private class QueueSlotNode
    {
        public Vector3      LocalPos;
        public NpcAgent     Occupant;
        public QueueSlotNode Back;
        public QueueSlotNode Left;
        public QueueSlotNode Right;
        public int          Depth;
    }

    private QueueSlotNode root;
    private Dictionary<NpcAgent, QueueSlotNode> assignment = new();

    public event Action<NpcAgent> OnCurrentCustomerChanged;

    public Vector3  QueueRootPos    => queueRoot != null ? queueRoot.position : transform.position;
    public NpcAgent CurrentCustomer => root != null ? root.Occupant : null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        root = new QueueSlotNode { LocalPos = Vector3.zero, Depth = 0 };
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public Vector3? TryReserveSlot(NpcAgent agent)
    {
        if (agent == null || queueRoot == null) return null;
        if (assignment.ContainsKey(agent)) return WorldOf(assignment[agent]);
        var node = FindFreeSlotBfs();
        if (node == null) return null;
        node.Occupant = agent;
        assignment[agent] = node;
        if (node == root) OnCurrentCustomerChanged?.Invoke(agent);
        return WorldOf(node);
    }

    public void ReleaseSlot(NpcAgent agent)
    {
        if (agent == null || !assignment.TryGetValue(agent, out var node)) return;
        bool wasFront = node == root;
        node.Occupant = null;
        assignment.Remove(agent);
        if (wasFront) AdvanceQueue();
    }

    public bool IsFrontSlot(NpcAgent agent)
    {
        return agent != null && root != null && root.Occupant == agent;
    }

    public Vector3? CurrentSlotOf(NpcAgent agent)
    {
        if (agent == null) return null;
        if (assignment.TryGetValue(agent, out var node)) return WorldOf(node);
        return null;
    }

    private void AdvanceQueue()
    {
        var waiting = new List<NpcAgent>();
        BfsCollectOccupants(root, waiting);
        ClearTree(root);
        foreach (var a in waiting)
        {
            var node = FindFreeSlotBfs();
            if (node == null) break;
            node.Occupant = a;
            assignment[a] = node;
        }
        OnCurrentCustomerChanged?.Invoke(root.Occupant);
    }

    private void BfsCollectOccupants(QueueSlotNode start, List<NpcAgent> out_)
    {
        if (start == null) return;
        var q = new Queue<QueueSlotNode>();
        q.Enqueue(start);
        while (q.Count > 0)
        {
            var n = q.Dequeue();
            if (n.Occupant != null) out_.Add(n.Occupant);
            if (n.Back  != null) q.Enqueue(n.Back);
            if (n.Left  != null) q.Enqueue(n.Left);
            if (n.Right != null) q.Enqueue(n.Right);
        }
    }

    private void ClearTree(QueueSlotNode start)
    {
        if (start == null) return;
        start.Occupant = null;
        start.Back = null; start.Left = null; start.Right = null;
        assignment.Clear();
    }

    private QueueSlotNode FindFreeSlotBfs()
    {
        var q = new Queue<QueueSlotNode>();
        q.Enqueue(root);
        while (q.Count > 0)
        {
            var n = q.Dequeue();
            if (n.Occupant == null) return n;
            if (n.Depth >= maxQueueDepth) continue;
            if (n.Back  == null) n.Back  = MakeChild(n, ChildKind.Back);
            if (n.Left  == null) n.Left  = MakeChild(n, ChildKind.Left);
            if (n.Right == null) n.Right = MakeChild(n, ChildKind.Right);
            q.Enqueue(n.Back); q.Enqueue(n.Left); q.Enqueue(n.Right);
        }
        return null;
    }

    private enum ChildKind { Back, Left, Right }

    private QueueSlotNode MakeChild(QueueSlotNode parent, ChildKind kind)
    {
        Vector3 local = parent.LocalPos;
        switch (kind)
        {
            case ChildKind.Back:  local += Vector3.forward * slotSpacing; break;
            case ChildKind.Left:  local += Vector3.forward * slotSpacing * 0.5f + Vector3.left  * slotSpacing * 0.5f; break;
            case ChildKind.Right: local += Vector3.forward * slotSpacing * 0.5f + Vector3.right * slotSpacing * 0.5f; break;
        }
        return new QueueSlotNode { LocalPos = local, Depth = parent.Depth + 1 };
    }

    private Vector3 WorldOf(QueueSlotNode node)
    {
        if (queueRoot == null || node == null) return transform.position;
        return queueRoot.TransformPoint(node.LocalPos);
    }
}
}
