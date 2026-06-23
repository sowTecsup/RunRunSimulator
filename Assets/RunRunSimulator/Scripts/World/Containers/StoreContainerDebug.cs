using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{
[DisallowMultipleComponent]
[RequireComponent(typeof(StoreContainer))]
public class StoreContainerDebug : MonoBehaviour
{
    private StoreContainer container;

    private void Awake() => container = GetComponent<StoreContainer>();

    [ShowInInspector, ReadOnly, PropertyOrder(0)]
    [LabelText("MoriMonchis adentro")]
    [TableList(IsReadOnly = true, AlwaysExpanded = true)]
    private List<InsideRow> Inside
    {
        get
        {
            var rows = new List<InsideRow>();
            if (container == null) container = GetComponent<StoreContainer>();
            if (container == null) return rows;
            var svc = CustomerService.Instance;
            foreach (var occ in container.Occupants)
            {
                var dna = occ?.DNA;
                if (dna == null) continue;
                rows.Add(new InsideRow
                {
                    Name   = string.IsNullOrEmpty(dna.CustomName) ? "MM" : dna.CustomName,
                    Gender = dna.Gender,
                    Busy   = dna.IsBusy,
                    Price  = svc != null ? svc.EstimateAverage(dna) : 0,
                });
            }
            return rows;
        }
    }

    [ShowInInspector, ReadOnly, PropertyOrder(1)]
    [LabelText("NPCs interactuando")]
    [TableList(IsReadOnly = true, AlwaysExpanded = true)]
    private List<NpcRow> Interacting
    {
        get
        {
            var rows = new List<NpcRow>();
            var ctrl = NpcController.Instance;
            if (ctrl == null) return rows;
            foreach (var npc in ctrl.Active)
            {
                if (npc == null || npc.CurrentDisplay != container) continue;
                rows.Add(new NpcRow
                {
                    Archetype = npc.Archetype != null ? npc.Archetype.DisplayName : "Cliente",
                    State     = npc.State,
                    Target    = npc.TargetMM != null
                        ? (string.IsNullOrEmpty(npc.TargetMM.CustomName) ? "MM" : npc.TargetMM.CustomName)
                        : "—",
                });
            }
            return rows;
        }
    }

    [Button(ButtonSizes.Medium), PropertyOrder(2)]
    [GUIColor(0.5f, 0.85f, 1f)]
    private void SpawnTestCustomer()
    {
        var ctrl = NpcController.Instance;
        if (ctrl == null) { Debug.LogWarning("[StoreContainerDebug] NpcController.Instance is null."); return; }
        ctrl.ForceSpawn();
    }

    private struct InsideRow
    {
        [ReadOnly] public string         Name;
        [ReadOnly] public CreatureGender Gender;
        [ReadOnly] public bool           Busy;
        [ReadOnly] public int            Price;
    }

    private struct NpcRow
    {
        [ReadOnly] public string            Archetype;
        [ReadOnly] public NpcAgent.NpcState State;
        [ReadOnly] public string            Target;
    }
}
}
