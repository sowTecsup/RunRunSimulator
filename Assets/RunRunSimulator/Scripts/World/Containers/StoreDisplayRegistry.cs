using System.Collections.Generic;
namespace MoriMonchiSimulator
{

public static class StoreDisplayRegistry
{
    private static readonly List<StoreContainer> displays = new List<StoreContainer>();

    public static IReadOnlyList<StoreContainer> All => displays;

    public static void Register(StoreContainer d)   { if (d != null && !displays.Contains(d)) displays.Add(d); }
    public static void Unregister(StoreContainer d) => displays.Remove(d);
}
}
