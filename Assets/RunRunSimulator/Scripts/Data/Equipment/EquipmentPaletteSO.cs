using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{
    [CreateAssetMenu(fileName = "EquipmentPalette", menuName = "RunRunSimulator/Equipment/Equipment Palette")]
    public class EquipmentPaletteSO : SerializedScriptableObject
    {
        [Title("Rareza → color (pastel, para el nombre del ítem)")]
        [OdinSerialize]
        private Dictionary<Rarity, Color> rarityColors = new Dictionary<Rarity, Color>();

        [Title("Slot → color (acento de la card)")]
        [OdinSerialize]
        private Dictionary<EquipmentSlot, Color> slotColors = new Dictionary<EquipmentSlot, Color>();

        [Button("Seteo base (pastel)", ButtonSizes.Large), GUIColor(0.4f, 1f, 0.6f)]
        private void SetupDefaults()
        {
            rarityColors = new Dictionary<Rarity, Color>
            {
                { Rarity.Common,    new Color(0.78f, 0.80f, 0.82f) },
                { Rarity.Uncommon,  new Color(0.62f, 0.82f, 0.60f) },
                { Rarity.Rare,      new Color(0.58f, 0.72f, 0.92f) },
                { Rarity.Epic,      new Color(0.76f, 0.62f, 0.90f) },
                { Rarity.Legendary, new Color(0.95f, 0.80f, 0.50f) },
            };

            slotColors = new Dictionary<EquipmentSlot, Color>();
            foreach (EquipmentSlot s in System.Enum.GetValues(typeof(EquipmentSlot)))
                slotColors[s] = DefaultSlotColor(s);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public Color RarityColor(Rarity r) =>
            rarityColors != null && rarityColors.TryGetValue(r, out var c) ? c : BodyPart.RarityColor(r);

        public Color SlotColor(EquipmentSlot s) =>
            slotColors != null && slotColors.TryGetValue(s, out var c) ? c : DefaultSlotColor(s);

        private static Color DefaultSlotColor(EquipmentSlot s) => s switch
        {
            EquipmentSlot.Weapon => new Color(0.85f, 0.45f, 0.40f),
            EquipmentSlot.Armor  => new Color(0.45f, 0.62f, 0.85f),
            EquipmentSlot.Amulet => new Color(0.70f, 0.55f, 0.85f),
            _                    => new Color(0.50f, 0.50f, 0.58f),
        };
    }
}
