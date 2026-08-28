using UnityEngine;
using UnityEngine.UIElements;

namespace MoriMonchiSimulator.CombatPrototype
{
    public class EnemyBriefPanel : MonoBehaviour
    {
        [SerializeField] private UIDocument document;

        private VisualElement panel;

        public void Show(EnemyUnit enemy, Vector2 screenPosition)
        {
            if (document == null || enemy == null || enemy.Definition == null) return;
            if (panel == null || panel.panel == null || (document.rootVisualElement != null && panel.panel != document.rootVisualElement.panel)) BuildPanel();
            panel.Clear();
            Populate(enemy.Definition);

            float left = screenPosition.x + 12f;
            if (left > Screen.width - 260) left -= 280f;
            panel.style.left = left;
            panel.style.top = (Screen.height - screenPosition.y) + 12f;
            panel.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            if (panel != null) panel.style.display = DisplayStyle.None;
        }

        private void BuildPanel()
        {
            panel = new VisualElement();
            panel.style.position = Position.Absolute;
            panel.style.width = 240;
            panel.style.backgroundColor = Hex("#0D0F14F0");
            panel.style.borderTopWidth = panel.style.borderBottomWidth = panel.style.borderLeftWidth = panel.style.borderRightWidth = 2;
            panel.style.borderTopColor = panel.style.borderBottomColor = panel.style.borderLeftColor = panel.style.borderRightColor = Hex("#C33");
            panel.style.borderTopLeftRadius = panel.style.borderTopRightRadius = panel.style.borderBottomLeftRadius = panel.style.borderBottomRightRadius = 8;
            panel.style.paddingTop = panel.style.paddingBottom = panel.style.paddingLeft = panel.style.paddingRight = 10;
            panel.pickingMode = PickingMode.Ignore;
            panel.style.display = DisplayStyle.None;
            document.rootVisualElement.Add(panel);
        }

        private void Populate(EnemyDefinitionSO def)
        {
            Label title = new Label(def.DisplayName);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 14;
            title.style.color = Color.white;
            panel.Add(title);

            Label ticksLine = new Label("Guardia " + def.GuardTicks + " · Gracia " + def.FinisherTicks);
            ticksLine.style.fontSize = 12;
            ticksLine.style.color = Hex("#F88");
            panel.Add(ticksLine);

            if (def.BriefLines == null) return;
            for (int i = 0; i < def.BriefLines.Length; i++)
            {
                Label line = new Label("· " + def.BriefLines[i]);
                line.style.fontSize = 12;
                line.style.color = Hex("#DDD");
                line.style.whiteSpace = WhiteSpace.Normal;
                panel.Add(line);
            }
        }

        private static Color Hex(string html)
        {
            ColorUtility.TryParseHtmlString(html, out Color color);
            return color;
        }
    }
}
