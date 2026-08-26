using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoriMonchiSimulator.CombatPrototype
{
    public class CombatPrototypeHUD : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private TargetingController targeting;
        private CombatPrototypeManager manager;
        private VisualElement bottomBar;
        private VisualElement beatStrip;
        private Label bannerLabel;
        private Button executeButton;
        private readonly List<PlayerCardView> playerCards = new List<PlayerCardView>();

        private static readonly Dictionary<CombatPhase, (string Text, int Size, Color Bg)> BannerByPhase = new Dictionary<CombatPhase, (string, int, Color)>
        {
            { CombatPhase.Planning, ("PLANIFICACIÓN — F1-F3 dragón · 1-3 plantilla · Enter confirma · Tab beat · Backspace deshace", 13, new Color(0f, 0f, 0f, 0.55f)) },
            { CombatPhase.Executing, ("EJECUTANDO...", 18, new Color(0f, 0f, 0f, 0.55f)) },
            { CombatPhase.EnemyTurn, ("TURNO ENEMIGO", 18, new Color(0f, 0f, 0f, 0.55f)) },
            { CombatPhase.Victory, ("VICTORIA — R para reiniciar", 18, new Color(0.1f, 0.35f, 0.12f, 0.85f)) },
            { CombatPhase.Defeat, ("DERROTA — R para reiniciar", 18, new Color(0.45f, 0.08f, 0.08f, 0.85f)) }
        };

        public void Bind(CombatPrototypeManager m)
        {
            manager = m;
            BuildUi();
        }

        public bool IsPointerOver(Vector2 screenPosition)
        {
            return false;
        }

        public void Refresh()
        {
            if (manager == null) return;
            if (bannerLabel == null || bannerLabel.panel == null) BuildUi();
            UpdateBanner();
            UpdateBeatStrip();
            UpdatePlayerCards();
            UpdateExecuteButton();
        }

        private void BuildUi()
        {
            if (document == null) return;
            VisualElement root = document.rootVisualElement;
            root.Clear();
            AnchorHorizontal(root);
            root.style.top = 0;
            root.style.bottom = 0;
            root.pickingMode = PickingMode.Ignore;
            playerCards.Clear();

            bannerLabel = MakeLabel(root, "", 18, Color.white, true);
            AnchorHorizontal(bannerLabel);
            bannerLabel.style.top = 10;
            bannerLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            SetPadding(bannerLabel, 6, 14);
            SetRadius(bannerLabel, 6);

            beatStrip = new VisualElement();
            AnchorHorizontal(beatStrip);
            beatStrip.style.bottom = 152;
            beatStrip.style.flexDirection = FlexDirection.Row;
            beatStrip.style.justifyContent = Justify.Center;
            beatStrip.pickingMode = PickingMode.Ignore;
            root.Add(beatStrip);

            bottomBar = new VisualElement();
            AnchorHorizontal(bottomBar);
            bottomBar.style.bottom = 8;
            bottomBar.style.flexDirection = FlexDirection.Row;
            bottomBar.style.justifyContent = Justify.Center;
            bottomBar.style.alignItems = Align.FlexEnd;
            bottomBar.pickingMode = PickingMode.Ignore;
            root.Add(bottomBar);

            List<PlayerUnit> players = manager != null && manager.Canonical != null ? manager.Canonical.GetPlayers() : new List<PlayerUnit>();
            for (int i = 0; i < players.Count; i++) bottomBar.Add(BuildPlayerCard(players[i], i));

            executeButton = new Button();
            executeButton.text = "EXECUTE";
            executeButton.style.width = 110;
            executeButton.style.height = 64;
            executeButton.style.fontSize = 16;
            executeButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            executeButton.style.color = Color.white;
            executeButton.style.backgroundColor = Hex("#2E7D32");
            executeButton.clicked += () => manager.ExecutePlan();
            bottomBar.Add(executeButton);
            Refresh();
        }

        private VisualElement BuildPlayerCard(PlayerUnit player, int slot)
        {
            VisualElement card = new VisualElement();
            card.style.width = 190;
            card.style.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 0.92f);
            SetBorderWidth(card, 2);
            SetRadius(card, 8);
            SetPadding(card, 8, 8);
            card.style.marginLeft = 6;
            card.style.marginRight = 6;
            card.pickingMode = PickingMode.Ignore;
            MakeLabel(card, "F" + (slot + 1) + " " + player.Definition.DisplayName, 14, Color.white, true);
            Label ticks = MakeLabel(card, "", 12, Hex("#DDD"));

            List<Label> abilityLabels = new List<Label>();
            for (int i = 0; i < 3; i++) abilityLabels.Add(MakeLabel(card, "", 12, Color.white));
            playerCards.Add(new PlayerCardView { UnitId = player.Id, Definition = player.Definition, Card = card, Ticks = ticks, AbilityLabels = abilityLabels });
            return card;
        }

        private void UpdateBanner()
        {
            if (bannerLabel == null) return;
            var phaseData = BannerByPhase[manager.Phase];
            bannerLabel.text = phaseData.Text;
            bannerLabel.style.fontSize = phaseData.Size;
            bannerLabel.style.backgroundColor = phaseData.Bg;
        }

        private void UpdateBeatStrip()
        {
            if (beatStrip == null) return;
            beatStrip.Clear();
            if (manager.Plan == null) return;

            List<Beat> beats = manager.Plan.Beats;
            for (int i = 0; i < beats.Count; i++)
            {
                Label chip = MakeLabel(beatStrip, "B" + (i + 1) + ": " + beats[i].Actions.Count, 11, Color.white);
                chip.style.backgroundColor = Hex("#1B1E27CC");
                SetBorderWidth(chip, 1);
                SetBorderColor(chip, i == beats.Count - 1 ? Hex("#FFD34D") : Hex("#555"));
                SetPadding(chip, 4, 8);
                chip.style.marginLeft = 3;
                chip.style.marginRight = 3;
            }
        }

        private void UpdatePlayerCards()
        {
            CombatSimState canonical = manager.Canonical;
            CombatSimState projected = manager.Projection != null ? manager.Projection.FinalState : null;
            for (int i = 0; i < playerCards.Count; i++)
            {
                PlayerCardView view = playerCards[i];
                CombatUnit unit = canonical != null ? canonical.GetUnit(view.UnitId) : null;
                int actualTicks = unit != null ? unit.Ticks : 0;
                string ticksText = "Ticks: " + actualTicks;
                if (projected != null)
                {
                    CombatUnit projectedUnit = projected.GetUnit(view.UnitId);
                    int projectedTicks = projectedUnit != null && projectedUnit.Alive ? projectedUnit.Ticks : 0;
                    if (projectedTicks != actualTicks) ticksText += " → " + projectedTicks;
                }
                view.Ticks.text = ticksText;

                bool selected = targeting != null && targeting.SelectedUnitId == view.UnitId;
                SetBorderColor(view.Card, selected ? Color.white : view.Definition.Tint);

                CombatAbilitySO[] abilities = view.Definition.Abilities;
                for (int a = 0; a < view.AbilityLabels.Count; a++)
                {
                    Label label = view.AbilityLabels[a];
                    if (abilities == null || a >= abilities.Length || abilities[a] == null)
                    {
                        label.text = "";
                        continue;
                    }

                    label.text = "[" + (a + 1) + "] " + abilities[a].DisplayName;
                    bool used = manager.Plan != null && manager.Plan.IsAbilityUsed(view.UnitId, a);
                    bool isSelected = selected && targeting != null && targeting.SelectedAbilityIndex == a;
                    if (used) label.style.color = Hex("#666");
                    else if (isSelected) label.style.color = Hex("#FFD34D");
                    else label.style.color = Color.white;
                }
            }
        }

        private void UpdateExecuteButton()
        {
            if (executeButton == null) return;
            bool enabled = manager.Phase == CombatPhase.Planning && manager.Plan != null && manager.Plan.TotalActions > 0;
            executeButton.SetEnabled(enabled);
        }

        private static Label MakeLabel(VisualElement parent, string text, int fontSize, Color color, bool bold = false)
        {
            Label label = new Label(text);
            label.style.fontSize = fontSize;
            label.style.color = color;
            if (bold) label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.pickingMode = PickingMode.Ignore;
            parent.Add(label);
            return label;
        }

        private static void AnchorHorizontal(VisualElement e)
        {
            e.style.position = Position.Absolute;
            e.style.left = 0;
            e.style.right = 0;
        }

        private static Color Hex(string html)
        {
            ColorUtility.TryParseHtmlString(html, out Color color);
            return color;
        }

        private static void SetBorderColor(VisualElement e, Color c)
        {
            e.style.borderTopColor = e.style.borderBottomColor = e.style.borderLeftColor = e.style.borderRightColor = c;
        }

        private static void SetBorderWidth(VisualElement e, float w)
        {
            e.style.borderTopWidth = e.style.borderBottomWidth = e.style.borderLeftWidth = e.style.borderRightWidth = w;
        }

        private static void SetRadius(VisualElement e, float r)
        {
            e.style.borderTopLeftRadius = e.style.borderTopRightRadius = e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = r;
        }

        private static void SetPadding(VisualElement e, float v, float h)
        {
            e.style.paddingTop = e.style.paddingBottom = v;
            e.style.paddingLeft = e.style.paddingRight = h;
        }

        private class PlayerCardView
        {
            public int UnitId;
            public PlayerUnitDefinitionSO Definition;
            public VisualElement Card;
            public Label Ticks;
            public List<Label> AbilityLabels;
        }
    }
}
