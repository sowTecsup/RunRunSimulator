// A rest area / bed — refills Energy. Add to a furniture prefab.
namespace MoriMonchiSimulator
{
public sealed class RestZone : NeedStation
{
    public override NeedType Need => NeedType.Energy;
}
}
