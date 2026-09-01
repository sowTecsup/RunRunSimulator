using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

public static class UiPanels
{
    public static VisualElement RootOf(UIDocument document) =>
        document != null ? document.rootVisualElement : null;

    public static void SetActiveIndex(IList<VisualElement> items, int index, string activeClass)
    {
        for (int i = 0; i < items.Count; i++)
            items[i].EnableInClassList(activeClass, i == index);
    }

    public static int ClampSelection(int count, int index) =>
        count == 0 ? -1 : Mathf.Clamp(index, 0, count - 1);
}
}
