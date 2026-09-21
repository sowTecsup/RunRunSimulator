using System.Collections.Generic;
namespace MoriMonchiSimulator
{

public class RegistryData
{
    public Dictionary<string, CreatureDNA> Alive = new Dictionary<string, CreatureDNA>();
    public Dictionary<string, CreatureDNA> Departed = new Dictionary<string, CreatureDNA>();
}
}
