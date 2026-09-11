using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoriMonchiSimulator
{
public class ArenaPaletteControl : MonoBehaviour
{
    [Required, SerializeField] private UIDocument uiDocument;
    [Required, SerializeField] private ArenaSandbox sandbox;
    [Required, SerializeField] private ArenaPaletteApplier palette;

    private VisualElement container;
    private Button[] buttons;
    private Action[] handlers;
    private bool built;
    private int lastHighlightedIndex = -1;

    private void OnEnable()
    {
        TryBuild();
    }

    private void OnDisable()
    {
        if (buttons != null && handlers != null)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && handlers[i] != null)
                    buttons[i].clicked -= handlers[i];
            }
        }

        container?.Clear();
        container = null;
        buttons = null;
        handlers = null;
        built = false;
        lastHighlightedIndex = -1;
    }

    private void Update()
    {
        if (!built)
        {
            TryBuild();
            return;
        }

        if (buttons == null || buttons.Length == 0) return;

        int currentIndex = palette.CurrentIndex;
        if (currentIndex == lastHighlightedIndex) return;

        if (lastHighlightedIndex >= 0 && lastHighlightedIndex < buttons.Length)
            buttons[lastHighlightedIndex].RemoveFromClassList("hud-palette-btn--on");

        if (currentIndex >= 0 && currentIndex < buttons.Length)
            buttons[currentIndex].AddToClassList("hud-palette-btn--on");

        lastHighlightedIndex = currentIndex;
    }

    private void TryBuild()
    {
        if (built) return;
        if (uiDocument == null || sandbox == null || palette == null) return;

        var root = uiDocument.rootVisualElement;
        if (root == null) return;

        container = root.Q<VisualElement>("hud-palettes");
        if (container == null) return;

        if (palette.Palettes.Count == 0)
        {
            built = true;
            return;
        }

        buttons = new Button[palette.Palettes.Count];
        handlers = new Action[palette.Palettes.Count];

        for (int i = 0; i < palette.Palettes.Count; i++)
        {
            int index = i;
            var entry = palette.Palettes[i];
            var button = new Button { text = entry != null ? entry.DisplayName : "" };
            button.AddToClassList("hud-palette-btn");

            Action handler = () => sandbox.SetPaletteIndex(index);
            button.clicked += handler;

            container.Add(button);
            buttons[i] = button;
            handlers[i] = handler;
        }

        lastHighlightedIndex = -1;
        built = true;
    }
}
}
