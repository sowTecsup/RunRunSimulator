using DamageNumbersPro;
using UnityEngine;
namespace MoriMonchiSimulator
{
public class CombatDamageNumbers : MonoBehaviour
{
    [SerializeField] private CombatPopupPaletteSO palette;
    [SerializeField] private DamageNumber numberPrefab;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 1.5f, 0f);

    private void OnEnable()  => CombatVisualEvents.OnPopup += HandlePopup;
    private void OnDisable() => CombatVisualEvents.OnPopup -= HandlePopup;

    private void HandlePopup(CombatVisualPopup p)
    {
        if (numberPrefab == null) return;
        var dn = numberPrefab.Spawn(p.Position + spawnOffset, p.Amount);
        if (dn == null) return;

        dn.enableNumber  = p.Kind != CombatPopupKind.Stun && p.Amount >= 0.5f;
        dn.enableTopText = true;
        dn.topText       = Label(p.Kind);

        if (palette != null) dn.SetColor(palette.GetColor(p.Kind));
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
        _                      => "",
    };
}
}
