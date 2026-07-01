using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{
    [CreateAssetMenu(fileName = "CombatPopupPalette", menuName = "RunRunSimulator/Combat/Combat Popup Palette")]
    public class CombatPopupPaletteSO : SerializedScriptableObject
    {
        [Title("Tipo de popup → color")]
        [OdinSerialize]
        private Dictionary<CombatPopupKind, Color> colors = new Dictionary<CombatPopupKind, Color>();

        [Button("Seteo base", ButtonSizes.Large), GUIColor(0.4f, 1f, 0.6f)]
        private void SetupDefaults()
        {
            colors = new Dictionary<CombatPopupKind, Color>
            {
                { CombatPopupKind.Hit,    new Color(1f,    1f,    1f,    1f) },
                { CombatPopupKind.Crit,   new Color(1f,    0.82f, 0.23f, 1f) },
                { CombatPopupKind.Poison, new Color(0.50f, 0.82f, 0.31f, 1f) },
                { CombatPopupKind.Burn,   new Color(1f,    0.48f, 0.18f, 1f) },
                { CombatPopupKind.Thorns, new Color(0.75f, 0.31f, 0.30f, 1f) },
                { CombatPopupKind.Heal,   new Color(0.31f, 0.88f, 0.48f, 1f) },
                { CombatPopupKind.Regen,  new Color(0.56f, 0.88f, 0.69f, 1f) },
                { CombatPopupKind.Stun,   new Color(0.98f, 0.85f, 0.30f, 1f) },
            };

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public Color GetColor(CombatPopupKind kind) =>
            colors != null && colors.TryGetValue(kind, out var c) ? c : Color.white;
    }
}
