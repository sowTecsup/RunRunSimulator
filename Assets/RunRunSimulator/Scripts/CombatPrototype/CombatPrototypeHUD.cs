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
        private Label actionBudgetLabel;
        private Label bannerLabel;
        private Label selectionLabel;
        private Button executeButton;
        private VisualElement restartContainer;
        private Button restartButton;
        private readonly List<PlayerCardView> playerCards = new List<PlayerCardView>();

        private static readonly Dictionary<CombatPhase, (string Text, int Size, Color Bg)> BannerByPhase = new Dictionary<CombatPhase, (string, int, Color)>
        {
            { CombatPhase.Planning, ("PLANIFICACIÓN — F1-F3 dragón · 1-3 poder · clic: destino · arrastrá: orientar · soltá: confirmar\nWASD mover cámara · ←/→ girar · rueda zoom · Tab beat · Backspace deshace · clic der: info", 15, new Color(0f, 0f, 0f, 0.55f)) },
            { CombatPhase.Executing, ("EJECUTANDO...", 20, new Color(0f, 0f, 0f, 0.55f)) },
            { CombatPhase.EnemyTurn, ("TURNO ENEMIGO", 20, new Color(0f, 0f, 0f, 0.55f)) },
            { CombatPhase.Victory, ("VICTORIA — ¡LA SEMILLA GERMINÓ!", 20, new Color(0.1f, 0.35f, 0.12f, 0.85f)) },
            { CombatPhase.Defeat, ("DERROTA — la noche devoró la semilla", 20, new Color(0.45f, 0.08f, 0.08f, 0.85f)) },
            { CombatPhase.Setup, ("DESPLIEGUE NOCTURNO — la semilla solo crece de noche\n←/→ girar cámara · rueda: zoom", 16, new Color(0.10f, 0.07f, 0.22f, 0.8f)) },
            { CombatPhase.Spawning, ("REFUERZOS NOCTURNOS — llegan saltando desde el borde de la isla", 18, new Color(0.10f, 0.07f, 0.22f, 0.8f)) }
        };

        public void Bind(CombatPrototypeManager m)
        {
            manager = m;
            BuildUi();
        }

        private void OnEnable()
        {
            if (targeting != null) targeting.SelectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            if (targeting != null) targeting.SelectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            Refresh();
        }

        public bool IsPointerOver(Vector2 screenPosition)
        {
            if (document == null || document.rootVisualElement == null || document.rootVisualElement.panel == null) return false;
            IPanel panel = document.rootVisualElement.panel;
            Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));
            return panel.Pick(panelPosition) != null;
        }

        public void Refresh()
        {
            if (manager == null) return;
            if (bannerLabel == null || bannerLabel.panel == null) BuildUi();
            UpdateBanner();
            UpdateSelectionLabel();
            UpdateBeatStrip();
            UpdateActionBudget();
            UpdatePlayerCards();
            UpdateExecuteButton();
        }

        private void BuildUi()
        {
            if (document == null) return;
            VisualElement root = document.rootVisualElement;
            root.Clear();
            root.style.flexDirection = FlexDirection.Column;
            root.style.justifyContent = Justify.SpaceBetween;
            root.pickingMode = PickingMode.Ignore;
            playerCards.Clear();

            VisualElement topBand = new VisualElement();
            topBand.style.flexDirection = FlexDirection.Column;
            topBand.style.alignItems = Align.Stretch;
            topBand.pickingMode = PickingMode.Ignore;

            bannerLabel = MakeLabel(topBand, "", 18, Color.white, true);
            bannerLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            SetPadding(bannerLabel, 6, 14);
            SetRadius(bannerLabel, 6);
            bannerLabel.pickingMode = PickingMode.Position;
            bannerLabel.style.marginTop = 8;
            bannerLabel.style.marginLeft = 8;
            bannerLabel.style.marginRight = 8;

            selectionLabel = MakeLabel(topBand, "", 16, Color.white, true);
            selectionLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            SetPadding(selectionLabel, 4, 12);
            SetRadius(selectionLabel, 6);
            selectionLabel.pickingMode = PickingMode.Position;
            selectionLabel.style.marginTop = 4;
            selectionLabel.style.marginLeft = 120;
            selectionLabel.style.marginRight = 120;

            restartContainer = new VisualElement();
            restartContainer.style.flexDirection = FlexDirection.Row;
            restartContainer.style.justifyContent = Justify.Center;
            restartContainer.style.marginTop = 8;
            restartContainer.pickingMode = PickingMode.Ignore;
            restartContainer.style.display = DisplayStyle.None;

            restartButton = new Button();
            restartButton.text = "REINICIAR (R)";
            restartButton.style.fontSize = 18;
            restartButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            restartButton.style.color = Color.white;
            restartButton.style.backgroundColor = Hex("#2E7D32");
            restartButton.style.width = 200;
            restartButton.style.height = 44;
            SetRadius(restartButton, 6);
            restartButton.clicked += () => manager.RestartEncounter();
            restartContainer.Add(restartButton);
            topBand.Add(restartContainer);

            root.Add(topBand);

            VisualElement bottomBand = new VisualElement();
            bottomBand.style.flexDirection = FlexDirection.Column;
            bottomBand.style.alignItems = Align.Stretch;
            bottomBand.pickingMode = PickingMode.Ignore;

            actionBudgetLabel = MakeLabel(bottomBand, "", 13, Color.white, true);
            actionBudgetLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            actionBudgetLabel.style.marginBottom = 2;

            beatStrip = new VisualElement();
            beatStrip.style.flexDirection = FlexDirection.Row;
            beatStrip.style.justifyContent = Justify.Center;
            beatStrip.pickingMode = PickingMode.Ignore;
            beatStrip.style.marginBottom = 6;
            bottomBand.Add(beatStrip);

            bottomBar = new VisualElement();
            bottomBar.style.flexDirection = FlexDirection.Row;
            bottomBar.style.justifyContent = Justify.Center;
            bottomBar.style.alignItems = Align.FlexEnd;
            bottomBar.pickingMode = PickingMode.Ignore;
            bottomBar.style.marginBottom = 8;
            bottomBand.Add(bottomBar);

            root.Add(bottomBand);

            executeButton = new Button();
            executeButton.text = "EXECUTE";
            executeButton.style.width = 140;
            executeButton.style.height = 78;
            executeButton.style.fontSize = 20;
            executeButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            executeButton.style.color = Color.white;
            executeButton.style.backgroundColor = Hex("#2E7D32");
            executeButton.clicked += () => manager.ExecutePlan();

            List<PlayerUnit> players = manager != null && manager.Canonical != null ? manager.Canonical.GetPlayers() : new List<PlayerUnit>();
            for (int i = 0; i < players.Count; i++) bottomBar.Add(BuildPlayerCard(players[i], i));
            bottomBar.Add(executeButton);
            Refresh();
        }

        private VisualElement BuildPlayerCard(PlayerUnit player, int slot)
        {
            VisualElement card = new VisualElement();
            card.style.width = 250;
            card.style.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 0.92f);
            SetBorderWidth(card, 2);
            SetRadius(card, 8);
            SetPadding(card, 10, 10);
            card.style.marginLeft = 6;
            card.style.marginRight = 6;
            card.pickingMode = PickingMode.Position;
            MakeLabel(card, "F" + (slot + 1) + " " + player.Definition.DisplayName, 18, Color.white, true);
            Label ticks = MakeLabel(card, "", 15, Hex("#DDD"));

            CombatAbilitySO[] abilities = player.Definition.Abilities;
            List<Label> abilityLabels = new List<Label>();
            List<VisualElement> abilityRows = new List<VisualElement>();
            for (int i = 0; i < 3; i++)
            {
                CombatAbilitySO ability = abilities != null && i < abilities.Length ? abilities[i] : null;

                VisualElement row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginTop = 2;
                row.style.marginBottom = 2;
                SetPadding(row, 2, 4);
                SetRadius(row, 4);
                row.pickingMode = PickingMode.Ignore;

                Label label = new Label("");
                label.style.fontSize = 15;
                label.style.color = Color.white;
                label.pickingMode = PickingMode.Ignore;
                row.Add(label);

                if (ability != null)
                {
                    row.Add(AbilityCardVisuals.BuildAbilityMiniGrid(ability));
                    row.Add(AbilityCardVisuals.BuildAbilityTag(ability));
                }

                card.Add(row);
                abilityLabels.Add(label);
                abilityRows.Add(row);
            }

            playerCards.Add(new PlayerCardView { UnitId = player.Id, Definition = player.Definition, Card = card, Ticks = ticks, AbilityLabels = abilityLabels, AbilityRows = abilityRows });
            return card;
        }

        private void UpdateBanner()
        {
            if (bannerLabel == null) return;
            var phaseData = BannerByPhase[manager.Phase];
            bannerLabel.text = phaseData.Text;
            if (manager.Phase == CombatPhase.Planning && manager.Seed != null)
                bannerLabel.text = phaseData.Text + "\nSEMILLA: germina en " + Mathf.Max(0, manager.GerminationTurn - manager.TurnNumber) + " turnos · vida " + manager.Seed.Ticks + "/" + manager.Seed.MaxTicks;
            bannerLabel.style.fontSize = phaseData.Size;
            bannerLabel.style.backgroundColor = phaseData.Bg;

            if (restartContainer != null)
                restartContainer.style.display = manager.Phase == CombatPhase.Victory || manager.Phase == CombatPhase.Defeat ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void UpdateSelectionLabel()
        {
            if (selectionLabel == null) return;
            if (manager.Phase == CombatPhase.Setup)
            {
                if (manager.AwaitingSeed)
                {
                    selectionLabel.style.display = DisplayStyle.Flex;
                    selectionLabel.style.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
                    selectionLabel.text = "Plantá la SEMILLA NOCTURNA: clic en una celda libre";
                    selectionLabel.style.color = new Color(0.71f, 0.91f, 0.55f);
                    return;
                }

                PlayerUnitDefinitionSO def = manager.NextDeployDefinition;
                if (def == null)
                {
                    selectionLabel.style.display = DisplayStyle.None;
                    return;
                }

                selectionLabel.style.display = DisplayStyle.Flex;
                selectionLabel.style.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
                selectionLabel.text = "Desplegá a " + def.DisplayName + ": clic en una celda libre";
                Color setupTint = def.Tint;
                setupTint.a = 1f;
                selectionLabel.style.color = setupTint;
                return;
            }

            if (manager.Phase != CombatPhase.Planning || targeting == null)
            {
                selectionLabel.style.display = DisplayStyle.None;
                return;
            }

            selectionLabel.style.display = DisplayStyle.Flex;
            selectionLabel.style.backgroundColor = new Color(0f, 0f, 0f, 0.55f);

            if (targeting.SelectedUnitId < 0)
            {
                selectionLabel.text = "Elegí un dragón: F1-F3 o clic sobre él";
                selectionLabel.style.color = Color.white;
                return;
            }

            PlayerUnit player = manager.Canonical != null ? manager.Canonical.GetUnit(targeting.SelectedUnitId) as PlayerUnit : null;
            if (player == null)
            {
                selectionLabel.style.display = DisplayStyle.None;
                return;
            }

            Color tint = player.Definition.Tint;
            tint.a = 1f;
            selectionLabel.style.color = tint;

            if (targeting.SelectedAbilityIndex < 0)
            {
                selectionLabel.text = player.Definition.DisplayName + " — elegí plantilla: 1-3";
                return;
            }

            CombatAbilitySO[] abilities = player.Definition.Abilities;
            CombatAbilitySO ability = abilities != null && targeting.SelectedAbilityIndex < abilities.Length ? abilities[targeting.SelectedAbilityIndex] : null;
            string abilityName = ability != null ? ability.DisplayName : "";

            string guide;
            if (targeting.AwaitingSlamCell)
                guide = ": objetivo fijado — elegí la celda del slam y confirmá";
            else if (ability != null && ability.Type == AbilityType.Movement)
                guide = " · clic en la celda de destino y soltá";
            else if (ability != null && ability.Landing == LandingKind.Stay)
                guide = " · clic en la celda de impacto (no te movés) y soltá";
            else if (ability != null && ability.Targeting == TargetingMode.AirborneEnemy && !targeting.AwaitingSlamCell)
                guide = ": solo funciona contra un enemigo EN EL AIRE — lanzalo primero con la Voltereta del Ágil, después clic en el enemigo aéreo";
            else
                guide = " · clic en el DESTINO, arrastrá para orientar el golpe, soltá para confirmar";

            selectionLabel.text = player.Definition.DisplayName + " — " + abilityName + guide;
        }

        private void UpdateBeatStrip()
        {
            if (beatStrip == null) return;
            beatStrip.Clear();
            if (manager.Plan == null) return;

            if (manager.Plan.TotalActions == 0)
            {
                MakeLabel(beatStrip, "Elegí hasta " + Choreography.MaxActions + " acciones — clic destino + arrastre", 13, Hex("#AAB"));
                return;
            }

            List<Beat> beats = manager.Plan.Beats;
            Label lastChip = null;
            for (int b = 0; b < beats.Count; b++)
            {
                List<PlannedAction> actions = beats[b].Actions;
                for (int a = 0; a < actions.Count; a++)
                {
                    PlannedAction action = actions[a];
                    PlayerUnit pl = manager.Canonical != null ? manager.Canonical.GetUnit(action.UnitId) as PlayerUnit : null;
                    CombatAbilitySO ab = pl != null && pl.Definition.Abilities != null && action.AbilityIndex < pl.Definition.Abilities.Length ? pl.Definition.Abilities[action.AbilityIndex] : null;
                    string dragon = pl != null ? pl.Definition.DisplayName : "?";
                    string power = ab != null ? ab.DisplayName : "?";

                    Label chip = MakeLabel(beatStrip, "B" + (b + 1) + " · " + dragon + " → " + power, 14, Color.white, true);
                    chip.pickingMode = PickingMode.Position;
                    chip.style.backgroundColor = Hex("#1B1E27E6");
                    SetBorderWidth(chip, 2);
                    Color tint = pl != null ? pl.Definition.Tint : Color.white;
                    tint.a = 1f;
                    SetBorderColor(chip, tint);
                    SetPadding(chip, 5, 10);
                    SetRadius(chip, 6);
                    chip.style.marginLeft = 3;
                    chip.style.marginRight = 3;
                    lastChip = chip;
                }
            }

            if (lastChip != null)
            {
                SetBorderWidth(lastChip, 3);
                SetBorderColor(lastChip, Hex("#FFD34D"));
            }
        }

        private void UpdateActionBudget()
        {
            if (actionBudgetLabel == null) return;
            int used = manager.Plan != null ? manager.Plan.TotalActions : 0;
            actionBudgetLabel.text = "Acciones " + used + "/" + Choreography.MaxActions;
            actionBudgetLabel.style.color = used >= Choreography.MaxActions ? Hex("#FFD34D") : Color.white;
        }

        private void UpdatePlayerCards()
        {
            List<PlayerUnit> players = manager.Canonical != null ? manager.Canonical.GetPlayers() : new List<PlayerUnit>();
            if (playerCards.Count != players.Count && bottomBar != null)
            {
                bottomBar.Clear();
                playerCards.Clear();
                for (int i = 0; i < players.Count; i++) bottomBar.Add(BuildPlayerCard(players[i], i));
                bottomBar.Add(executeButton);
            }

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
                view.Card.style.backgroundColor = selected ? new Color(0.16f, 0.19f, 0.28f, 0.95f) : new Color(0.08f, 0.09f, 0.12f, 0.92f);

                CombatAbilitySO[] abilities = view.Definition.Abilities;
                for (int a = 0; a < view.AbilityLabels.Count; a++)
                {
                    Label label = view.AbilityLabels[a];
                    VisualElement row = view.AbilityRows[a];
                    if (abilities == null || a >= abilities.Length || abilities[a] == null)
                    {
                        label.text = "";
                        row.style.backgroundColor = Color.clear;
                        continue;
                    }

                    bool used = manager.Plan != null && manager.Plan.IsAbilityUsed(view.UnitId, a);
                    bool isSelected = selected && targeting != null && targeting.SelectedAbilityIndex == a;
                    if (used)
                    {
                        label.text = "✓ " + abilities[a].DisplayName;
                        label.style.color = Hex("#9AE6A0");
                        row.style.backgroundColor = Hex("#1E3320CC");
                    }
                    else if (isSelected)
                    {
                        label.text = "[" + (a + 1) + "] " + abilities[a].DisplayName;
                        label.style.color = Hex("#1B1E27");
                        row.style.backgroundColor = Hex("#FFD34D");
                    }
                    else
                    {
                        label.text = "[" + (a + 1) + "] " + abilities[a].DisplayName;
                        label.style.color = Color.white;
                        row.style.backgroundColor = Color.clear;
                    }
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
            public List<VisualElement> AbilityRows;
        }
    }
}
