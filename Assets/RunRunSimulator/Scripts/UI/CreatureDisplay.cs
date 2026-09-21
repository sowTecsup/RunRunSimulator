using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

public static class CreatureDisplay
{
    public static string StateOf(CreatureDNA d) =>
        d.IsSold                           ? Loc.Tr("status.sold")     :
        d.IsDead                           ? Loc.Tr("status.dead")     :
        d.BusyState == BusyReason.Breeding ? Loc.Tr("status.breeding") :
        Loc.Tr("status.free");

    public static Color RarityColor(Rarity r) => BodyPart.RarityColor(r);

    public static void ApplyRarityBorder(VisualElement el, Color c)
    {
        el.style.borderTopColor    = c;
        el.style.borderBottomColor = c;
        el.style.borderLeftColor   = c;
        el.style.borderRightColor  = c;
    }
}
}
