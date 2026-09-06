using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

[RequireComponent(typeof(UIDocument))]
public class ArenaResultPanel : MonoBehaviour
{
    private VisualElement root;
    private Label titleLabel;
    private VisualElement playerColumn;
    private VisualElement rivalColumn;

    private void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement.Q("result-root");
        if (root == null) return;

        titleLabel = root.Q<Label>("result-title");
        playerColumn = root.Q("result-player");
        rivalColumn = root.Q("result-rival");

        Hide();
    }

    public void Show(ExpeditionTeam winner, int mine, int theirs, IReadOnlyList<ArenaRoundStat> stats)
    {
        if (root == null) return;

        titleLabel.RemoveFromClassList("result__title--win");
        titleLabel.RemoveFromClassList("result__title--lose");
        titleLabel.RemoveFromClassList("result__title--draw");

        switch (winner)
        {
            case ExpeditionTeam.Player:
                titleLabel.text = $"Ganaste {mine}-{theirs}";
                titleLabel.AddToClassList("result__title--win");
                break;
            case ExpeditionTeam.Rival:
                titleLabel.text = $"Perdiste {mine}-{theirs}";
                titleLabel.AddToClassList("result__title--lose");
                break;
            default:
                titleLabel.text = $"Empate {mine}-{theirs}";
                titleLabel.AddToClassList("result__title--draw");
                break;
        }

        playerColumn.Clear();
        rivalColumn.Clear();

        var player = new List<ArenaRoundStat>();
        var rival = new List<ArenaRoundStat>();
        foreach (var stat in stats)
        {
            if (stat.Team == ExpeditionTeam.Rival) rival.Add(stat);
            else player.Add(stat);
        }

        player.Sort((a, b) => b.Secured.CompareTo(a.Secured));
        rival.Sort((a, b) => b.Secured.CompareTo(a.Secured));

        foreach (var stat in player) playerColumn.Add(BuildRow(stat));
        foreach (var stat in rival) rivalColumn.Add(BuildRow(stat));

        root.AddToClassList("result--show");
    }

    public void Hide()
    {
        if (root == null) return;
        root.RemoveFromClassList("result--show");
    }

    private static VisualElement BuildRow(ArenaRoundStat stat)
    {
        var row = new VisualElement();
        row.AddToClassList("result-row");

        var swatch = new VisualElement();
        swatch.AddToClassList("result-row__swatch");
        swatch.style.backgroundColor = stat.Color;
        row.Add(swatch);

        var column = new VisualElement();
        var name = new Label(stat.Name);
        name.AddToClassList("result-row__name");
        var stats = new Label($"{Verb(stat.Occupation)}  ·  aseguró {stat.Secured}  ·  minó {stat.Collected}  ·  tumbó {stat.HitsLanded}  ·  cayó {stat.TimesKnocked}");
        stats.AddToClassList("result-row__stats");
        column.Add(name);
        column.Add(stats);
        row.Add(column);

        return row;
    }

    private static string Verb(Occupation occupation)
    {
        switch (occupation)
        {
            case Occupation.Guard: return "vigiló";
            case Occupation.Break: return "rompió";
            case Occupation.Decoy: return "distrajo";
            case Occupation.Explore: return "exploró";
            default: return "recolectó";
        }
    }
}
}
