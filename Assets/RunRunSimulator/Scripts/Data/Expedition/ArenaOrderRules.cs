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

    public static bool IsLocked(CreatureDNA dna, ExpeditionRulesSO rules, OrderPillar pillar, out int forced)
    {
        forced = -1;
        if (dna == null || rules == null) return false;

        switch (pillar)
        {
            case OrderPillar.Contact:
                if (dna.Boldness >= rules.BoldFightLock) forced = (int)ContactChoice.Fight;
                else if (dna.Boldness <= rules.ShyFleeLock) forced = (int)ContactChoice.Flee;
                break;
            case OrderPillar.Posture:
                if (dna.Sociability >= rules.SocialProtectLock) forced = (int)PostureChoice.Protect;
                else if (dna.Sociability <= rules.LonerAggressiveLock) forced = (int)PostureChoice.Aggressive;
                break;
        }

        return forced >= 0;
    }

    public static ArenaOrders Clamp(CreatureDNA dna, ExpeditionRulesSO rules, ArenaOrders o)
    {
        if (IsLocked(dna, rules, OrderPillar.Contact, out int contact)) o.Contact = (ContactChoice)contact;
        if (IsLocked(dna, rules, OrderPillar.Posture, out int posture)) o.Posture = (PostureChoice)posture;
        return o;
    }
}
}
