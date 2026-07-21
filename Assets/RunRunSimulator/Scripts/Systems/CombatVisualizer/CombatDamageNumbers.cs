using System.Collections.Generic;
using DamageNumbersPro;
using UnityEngine;
namespace MoriMonchiSimulator
{
public class CombatDamageNumbers : MonoBehaviour
{
    [System.Serializable]
    private struct KindPrefabOverride
    {
        public CombatPopupKind Kind;
        public DamageNumber    Prefab;
    }

    [SerializeField] private CombatPopupPaletteSO palette;
    [SerializeField] private DamageNumber numberPrefab;
    [SerializeField] private List<KindPrefabOverride> prefabOverrides = new List<KindPrefabOverride>();
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.35f, 0f);
    [SerializeField, Min(1f)] private float critScale = 1.35f;
    [SerializeField] private float popupScale = 0.7f;
    [SerializeField] private float popupLifetime = 2.5f;
    [SerializeField] private float critOutlineWidth = 0.25f;
    [SerializeField] private Color critOutlineColor = new Color32(0xFF, 0xC8, 0x30, 0xFF);

    private static readonly HashSet<CombatPopupKind> NumericKinds = new HashSet<CombatPopupKind>
    {
        CombatPopupKind.Hit, CombatPopupKind.Crit, CombatPopupKind.Heal, CombatPopupKind.Regen,
        CombatPopupKind.Poison, CombatPopupKind.Burn, CombatPopupKind.Thorns, CombatPopupKind.Lifesteal,
    };

    private void OnEnable()  => CombatVisualEvents.OnPopup += HandlePopup;
    private void OnDisable() => CombatVisualEvents.OnPopup -= HandlePopup;

    private void HandlePopup(CombatVisualPopup p)
    {
        var prefab = ResolvePrefab(p.Kind);
        if (prefab == null) return;

        int rounded = Mathf.RoundToInt(p.Amount);
        if (NumericKinds.Contains(p.Kind) && rounded <= 0) return;

        var dn = prefab.Spawn(p.Position + spawnOffset, rounded);
        if (dn == null) return;

        dn.lifetime = popupLifetime;
        dn.SetScale(popupScale);

        dn.enableNumber = p.Kind != CombatPopupKind.Stun && rounded >= 1;

        bool suppressTopText = p.Kind == CombatPopupKind.Hit || p.Kind == CombatPopupKind.Crit
            || p.Kind == CombatPopupKind.Heal || p.Kind == CombatPopupKind.Regen;
        dn.enableTopText = !suppressTopText;
        if (!suppressTopText) dn.topText = string.IsNullOrEmpty(p.Text) ? Label(p.Kind) : p.Text;

        if (p.HasOverrideColor) dn.SetColor(p.OverrideColor);
        else if (palette != null) dn.SetColor(palette.GetColor(p.Kind));
        if (p.Follow != null) dn.SetFollowedTarget(p.Follow);

        if (p.Kind == CombatPopupKind.Hit || p.Kind == CombatPopupKind.Crit)
        {
            dn.enableLeftText = true;
            dn.leftText       = "-";
            dn.UpdateText();
        }
        else if (p.Kind == CombatPopupKind.Heal || p.Kind == CombatPopupKind.Regen)
        {
            dn.enableLeftText = true;
            dn.leftText       = "+";
            dn.UpdateText();
        }

        if (p.Kind == CombatPopupKind.Crit)
        {
            dn.SetScale(popupScale * critScale);
            foreach (var text in dn.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                text.outlineWidth = critOutlineWidth;
                text.outlineColor = critOutlineColor;
            }
        }
    }

    private DamageNumber ResolvePrefab(CombatPopupKind kind)
    {
        foreach (var o in prefabOverrides)
        {
            if (o.Kind == kind && o.Prefab != null) return o.Prefab;
        }
        return numberPrefab;
    }

    private static string Label(CombatPopupKind kind) => kind switch
    {
        CombatPopupKind.Hit    => "Golpe",
        CombatPopupKind.Crit   => "¡Crítico!",
        CombatPopupKind.Poison => "Veneno",
        CombatPopupKind.Burn   => "Quemadura",
        CombatPopupKind.Thorns => "Espinas",
        CombatPopupKind.Heal   => "Cura",
        CombatPopupKind.Regen  => "Regeneración",
        CombatPopupKind.Stun   => "Aturdido",
        CombatPopupKind.Synergy => "¡Sinergia!",
        CombatPopupKind.Static    => "Static",
        CombatPopupKind.Pulse     => "Pulse",
        CombatPopupKind.Steel     => "Steel",
        CombatPopupKind.Mist      => "Mist",
        CombatPopupKind.Lifesteal => "Robo de vida",
        CombatPopupKind.Shield => "Escudo",
        CombatPopupKind.Reaction => "¡Reacción!",
        _                      => "",
    };
}
}
