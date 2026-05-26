using Sirenix.OdinInspector;
using UnityEngine;

[System.Serializable]
public class CreaturePartData
{
    [HorizontalGroup("Row", Width = 70)]
    [PreviewField(55, ObjectFieldAlignment.Left), HideLabel]
    public Sprite Sprite;

    [VerticalGroup("Row/Details"), LabelWidth(45)]
    public string ID;

    [VerticalGroup("Row/Details"), LabelWidth(45)]
    public string Name;
}
