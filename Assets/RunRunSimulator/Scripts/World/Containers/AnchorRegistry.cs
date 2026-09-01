using System.Collections.Generic;
namespace MoriMonchiSimulator
{

public static class AnchorRegistry
{
    private static readonly Dictionary<string, MoriMochiContainer> places = new Dictionary<string, MoriMochiContainer>();

    public static void Register(MoriMochiContainer place)
    {
        if (place == null || string.IsNullOrEmpty(place.AnchorKey)) return;
        places[place.AnchorKey] = place;
    }

    public static void Unregister(MoriMochiContainer place)
    {
        if (place == null || string.IsNullOrEmpty(place.AnchorKey)) return;
        if (places.TryGetValue(place.AnchorKey, out var stored) && ReferenceEquals(stored, place))
            places.Remove(place.AnchorKey);
    }

    public static bool TryGet(string key, out MoriMochiContainer place)
    {
        if (string.IsNullOrEmpty(key)) { place = null; return false; }
        return places.TryGetValue(key, out place);
    }
}
}
