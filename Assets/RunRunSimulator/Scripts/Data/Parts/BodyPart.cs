using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

public abstract class BodyPart : SerializedScriptableObject
{
    [HorizontalGroup("Header", Width = 65)]
    [PreviewField(55, ObjectFieldAlignment.Left), HideLabel]
    public Sprite Icon;

    [VerticalGroup("Header/Info"), LabelWidth(55)]
    public string ID;

    [VerticalGroup("Header/Info"), LabelWidth(55)]
    public string Name;

    [VerticalGroup("Header/Info"), LabelWidth(55)]
    [GUIColor(nameof(GetRarityColor))]
    public Rarity Rarity;

    [VerticalGroup("Header/Info"), LabelWidth(55)]
    [GUIColor(nameof(GetSetColor))]
    public PartSet Set;

    private Color GetRarityColor() => Rarity switch
    {
        Rarity.Common    => Color.white,
        Rarity.Uncommon  => new Color(0.5f, 1f, 0.5f),
        Rarity.Rare      => new Color(0.4f, 0.65f, 1f),
        Rarity.Epic      => new Color(0.85f, 0.45f, 1f),
        Rarity.Legendary => new Color(1f, 0.75f, 0.2f),
        _                => Color.white
    };

    private Color GetSetColor() => Set switch
    {
        PartSet.GooGang        => new Color(0.4f, 0.95f, 0.4f),
        PartSet.BogBrigade     => new Color(0.55f, 0.75f, 0.25f),
        PartSet.FuzzFactory    => new Color(1f,   0.7f,  0.85f),
        PartSet.CosmicCreeps   => new Color(0.5f, 0.3f,  1f),
        PartSet.NeonNightmares => new Color(1f,   0.2f,  0.85f),
        PartSet.CrunchCrew     => new Color(0.85f, 0.65f, 0.2f),
        PartSet.GrimGlobs      => new Color(0.55f, 0.55f, 0.65f),
        PartSet.SpudSquad      => new Color(0.95f, 0.85f, 0.55f),
        PartSet.MoldMob        => new Color(0.65f, 0.85f, 0.3f),
        PartSet.ZapZone        => new Color(1f,   1f,   0.3f),
        _                      => Color.gray
    };
}
