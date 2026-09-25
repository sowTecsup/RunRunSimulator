using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoriMonchiSimulator
{
    [RequireComponent(typeof(UIDocument))]
    public class SlimeAnimLabPanel : MonoBehaviour
    {
        [SerializeField] private List<SlimeAnimLabLoop> loops;

        private VisualElement buttonsContainer;
        private Button replayAllButton;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            buttonsContainer = root.Q("slime-lab__buttons");
            replayAllButton = root.Q<Button>("slime-lab-all");

            if (buttonsContainer == null) return;

            buttonsContainer.Clear();
            foreach (var loop in loops)
            {
                if (loop == null) continue;
                var button = new Button(loop.Replay) { text = loop.Label };
                button.AddToClassList("slime-lab__btn");
                buttonsContainer.Add(button);
            }

            if (replayAllButton != null) replayAllButton.clicked += ReplayAll;
        }

        private void OnDisable()
        {
            if (replayAllButton != null) replayAllButton.clicked -= ReplayAll;
            if (buttonsContainer != null) buttonsContainer.Clear();
        }

        private void ReplayAll()
        {
            foreach (var loop in loops)
            {
                if (loop == null) continue;
                loop.Replay();
            }
        }
    }
}
