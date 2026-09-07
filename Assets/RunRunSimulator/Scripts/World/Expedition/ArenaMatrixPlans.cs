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
    private static readonly ArenaOrders GuaV = new ArenaOrders(LootChoice.Small, ContactChoice.Fight, PostureChoice.Protect);
    private static readonly ArenaOrders CazC = new ArenaOrders(LootChoice.Big, ContactChoice.Fight, PostureChoice.Aggressive);
    private static readonly ArenaOrders CazV = new ArenaOrders(LootChoice.Small, ContactChoice.Fight, PostureChoice.Aggressive);
    private static readonly ArenaOrders RecC = new ArenaOrders(LootChoice.Big, ContactChoice.Flee, PostureChoice.Protect);
    private static readonly ArenaOrders RecV = new ArenaOrders(LootChoice.Small, ContactChoice.Flee, PostureChoice.Protect);
    private static readonly ArenaOrders SenC = new ArenaOrders(LootChoice.Big, ContactChoice.Flee, PostureChoice.Aggressive);
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
        Team("MurallaVetas", GuaV, RecV, RecV),
        Team("Hormiguero", RecV, RecV, RecV),
        Team("HormigueroSenuelo", RecV, RecV, SenC),
        Team("Jauria", CazC, CazV, RecV),
        Team("JauriaCentro", CazC, CazC, RecC),
        Team("Emboscada", CazV, SenV, RecC),
        Team("Mixta", GuaC, CazC, RecV),
        Team("Codicia", RecC, RecC, RecC),
        Team("Escolta", CazC, CazV, RecC),
        Team("Senuelos", SenC, SenV, RecV),
        Team("DobleGuardia", GuaC, GuaV, RecC),
        Team("ContraJauria", GuaV, CazV, RecV),
        Team("Fortin", GuaC, CazV, RecC),
        Team("Engano", SenC, RecC, RecC),
        Team("Rebano", RecC, RecV, SenV)
    };

    public static readonly ArenaMatrixTeam[] Subset10 =
    {
        Find(Plans16, "Muralla"),
        Find(Plans16, "Fortin"),
        Find(Plans16, "Jauria"),
        Find(Plans16, "Hormiguero"),
        Find(Plans16, "HormigueroSenuelo"),
        Find(Plans16, "Senuelos"),
        Find(Plans16, "Engano"),
        Find(Plans16, "Emboscada"),
        Find(Plans16, "Codicia"),
        Find(Plans16, "Mixta")
    };

    public static readonly ArenaMatrixTeam[] Personalities6 =
    {
        Team("RosterA", new[] { CazC, RecV, GuaV }, new[] { 0.90f, 0.15f, 0.50f }, new[] { 0.25f, 0.85f, 0.50f }),
        Team("RosterB", new[] { CazV, RecC, CazC }, new[] { 0.90f, 0.15f, 0.50f }, new[] { 0.25f, 0.85f, 0.50f }),
        Team("TresTimidos", new[] { RecC, RecV, RecV }, new[] { 0.15f, 0.15f, 0.15f }, new[] { 0.85f, 0.85f, 0.85f }),
        Team("TresOsados", new[] { CazC, CazV, CazV }, new[] { 0.90f, 0.90f, 0.90f }, new[] { 0.25f, 0.25f, 0.25f }),
        Team("OsadosSociables", new[] { GuaC, GuaV, RecC }, new[] { 0.90f, 0.90f, 0.15f }, new[] { 0.85f, 0.85f, 0.85f }),
        Team("TimidosSolitarios", new[] { SenC, SenV, RecC }, new[] { 0.15f, 0.15f, 0.50f }, new[] { 0.15f, 0.15f, 0.50f })
    };

    public static ArenaMatrixTeam Find(ArenaMatrixTeam[] set, string name)
    {
        for (int i = 0; i < set.Length; i++)
            if (set[i].Name == name) return set[i];
        return default;
    }
}
}
