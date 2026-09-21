using System;
namespace MoriMonchiSimulator
{

public static class CreatureLifecycle
{
    public static void Kill(CreatureDNA dna)
    {
        var registry = GameManager.Instance != null ? GameManager.Instance.Registry : null;
        if (dna == null || registry == null) return;

        dna.IsDead = true;
        registry.Depart(dna.UniqueID);

        GameEvents.CreatureDeparted(dna);
        GameEvents.RegistryChanged(registry);
    }

    public static void Adopt(CreatureDNA dna)
    {
        var registry = GameManager.Instance != null ? GameManager.Instance.Registry : null;
        if (dna == null || registry == null) return;

        dna.BusyState = BusyReason.Sold;
        dna.SaleDate = DateTime.UtcNow;
        registry.Depart(dna.UniqueID);

        GameEvents.CreatureDeparted(dna);
        GameEvents.RegistryChanged(registry);
    }
}
}
