using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{
[DisallowMultipleComponent]
[RequireComponent(typeof(StoreContainer))]
public class StoreContainerPriceTagUITK : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    private StoreContainer container;
    private VisualElement root;
    private VisualElement list;

    private void Awake()
    {
        container = GetComponent<StoreContainer>();
    }

    private void OnEnable()
    {
        if (container != null) container.OnDisplayContentsChanged += HandleChanged;
    }

    private void OnDisable()
    {
        if (container != null) container.OnDisplayContentsChanged -= HandleChanged;
    }

    private void Start()
    {
        if (document == null) return;
        root = document.rootVisualElement;
        list = root?.Q<VisualElement>("price-list");
        Rebuild(container != null ? container.Occupants : null);
    }

    private void HandleChanged(IReadOnlyList<MoriMochiAgent> occupants) => Rebuild(occupants);

    private void Rebuild(IReadOnlyList<MoriMochiAgent> occupants)
    {
        if (list == null || root == null) return;
        list.Clear();
        if (occupants == null || occupants.Count == 0)
        {
            root.style.display = DisplayStyle.None;
            return;
        }
        root.style.display = DisplayStyle.Flex;
        var svc = CustomerService.Instance;
        foreach (var occ in occupants)
        {
            var dna = occ?.DNA;
            if (dna == null) continue;
            int price = svc != null ? svc.EstimateAverage(dna) : 0;
            var row = new Label($"{(string.IsNullOrEmpty(dna.CustomName) ? "MM" : dna.CustomName)} · {price} D");
            row.AddToClassList("price-row");
            list.Add(row);
        }
    }
}
}
