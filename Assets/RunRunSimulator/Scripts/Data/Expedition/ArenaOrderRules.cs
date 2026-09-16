namespace MoriMonchiSimulator
{

public static class ArenaOrderRules
{
    public static Occupation ToOccupation(ArenaOrders o)
    {
        if (o.Contact == ContactChoice.Fight)
            return o.Posture == PostureChoice.Protect ? Occupation.Guard : Occupation.Break;
        return o.Posture == PostureChoice.Protect ? Occupation.Gather : Occupation.Decoy;
    }

    public static ArenaSite ToSite(ArenaOrders o)
    {
        if (o.Loot == LootChoice.Big) return ArenaSite.Center;
        return o.Posture == PostureChoice.Protect ? ArenaSite.NearVein : ArenaSite.FarVein;
    }

    public static ArenaOrders FromOccupation(Occupation occupation, ArenaSite site)
    {
        var loot = site == ArenaSite.Center ? LootChoice.Big : LootChoice.Small;
        switch (occupation)
        {
            case Occupation.Guard: return new ArenaOrders(loot, ContactChoice.Fight, PostureChoice.Protect);
            case Occupation.Break: return new ArenaOrders(loot, ContactChoice.Fight, PostureChoice.Aggressive);
            case Occupation.Decoy: return new ArenaOrders(loot, ContactChoice.Flee, PostureChoice.Aggressive);
            default:               return new ArenaOrders(loot, ContactChoice.Flee, PostureChoice.Protect);
        }
    }

    public static int Choice(ArenaOrders o, OrderPillar pillar)
    {
        switch (pillar)
        {
            case OrderPillar.Loot:    return (int)o.Loot;
            case OrderPillar.Contact: return (int)o.Contact;
            default:                  return (int)o.Posture;
        }
    }

    public static ArenaOrders With(ArenaOrders o, OrderPillar pillar, int choice)
    {
        switch (pillar)
        {
            case OrderPillar.Loot:    o.Loot = (LootChoice)choice; break;
            case OrderPillar.Contact: o.Contact = (ContactChoice)choice; break;
            default:                  o.Posture = (PostureChoice)choice; break;
        }
        return o;
    }

    public static ArenaOrders Clamp(CreatureDNA dna, ExpeditionRulesSO rules, ArenaOrders o)
    {
        if (dna == null) return o;
        if (ArenaBases.TryBaseOf(dna.Role, o, out _)) return o;
        return ArenaBases.ToOrders(dna.Role, ArenaBases.Default(dna.Role));
    }
}
}
