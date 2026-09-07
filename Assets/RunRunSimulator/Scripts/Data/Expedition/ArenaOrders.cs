using System;
namespace MoriMonchiSimulator
{

[Serializable]
public struct ArenaOrders
{
    public LootChoice Loot;
    public ContactChoice Contact;
    public PostureChoice Posture;

    public ArenaOrders(LootChoice loot, ContactChoice contact, PostureChoice posture)
    {
        Loot = loot;
        Contact = contact;
        Posture = posture;
    }

    public static ArenaOrders Default => new ArenaOrders(LootChoice.Big, ContactChoice.Flee, PostureChoice.Protect);

    public bool Equals(ArenaOrders other) => Loot == other.Loot && Contact == other.Contact && Posture == other.Posture;
}
}
