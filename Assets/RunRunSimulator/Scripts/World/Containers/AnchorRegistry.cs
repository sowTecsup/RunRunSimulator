using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

// A place a creature can be anchored to: a breeding pen, a store shelf, a normal corral. Generalizes
// the "this MoriMochi lives HERE" relation so that on load each anchored creature is dropped DIRECTLY
// into its place instead of being cannon-fired across the floor and drifting in. The DNA persists the
// place's key (LocationKey) + an optional fixed slot; the spawner resolves the live place via the
// registry below and hands the body over with TryReclaim.
public interface IAnchorPlace
{
    string  AnchorKey { get; }                          // furniture cell key, "" if not placed
    Vector3 AnchorPosition(int slot);                   // where the spawner drops the body before reclaim
    bool    TryReclaim(MoriMochiAgent agent, int slot); // seat/confine the agent here; false if it can't (not ready / full)
}

// Lightweight runtime index of the IAnchorPlaces currently in the scene, keyed by AnchorKey. Places
// self-register in Start and unregister in OnDestroy, so it self-cleans across scene loads (no manual
// census, no scene wiring). The spawner queries it on load to place an anchored creature directly.
//
// Static — mirrors NeedStationRegistry / the GameEvents bus pattern — instead of a MonoBehaviour
// singleton, so there's no lifetime to manage. Place ↔ spawner ↔ agent are the SAME World domain, so
// this direct query is consistent with the pen/station precedent, not a cross-system singleton lookup.
public static class AnchorRegistry
{
    private static readonly Dictionary<string, IAnchorPlace> places = new Dictionary<string, IAnchorPlace>();

    // A place with no key yet (marker not stamped) is ignored — it isn't a resolvable anchor.
    public static void Register(IAnchorPlace place)
    {
        if (place == null || string.IsNullOrEmpty(place.AnchorKey)) return;
        places[place.AnchorKey] = place;
    }

    // Remove only if the stored value is THIS same place, so a re-register of the same key under a
    // fresh instance isn't clobbered by the old instance's OnDestroy firing afterwards.
    public static void Unregister(IAnchorPlace place)
    {
        if (place == null || string.IsNullOrEmpty(place.AnchorKey)) return;
        if (places.TryGetValue(place.AnchorKey, out var stored) && ReferenceEquals(stored, place))
            places.Remove(place.AnchorKey);
    }

    public static bool TryGet(string key, out IAnchorPlace place)
    {
        if (string.IsNullOrEmpty(key)) { place = null; return false; }
        return places.TryGetValue(key, out place);
    }
}
}
