using System;
namespace MoriMonchiSimulator
{

[Serializable]
public struct ArenaCastEntry
{
    public CreatureDNA Dna;
    public ExpeditionTeam Team;
    public ArenaOrders Orders;

    public Occupation Occupation => ArenaOrderRules.ToOccupation(Orders);
    public ArenaSite Site => ArenaOrderRules.ToSite(Orders);
}
}
