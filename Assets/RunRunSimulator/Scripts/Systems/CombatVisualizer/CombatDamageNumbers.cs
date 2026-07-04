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
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.6f, 0f);
    [SerializeField, Min(1f)] private float critScale = 1.35f;

    private void OnEnable()  => CombatVisualEvents.OnPopup += HandlePopup;
    private void OnDisable() => CombatVisualEvents.OnPopup -= HandlePopup;

    private void HandlePopup(CombatVisualPopup p)
    {
        var prefab = ResolvePrefab(p.Kind);
        if (prefab == null) return;

        var dn = prefab.Spawn(p.Position + spawnOffset, p.Amount);
        if (dn == null) return;

        dn.enableNumber  = p.Kind != CombatPopupKind.Stun && p.Amount >= 0.5f;
        dn.enableTopText = true;
        dn.topText       = Label(p.Kind);

        if (palette != null) dn.SetColor(palette.GetColor(p.Kind));
        if (p.Follow != null) dn.SetFollowedTarget(p.Follow);

        if (p.Kind == CombatPopupKind.Heal || p.Kind == CombatPopupKind.Regen)
        {
            dn.enableLeftText = true;
            dn.leftText       = "+";
            dn.UpdateText();
        }

        if (p.Kind == CombatPopupKind.Crit) dn.SetScale(critScale);
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
        _                      => "",
    };
}
}
