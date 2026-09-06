using System;
namespace MoriMonchiSimulator
{

[Serializable]
public struct ArenaCastEntry
{
    public CreatureDNA Dna;
    public ExpeditionTeam Team;
    public Occupation Occupation;
    public ArenaSite Site;
}
}
