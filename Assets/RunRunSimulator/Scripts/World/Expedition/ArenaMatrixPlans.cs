namespace MoriMonchiSimulator
{

public struct ArenaMatrixTeam
{
    public string Name;
    public ArenaOrders[] Orders;
    public float[] Boldness;
    public float[] Sociability;
}

public static class ArenaMatrixPlans
{
    private static readonly ArenaOrders GuaC = new ArenaOrders(LootChoice.Big, ContactChoice.Fight, PostureChoice.Protect);
    private static readonly ArenaOrders CazC = new ArenaOrders(LootChoice.Big, ContactChoice.Fight, PostureChoice.Aggressive);
    private static readonly ArenaOrders CazV = new ArenaOrders(LootChoice.Small, ContactChoice.Fight, PostureChoice.Aggressive);
    private static readonly ArenaOrders RecC = new ArenaOrders(LootChoice.Big, ContactChoice.Flee, PostureChoice.Protect);
    private static readonly ArenaOrders RecV = new ArenaOrders(LootChoice.Small, ContactChoice.Flee, PostureChoice.Protect);
    private static readonly ArenaOrders SenV = new ArenaOrders(LootChoice.Small, ContactChoice.Flee, PostureChoice.Aggressive);

    public static ArenaMatrixTeam Team(string name, ArenaOrders a, ArenaOrders b, ArenaOrders c)
    {
        return new ArenaMatrixTeam
        {
            Name = name,
            Orders = new[] { a, b, c },
            Boldness = new[] { 0.5f, 0.5f, 0.5f },
            Sociability = new[] { 0.5f, 0.5f, 0.5f }
        };
    }

    public static ArenaMatrixTeam Team(string name, ArenaOrders[] orders, float[] boldness, float[] sociability)
    {
        return new ArenaMatrixTeam
        {
            Name = name,
            Orders = orders,
            Boldness = boldness,
            Sociability = sociability
        };
    }

    public static readonly ArenaMatrixTeam[] Plans16 =
    {
        Team("Muralla", GuaC, RecC, RecC),
        Team("Hormiguero", RecV, RecV, RecV),
        Team("Jauria", CazC, CazV, RecV),
        Team("JauriaCentro", CazC, CazC, RecC),
        Team("Emboscada", CazV, SenV, RecC),
        Team("Mixta", GuaC, CazC, RecV),
        Team("Codicia", RecC, RecC, RecC),
        Team("Escolta", CazC, CazV, RecC),
        Team("Fortin", GuaC, CazV, RecC),
        Team("Rebano", RecC, RecV, SenV)
    };

    public static readonly ArenaMatrixTeam[] Subset10 =
    {
        Find(Plans16, "Muralla"),
        Find(Plans16, "Fortin"),
        Find(Plans16, "Jauria"),
        Find(Plans16, "Hormiguero"),
        Find(Plans16, "Emboscada"),
        Find(Plans16, "Codicia"),
        Find(Plans16, "Mixta")
    };

    public static readonly ArenaMatrixTeam[] Personalities6 =
    {
        Team("RosterB", new[] { CazV, RecC, CazC }, new[] { 0.90f, 0.15f, 0.50f }, new[] { 0.25f, 0.85f, 0.50f }),
        Team("TresTimidos", new[] { RecC, RecV, RecV }, new[] { 0.15f, 0.15f, 0.15f }, new[] { 0.85f, 0.85f, 0.85f }),
        Team("TresOsados", new[] { CazC, CazV, CazV }, new[] { 0.90f, 0.90f, 0.90f }, new[] { 0.25f, 0.25f, 0.25f })
    };

    public static ArenaMatrixTeam Find(ArenaMatrixTeam[] set, string name)
    {
        for (int i = 0; i < set.Length; i++)
            if (set[i].Name == name) return set[i];
        return default;
    }
}
}
