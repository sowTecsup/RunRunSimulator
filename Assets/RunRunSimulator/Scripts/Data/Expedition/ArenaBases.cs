namespace MoriMonchiSimulator
{

public static class ArenaBases
{
    public static readonly ArenaBase[] All = { ArenaBase.Territory, ArenaBase.Forage, ArenaBase.Opportunism };

    public static string Name(ArenaBase b)
    {
        switch (b)
        {
            case ArenaBase.Territory: return "Territorio";
            case ArenaBase.Forage: return "Rebusque";
            default: return "Oportunismo";
        }
    }

    public static string RoleName(Role r)
    {
        switch (r)
        {
            case Role.Protector: return "Protector";
            case Role.Agresivo: return "Agresivo";
            default: return "Empático";
        }
    }

    public static ArenaBase Closed(Role r)
    {
        switch (r)
        {
            case Role.Protector: return ArenaBase.Opportunism;
            case Role.Agresivo: return ArenaBase.Forage;
            default: return ArenaBase.Territory;
        }
    }

    public static bool Opens(Role r, ArenaBase b) => b != Closed(r);

    public static string ClosedReason(Role r)
    {
        switch (r)
        {
            case Role.Protector: return "un Protector no vive del oportunismo";
            case Role.Agresivo: return "un Agresivo no vive del rebusque";
            default: return "un Empático no vive del territorio";
        }
    }

    public static ArenaBase OpenAt(Role r, int index)
    {
        switch (r)
        {
            case Role.Protector: return index == 0 ? ArenaBase.Territory : ArenaBase.Forage;
            case Role.Agresivo: return index == 0 ? ArenaBase.Territory : ArenaBase.Opportunism;
            default: return index == 0 ? ArenaBase.Forage : ArenaBase.Opportunism;
        }
    }

    public static ArenaBase Default(Role r) => r == Role.Agresivo ? ArenaBase.Territory : ArenaBase.Forage;

    public static ArenaOrders ToOrders(Role r, ArenaBase b)
    {
        if (!Opens(r, b)) b = Default(r);

        switch (r)
        {
            case Role.Protector:
                return b == ArenaBase.Territory
                    ? new ArenaOrders(LootChoice.Big, ContactChoice.Fight, PostureChoice.Protect)
                    : new ArenaOrders(LootChoice.Small, ContactChoice.Flee, PostureChoice.Protect);
            case Role.Agresivo:
                return b == ArenaBase.Territory
                    ? new ArenaOrders(LootChoice.Big, ContactChoice.Fight, PostureChoice.Aggressive)
                    : new ArenaOrders(LootChoice.Small, ContactChoice.Flee, PostureChoice.Aggressive);
            default:
                return b == ArenaBase.Forage
                    ? new ArenaOrders(LootChoice.Big, ContactChoice.Flee, PostureChoice.Protect)
                    : new ArenaOrders(LootChoice.Small, ContactChoice.Fight, PostureChoice.Aggressive);
        }
    }

    public static bool TryBaseOf(Role r, ArenaOrders o, out ArenaBase b)
    {
        for (int i = 0; i < 2; i++)
        {
            var candidate = OpenAt(r, i);
            if (ToOrders(r, candidate).Equals(o))
            {
                b = candidate;
                return true;
            }
        }
        b = default;
        return false;
    }

    public static bool RoleFor(ArenaOrders o, out Role r)
    {
        var roles = new[] { Role.Protector, Role.Agresivo, Role.Empatico };
        for (int i = 0; i < roles.Length; i++)
        {
            if (TryBaseOf(roles[i], o, out _))
            {
                r = roles[i];
                return true;
            }
        }
        r = default;
        return false;
    }

    public static string VariantName(Role r, ArenaBase b)
    {
        if (!Opens(r, b)) return "";

        switch (r)
        {
            case Role.Protector: return b == ArenaBase.Territory ? "Dominante" : "Custodio";
            case Role.Agresivo: return b == ArenaBase.Territory ? "Invasor" : "Hiena";
            default: return b == ArenaBase.Forage ? "Compañera" : "Gaviota";
        }
    }

    public static string VariantDescription(Role r, ArenaBase b)
    {
        if (!Opens(r, b)) return "";

        switch (r)
        {
            case Role.Protector:
                return b == ArenaBase.Territory
                    ? "Se planta en el centro y no lo suelta: protege lo que ya tiene, pero si la rodean cae sola."
                    : "Cuida la veta más cercana y se repliega si aprietan: junta poco pero casi nunca pierde nada.";
            case Role.Agresivo:
                return b == ArenaBase.Territory
                    ? "Caza en el centro sin dar tregua: presiona fuerte pero se desgasta si nadie cae rápido."
                    : "Provoca en las vetas y se escapa con lo que sueltan los demás: vive de la pelea ajena.";
            default:
                return b == ArenaBase.Forage
                    ? "Junta del centro y corre al primer roce: rinde si nadie la alcanza, se queda vacía si la cazan."
                    : "Ronda las vetas aprovechando lo que dejan tirado: no busca pelea pero tampoco la evita si conviene.";
        }
    }

    public static string RivalRead(Role r)
    {
        string first = Name(OpenAt(r, 0));
        string second = Name(OpenAt(r, 1));
        string conj = second.Length > 0 && (second[0] == 'O' || second[0] == 'o') ? "u" : "o";
        return RoleName(r) + " · " + first + " " + conj + " " + second;
    }
}
}
