using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

// One equippable item. Stat modifiers and (Stage 2) combat procs live together in
// the polymorphic Effects list. A creature equips it by storing this asset's ID in
// CreatureDNA.Equipped[Slot] — the ID is the persisted contract (light, cloud-safe),
// the SO is resolved against EquipmentDatabaseSO.
[CreateAssetMenu(fileName = "Equipment", menuName = "RunRunSimulator/Equipment/Equipment Item")]
public class EquipmentSO : SerializedScriptableObject
{
    [HorizontalGroup("Header", Width = 65)]
    [PreviewField(55, ObjectFieldAlignment.Left), HideLabel]
    public Sprite Icon;

    [VerticalGroup("Header/Info"), LabelWidth(55), ReadOnly]
    public string ID;

    [VerticalGroup("Header/Info"), LabelWidth(55)]
    public string Name;

    [VerticalGroup("Header/Info"), LabelWidth(55)]
    public EquipmentSlot Slot;

    [VerticalGroup("Header/Info"), LabelWidth(55)]
    [GUIColor(nameof(GetRarityColor))]
    public Rarity Rarity;

    [VerticalGroup("Header/Info"), LabelWidth(55)]
    [Tooltip("Color del ícono cuando el ítem todavía no tiene sprite asignado.")]
    public Color IconColor = new Color(0.45f, 0.45f, 0.55f);

    [Title("Descripción")]
    [HideLabel, MultiLineProperty(3)]
    public string Description;

    [Title("Effects")]
    [OdinSerialize]
    [ListDrawerSettings(DefaultExpandedState = true)]
    public List<EquipmentEffectBase> Effects = new List<EquipmentEffectBase>();

    [ShowInInspector, ReadOnly, MultiLineProperty(3), LabelText("Resumen")]
    private string EffectsSummary =>
        Effects == null || Effects.Count == 0
            ? "—"
            : string.Join("\n", Effects.Where(e => e != null).Select(e => $"• {e.Summary()}"));


    private Color GetRarityColor() => BodyPart.RarityColor(Rarity);
}
}
