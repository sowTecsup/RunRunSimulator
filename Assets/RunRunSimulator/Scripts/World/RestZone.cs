// A rest area / bed — refills Energy. Add to a furniture prefab.
public sealed class RestZone : NeedStation
{
    public override NeedType Need => NeedType.Energy;
}
