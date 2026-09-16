using System;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

public class ArenaFloorPanel
{
    private readonly ArenaRunDirector director;
    private readonly Action onContinued;

    private readonly Label floorLabel;
    private readonly Button continueButton;
    private readonly Button retreatButton;
    private readonly Button giveUpButton;
    private readonly Button playButton;
    private readonly Button roomButton;
    private readonly Button paletteButton;
    private readonly Button castButton;
    private readonly Button pickButton;
    private readonly Button shuffleButton;

    public ArenaFloorPanel(VisualElement root, ArenaRunDirector director, Action onContinued)
    {
        this.director = director;
        this.onContinued = onContinued;

        floorLabel = root.Q<Label>("plan-floor");
        continueButton = root.Q<Button>("btn-continue");
        retreatButton = root.Q<Button>("btn-retreat");
        giveUpButton = root.Q<Button>("btn-giveup");
        playButton = root.Q<Button>("btn-play");
        roomButton = root.Q<Button>("btn-room");
        paletteButton = root.Q<Button>("btn-palette");
        castButton = root.Q<Button>("btn-cast");
        pickButton = root.Q<Button>("btn-pick");
        shuffleButton = root.Q<Button>("btn-shuffle");

        continueButton.clicked += OnContinueClicked;
        retreatButton.clicked += OnRetreatClicked;
        giveUpButton.clicked += OnGiveUpClicked;
    }

    public void Dispose()
    {
        continueButton.clicked -= OnContinueClicked;
        retreatButton.clicked -= OnRetreatClicked;
        giveUpButton.clicked -= OnGiveUpClicked;
    }

    public void Refresh()
    {
        if (director == null || !director.Active)
        {
            floorLabel.style.display = DisplayStyle.None;
            continueButton.style.display = DisplayStyle.None;
            retreatButton.style.display = DisplayStyle.None;
            giveUpButton.style.display = DisplayStyle.None;
            return;
        }

        roomButton.style.display = DisplayStyle.None;
        paletteButton.style.display = DisplayStyle.None;
        castButton.style.display = DisplayStyle.None;
        pickButton.style.display = DisplayStyle.None;
        shuffleButton.style.display = DisplayStyle.None;

        ArenaRun run = director.Run;
        floorLabel.style.display = DisplayStyle.Flex;

        if (run.Lost)
        {
            floorLabel.text = $"Perdiste en el piso {run.Floor} · botín perdido\n{TeamHealthLine(run)}";
            floorLabel.EnableInClassList("plan-floor--lost", true);
            playButton.style.display = DisplayStyle.None;
            continueButton.style.display = DisplayStyle.None;
            retreatButton.style.display = DisplayStyle.None;
            giveUpButton.style.display = DisplayStyle.Flex;
            giveUpButton.text = "Volver a la tienda";
            return;
        }

        floorLabel.EnableInClassList("plan-floor--lost", false);
        giveUpButton.style.display = DisplayStyle.None;

        if (director.FloorRecorded)
        {
            floorLabel.text = (run.CurrentKind == ArenaFloorKind.Buff
                ? $"Piso {run.Floor} superado · llevás {run.Material} material"
                : $"Piso {run.Floor} terminado · llevás {run.Material} material") + $"\n{TeamHealthLine(run)}";

            bool anyDown = AnyDown(run);
            playButton.style.display = DisplayStyle.None;
            continueButton.style.display = DisplayStyle.Flex;
            continueButton.text = $"Seguir → Piso {run.NextFloor}: {KindText(run.NextKind)}" + (anyDown ? " · riesgo: hay caídas" : "");
            retreatButton.style.display = DisplayStyle.Flex;
            retreatButton.text = $"Retirarse (asegura {run.Material})";
        }
        else
        {
            floorLabel.text = $"Piso {run.Floor} · {KindText(run.CurrentKind)}\n{TeamHealthLine(run)}";
            playButton.style.display = DisplayStyle.Flex;
            continueButton.style.display = DisplayStyle.None;
            retreatButton.style.display = run.Floor > 1 ? DisplayStyle.Flex : DisplayStyle.None;
            retreatButton.text = $"Retirarse (asegura {run.Material})";
        }
    }

    private static bool AnyDown(ArenaRun run)
    {
        for (int i = 0; i < run.TeamIds.Count; i++)
            if (run.IsDown(run.TeamIds[i])) return true;
        return false;
    }

    private string TeamHealthLine(ArenaRun run)
    {
        var sb = new StringBuilder("Vida: ");
        for (int i = 0; i < run.TeamIds.Count; i++)
        {
            string id = run.TeamIds[i];
            if (i > 0) sb.Append(" · ");
            string name = director.TeamNames.TryGetValue(id, out var n) ? n : "?";
            sb.Append(name).Append(' ').Append(Mathf.RoundToInt(run.HealthOf(id)));
            if (run.IsDown(id)) sb.Append(" (caída)");
        }
        return sb.ToString();
    }

    private void OnContinueClicked()
    {
        director.Continue();
        onContinued?.Invoke();
    }

    private void OnRetreatClicked() => director.Retreat();

    private void OnGiveUpClicked() => director.GiveUp();

    private static string KindText(ArenaFloorKind kind) => kind == ArenaFloorKind.Enemies ? "Enemigos" : "Buffo: +" + ArenaRun.BuffHealth + " vida y minerales gratis";
}
}
