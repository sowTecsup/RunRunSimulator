using UnityEngine;
using UnityEngine.UIElements;

namespace MoriMonchiSimulator.CombatPrototype
{
    public class TurnLogPanel : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private CombatPrototypeManager manager;

        private VisualElement panel;
        private ScrollView list;

        private void OnEnable()
        {
            if (manager != null) manager.TurnLogChanged += Rebuild;
            Rebuild();
        }

        private void OnDisable()
        {
            if (manager != null) manager.TurnLogChanged -= Rebuild;
        }

        private void Rebuild()
        {
            if (document == null || manager == null) return;
            if (panel == null || panel.panel == null) BuildPanel();

            list.Clear();

            if (manager.TurnLog.Count == 0)
            {
                panel.style.display = DisplayStyle.None;
                return;
            }

            panel.style.display = DisplayStyle.Flex;

            for (int i = manager.TurnLog.Count - 1; i >= 0; i--)
            {
                TurnLogEntry entry = manager.TurnLog[i];

                Label header = new Label("TURNO " + entry.Turn);
                header.style.unityFontStyleAndWeight = FontStyle.Bold;
                header.style.fontSize = 12;
                header.style.color = Hex("#FFD24A");
                header.style.marginTop = i == manager.TurnLog.Count - 1 ? 0 : 8;
                list.Add(header);

                for (int l = 0; l < entry.Lines.Count; l++)
                {
                    Label line = new Label(entry.Lines[l]);
                    line.style.fontSize = 11;
                    line.style.color = Hex("#DDD");
                    line.style.whiteSpace = WhiteSpace.Normal;
                    list.Add(line);
                }
            }
        }

        private void BuildPanel()
        {
            panel = new VisualElement();
            panel.style.position = Position.Absolute;
            panel.style.left = 10;
            panel.style.top = 120;
            panel.style.width = 230;
            panel.style.maxHeight = 380;
            panel.style.backgroundColor = Hex("#0D0F14E0");
            panel.style.borderTopWidth = panel.style.borderBottomWidth = panel.style.borderLeftWidth = panel.style.borderRightWidth = 1;
            panel.style.borderTopColor = panel.style.borderBottomColor = panel.style.borderLeftColor = panel.style.borderRightColor = Hex("#555");
            panel.style.borderTopLeftRadius = panel.style.borderTopRightRadius = panel.style.borderBottomLeftRadius = panel.style.borderBottomRightRadius = 8;
            panel.style.paddingTop = panel.style.paddingBottom = panel.style.paddingLeft = panel.style.paddingRight = 8;
            panel.pickingMode = PickingMode.Ignore;
            panel.style.display = DisplayStyle.None;

            Label title = new Label("TURNOS EJECUTADOS");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 11;
            title.style.color = Hex("#AAB6C2");
            title.style.marginBottom = 4;
            panel.Add(title);

            list = new ScrollView(ScrollViewMode.Vertical);
            list.pickingMode = PickingMode.Ignore;
            list.contentContainer.pickingMode = PickingMode.Ignore;
            panel.Add(list);

            document.rootVisualElement.Add(panel);
        }

        private static Color Hex(string html)
        {
            ColorUtility.TryParseHtmlString(html, out Color color);
            return color;
        }
    }
}
