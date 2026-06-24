using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace MoriMonchiSimulator
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class NpcAgent : MonoBehaviour
    {
        public enum NpcState
        {
            Spawned,
            Wandering,
            InspectingDisplay,
            ApproachingRegister,
            Queueing,
            WaitingAtRegister,
            Negotiating,
            Leaving
        }

        public enum LeaveReason { None, Purchased, Outbid, QueueFull }

        public CustomerArchetypeSO Archetype { get; private set; }
        public string DisplayName { get; private set; }
        public float ReactionDelay { get; private set; }
        public LeaveReason Reason { get; private set; }
        public CreatureDNA TargetMM { get; private set; }
        public int InitialOffer { get; private set; }
        public int CurrentOffer { get; private set; }
        public bool HasCounteredOnce { get; private set; }
        public NpcState State { get; private set; } = NpcState.Spawned;
        public StoreContainer CurrentDisplay => currentDisplay;
        public int AreaMask => navAgent != null ? navAgent.areaMask : NavMesh.AllAreas;

        [Title("Movement")]
        [MinValue(0.1f)] [SerializeField] private float arriveDistance = 0.5f;

        [Title("Walkable areas")]
        [Tooltip("Áreas de NavMesh por las que el NPC PUEDE caminar — su único cerco. Elegí las exactas desde Navigation → Areas (típico: ShopFrontDesk + Outside) para que nunca rutee por el breeding room. La cola de la caja hereda esta máscara. Vacío o nombres inexistentes → sin restricción (AllAreas).")]
        [ValueDropdown(nameof(EditorNavMeshAreaNames))]
        [SerializeField] private List<string> walkableAreaNames = new List<string> { "ShopFrontDesk", "Outside" };

        [Title("Per-instance variation")]
        [Tooltip("Variación ± aplicada por separado a velocidad, giro y aceleración del NavMeshAgent al instanciar (0.15 = ±15%). Cada cliente sale un poco más rápido/lento y gira distinto.")]
        [MinValue(0f)] [SerializeField] private float moveVariation = 0.15f;
        [Tooltip("Rango de avoidancePriority (0-99, menor = más prioridad) sorteado por cliente, para que se esquiven con personalidad en vez de empujarse igual.")]
        [SerializeField] private Vector2Int avoidancePriorityRange = new Vector2Int(30, 70);
        [Tooltip("Rango (s) del delay random de reacción: cuánto 'piensa' cada cliente antes de soltar su frase al cambiar de situación.")]
        [SerializeField] private Vector2 reactionDelayRange = new Vector2(0.2f, 1.2f);

        private const float displaySampleRadius = 1.5f;

        private IReadOnlyList<StoreContainer> displays;
        private CashRegister register;
        private NpcController controller;

        private NavMeshAgent navAgent;
        private int inspectionsRemaining;
        private StoreContainer currentDisplay;
        private StoreContainer reservedDisplay;
        private float inspectTimer;
        private float waitTimer;
        private Vector3 reservedQueueSlot;

        private bool leavingDestinationSet;
        private bool approachingSlotRequested;

        public void Initialize(CustomerArchetypeSO archetype, IReadOnlyList<StoreContainer> shopDisplays, CashRegister cashRegister, NpcController owner)
        {
            Archetype = archetype;
            DisplayName = NpcNameBank.GetRandomName();
            displays = shopDisplays;
            register = cashRegister;
            controller = owner;
            navAgent = GetComponent<NavMeshAgent>();
            ApplyWalkableAreas();
            ApplyInstanceVariation();
            inspectionsRemaining = UnityEngine.Random.Range(archetype.MinInspections, archetype.MaxInspections + 1);
            TransitionTo(NpcState.Wandering);
        }

        private void ApplyWalkableAreas()
        {
            int mask = 0;
            if (walkableAreaNames != null)
                foreach (var areaName in walkableAreaNames)
                {
                    int idx = NavMesh.GetAreaFromName(areaName);
                    if (idx >= 0) mask |= 1 << idx;
                }
            navAgent.areaMask = mask != 0 ? mask : NavMesh.AllAreas;
        }

        private static System.Collections.Generic.IEnumerable<string> EditorNavMeshAreaNames()
        {
#if UNITY_EDITOR
            return NavMesh.GetAreaNames();
#else
            return System.Array.Empty<string>();
#endif
        }

        private void ApplyInstanceVariation()
        {
            float v = moveVariation;
            navAgent.speed        *= UnityEngine.Random.Range(Mathf.Max(0.1f, 1f - v), 1f + v);
            navAgent.angularSpeed *= UnityEngine.Random.Range(Mathf.Max(0.1f, 1f - v), 1f + v);
            navAgent.acceleration *= UnityEngine.Random.Range(Mathf.Max(0.1f, 1f - v), 1f + v);
            navAgent.avoidancePriority = UnityEngine.Random.Range(avoidancePriorityRange.x, avoidancePriorityRange.y + 1);
            ReactionDelay = UnityEngine.Random.Range(reactionDelayRange.x, reactionDelayRange.y);
        }

        private void Update()
        {
            switch (State)
            {
                case NpcState.Wandering:            TickWandering();            break;
                case NpcState.InspectingDisplay:    TickInspecting();           break;
                case NpcState.ApproachingRegister:  TickApproachingRegister();  break;
                case NpcState.Queueing:             TickQueueing();             break;
                case NpcState.WaitingAtRegister:    TickWaiting();              break;
                case NpcState.Negotiating:                                      break;
                case NpcState.Leaving:              TickLeaving();              break;
            }
        }

        private void TickWandering()
        {
            ReleaseDisplaySlot();

            var candidates = new List<StoreContainer>();
            foreach (var d in displays)
            {
                if (d != null && d.Occupants != null && d.Occupants.Count > 0)
                    candidates.Add(d);
            }

            if (candidates.Count == 0)
            {
                TransitionTo(NpcState.Leaving);
                return;
            }

            int start = UnityEngine.Random.Range(0, candidates.Count);
            for (int k = 0; k < candidates.Count; k++)
            {
                var d = candidates[(start + k) % candidates.Count];
                if (d.TryReserveUsePoint(this, transform.position, navAgent.areaMask, displaySampleRadius, out var usePos))
                {
                    reservedDisplay = d;
                    currentDisplay  = d;
                    navAgent.SetDestination(usePos);
                    TransitionTo(NpcState.InspectingDisplay);
                    return;
                }
            }

            TransitionTo(NpcState.Leaving);
        }

        private void ReleaseDisplaySlot()
        {
            if (reservedDisplay != null) { reservedDisplay.ReleaseUsePoint(this); reservedDisplay = null; }
        }

        private void TickInspecting()
        {
            if (navAgent.pathPending) return;
            if (navAgent.remainingDistance > arriveDistance + 0.01f) return;

            inspectTimer += Time.deltaTime;
            if (inspectTimer < Archetype.InspectionDuration) return;

            var pick = BestPickFromContainer(currentDisplay);
            inspectionsRemaining--;

            if (pick != null)
            {
                TargetMM = pick;
                var svc = CustomerService.Instance;
                if (svc != null)
                {
                    InitialOffer = svc.Valuation.Estimate(pick, Archetype, svc.Pricing);
                    CurrentOffer = InitialOffer;
                }
                HasCounteredOnce = false;
                GameEvents.CustomerDecided(this, pick);
                TransitionTo(NpcState.ApproachingRegister);
                return;
            }

            if (inspectionsRemaining > 0)
            {
                TransitionTo(NpcState.Wandering);
                return;
            }

            TransitionTo(NpcState.Leaving);
        }

        private void TickApproachingRegister()
        {
            if (approachingSlotRequested) return;

            approachingSlotRequested = true;
            var slot = register.TryReserveSlot(this);
            if (slot == null)
            {
                Reason = LeaveReason.QueueFull;
                TransitionTo(NpcState.Leaving);
                return;
            }

            reservedQueueSlot = slot.Value;
            navAgent.SetDestination(reservedQueueSlot);
            TransitionTo(NpcState.Queueing);
        }

        private void TickQueueing()
        {
            var slot = register.CurrentSlotOf(this);
            if (slot.HasValue && (slot.Value - reservedQueueSlot).sqrMagnitude > 0.04f)
            {
                reservedQueueSlot = slot.Value;
                navAgent.SetDestination(reservedQueueSlot);
            }

            float dist = Vector3.Distance(transform.position, reservedQueueSlot);

            if (register.IsFrontSlot(this) && dist < arriveDistance)
            {
                GameEvents.CustomerArrivedAtRegister(this);
                waitTimer = 0f;
                TransitionTo(NpcState.WaitingAtRegister);
                return;
            }

            if (!navAgent.pathPending && navAgent.remainingDistance > arriveDistance + 0.01f)
            {
                navAgent.SetDestination(reservedQueueSlot);
            }
        }

        private void TickWaiting()
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= Archetype.WaitTimeoutSeconds)
                TransitionTo(NpcState.Leaving);
        }

        private void TickLeaving()
        {
            if (!leavingDestinationSet) return;

            if (!navAgent.pathPending && navAgent.remainingDistance < arriveDistance)
            {
                bool wasSold = TargetMM != null && TargetMM.BusyState == BusyReason.Sold;
                GameEvents.CustomerLeft(this, wasSold);
                controller.Despawn(this);
            }
        }

        public void AcceptCurrentOffer()
        {
            if (TargetMM == null) { TransitionTo(NpcState.Leaving); return; }
            TargetMM.BusyState = BusyReason.Sold;
            TargetMM.SaleDate  = DateTime.UtcNow;
            Reason = LeaveReason.Purchased;
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.Inventory?.AddDabloons(CurrentOffer);
                GameEvents.CustomerSold(this, TargetMM, CurrentOffer);
                GameEvents.RegistryChanged(gm.Registry);
                GameEvents.InventoryChanged(gm.Inventory);
            }
            TransitionTo(NpcState.Leaving);
        }

        public bool TryCounterOffer()
        {
            if (HasCounteredOnce) return false;
            var svc = CustomerService.Instance;
            if (svc == null) return false;
            int candidate = svc.Negotiation.ComputeCounter(CurrentOffer, svc.Pricing);
            var decision = svc.Negotiation.EvaluateCounter(Archetype);
            HasCounteredOnce = true;
            if (decision == NegotiationResult.Accept)
            {
                CurrentOffer = candidate;
                return true;
            }
            TransitionTo(NpcState.Leaving);
            return false;
        }

        public void RejectByPlayer()
        {
            TransitionTo(NpcState.Leaving);
        }

        public void EnterNegotiating() => TransitionTo(NpcState.Negotiating);

        public void ExitNegotiating()
        {
            if (State == NpcState.Negotiating) TransitionTo(NpcState.WaitingAtRegister);
        }

        private void TransitionTo(NpcState next)
        {
            State = next;

            if (next == NpcState.Wandering)
            {
            }
            else if (next == NpcState.InspectingDisplay)
            {
                inspectTimer = 0f;
            }
            else if (next == NpcState.ApproachingRegister)
            {
                approachingSlotRequested = false;
                ReleaseDisplaySlot();
            }
            else if (next == NpcState.WaitingAtRegister)
            {
                waitTimer = 0f;
            }
            else if (next == NpcState.Leaving)
            {
                ReleaseDisplaySlot();
                leavingDestinationSet = false;
                if (register != null) register.ReleaseSlot(this);
                if (navAgent != null && navAgent.isOnNavMesh && controller != null && controller.ExitPoint != null)
                {
                    navAgent.SetDestination(controller.ExitPoint.position);
                    leavingDestinationSet = true;
                }
            }
        }

        private CreatureDNA BestPickFromContainer(StoreContainer container)
        {
            if (container == null) return null;
            CreatureDNA best = null;
            int bestPrice = int.MinValue;
            var svc = CustomerService.Instance;
            if (svc == null) return null;
            foreach (var occ in container.Occupants)
            {
                var dna = occ?.DNA;
                if (dna == null) continue;
                if (dna.IsBusy) continue;
                int price = svc.Valuation.Estimate(dna, Archetype, svc.Pricing);
                if (price > bestPrice) { bestPrice = price; best = dna; }
            }
            return best;
        }

        private void OnEnable() => GameEvents.OnCustomerSold += OnSomeoneSold;

        private void OnDisable()
        {
            GameEvents.OnCustomerSold -= OnSomeoneSold;
            ReleaseDisplaySlot();
            if (register != null) register.ReleaseSlot(this);
        }

        private void OnSomeoneSold(NpcAgent buyer, CreatureDNA mm, int finalPrice)
        {
            if (buyer == this || State == NpcState.Leaving) return;
            if (TargetMM == null || mm != TargetMM) return;
            Reason = LeaveReason.Outbid;
            TransitionTo(NpcState.Leaving);
        }
    }
}
