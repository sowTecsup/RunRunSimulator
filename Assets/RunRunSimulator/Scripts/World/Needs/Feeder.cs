// A feeding station — refills Health (hunger/wellbeing). Add to a furniture prefab.
namespace MoriMonchiSimulator
{
public sealed class Feeder : NeedStation
{
    public override NeedType Need => NeedType.Health;
}
}
