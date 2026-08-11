using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

// Word pools keyed by (PartSet, PartRole). Used by BodyPart.RollName() and PartDatabaseSO.RollAllNames().
// Creature display name = Body + Horn + Back + Wing + Face words in that order.
public static class PartNameBank
{
    private static readonly Dictionary<PartSet, Dictionary<PartRole, string[]>> bank =
        new Dictionary<PartSet, Dictionary<PartRole, string[]>>
        {
            {
                PartSet.GooGang, new Dictionary<PartRole, string[]>
                {
                    { PartRole.Body,  new[] { "Gloop",   "Slurp",  "Ooze",    "Blurp",   "Slime"   } },
                    { PartRole.Horn,  new[] { "Spike",   "Ooze",   "Nub",     "Drip",    "Blorp"   } },
                    { PartRole.Back,  new[] { "Ridge",   "Hump",   "Blob",    "Ripple",  "Mound"   } },
                    { PartRole.Wing,  new[] { "Splash",  "Flap",   "Waft",    "Drizzle", "Splat"   } },
                    { PartRole.Face,  new[] { "Blink",   "Goo",    "Seep",    "Gloop",   "Smear"   } },
                }
            },
            {
                PartSet.BogBrigade, new Dictionary<PartRole, string[]>
                {
                    { PartRole.Body,  new[] { "Murk",    "Muck",   "Boggle",  "Marsh",   "Sludge"  } },
                    { PartRole.Horn,  new[] { "Reed",    "Root",   "Snag",    "Twig",    "Bramble" } },
                    { PartRole.Back,  new[] { "Moss",    "Shell",  "Marsh",   "Bog",     "Silt"    } },
                    { PartRole.Wing,  new[] { "Mist",    "Fog",    "Flit",    "Drift",   "Haze"    } },
                    { PartRole.Face,  new[] { "Croak",   "Murk",   "Gaze",    "Bleary",  "Dim"     } },
                }
            },
            {
                PartSet.FuzzFactory, new Dictionary<PartRole, string[]>
                {
                    { PartRole.Body,  new[] { "Fluff",   "Puff",   "Snuggle", "Plush",   "Fuzzy"   } },
                    { PartRole.Horn,  new[] { "Curl",    "Nub",    "Poof",    "Tuft",    "Wisp"    } },
                    { PartRole.Back,  new[] { "Fluff",   "Puff",   "Plush",   "Snuggle", "Fuzz"    } },
                    { PartRole.Wing,  new[] { "Flutter", "Whisk",  "Flit",    "Puff",    "Waft"    } },
                    { PartRole.Face,  new[] { "Twinkle", "Blink",  "Glimmer", "Peep",    "Gleam"   } },
                }
            },
            {
                PartSet.CosmicCreeps, new Dictionary<PartRole, string[]>
                {
                    { PartRole.Body,  new[] { "Void",    "Flux",   "Nebula",  "Glitch",  "Phase"   } },
                    { PartRole.Horn,  new[] { "Spire",   "Prong",  "Shard",   "Warp",    "Beam"    } },
                    { PartRole.Back,  new[] { "Nebula",  "Void",   "Rift",    "Flux",    "Phase"   } },
                    { PartRole.Wing,  new[] { "Drift",   "Warp",   "Glide",   "Flicker", "Pulse"   } },
                    { PartRole.Face,  new[] { "Gaze",    "Peer",   "Stare",   "Scan",    "Glare"   } },
                }
            },
            {
                PartSet.NeonNightmares, new Dictionary<PartRole, string[]>
                {
                    { PartRole.Body,  new[] { "Neon",    "Blaze",  "Surge",   "Glitch",  "Flash"   } },
                    { PartRole.Horn,  new[] { "Spike",   "Flash",  "Blaze",   "Jolt",    "Neon"    } },
                    { PartRole.Back,  new[] { "Grid",    "Glitch", "Stripe",  "Surge",   "Neon"    } },
                    { PartRole.Wing,  new[] { "Streak",  "Flare",  "Zap",     "Dash",    "Glow"    } },
                    { PartRole.Face,  new[] { "Glare",   "Flare",  "Blaze",   "Shine",   "Pulse"   } },
                }
            },
            {
                PartSet.CrunchCrew, new Dictionary<PartRole, string[]>
                {
                    { PartRole.Body,  new[] { "Shell",   "Crunch", "Chitin",  "Husk",    "Casp"    } },
                    { PartRole.Horn,  new[] { "Spike",   "Barb",   "Tusk",    "Prong",   "Point"   } },
                    { PartRole.Back,  new[] { "Shell",   "Carapace","Husk",   "Casp",    "Plate"   } },
                    { PartRole.Wing,  new[] { "Buzz",    "Whir",   "Flit",    "Skitter", "Dart"    } },
                    { PartRole.Face,  new[] { "Stalk",   "Scope",  "Scan",    "Peer",    "Watch"   } },
                }
            },
            {
                PartSet.GrimGlobs, new Dictionary<PartRole, string[]>
                {
                    { PartRole.Body,  new[] { "Grim",    "Murk",   "Dread",   "Gloom",   "Soot"    } },
                    { PartRole.Horn,  new[] { "Fang",    "Spike",  "Thorn",   "Barb",    "Claw"    } },
                    { PartRole.Back,  new[] { "Gloom",   "Murk",   "Dread",   "Soot",    "Shade"   } },
                    { PartRole.Wing,  new[] { "Wraith",  "Shroud", "Flit",    "Loom",    "Drift"   } },
                    { PartRole.Face,  new[] { "Hollow",  "Gleam",  "Gloom",   "Peer",    "Leer"    } },
                }
            },
            {
                PartSet.SpudSquad, new Dictionary<PartRole, string[]>
                {
                    { PartRole.Body,  new[] { "Spud",    "Chunk",  "Blob",    "Knob",    "Lump"    } },
                    { PartRole.Horn,  new[] { "Nub",     "Stub",   "Knob",    "Bump",    "Peg"     } },
                    { PartRole.Back,  new[] { "Chunk",   "Lump",   "Mound",   "Bulk",    "Knob"    } },
                    { PartRole.Wing,  new[] { "Flap",    "Waddle", "Thud",    "Bounce",  "Roll"    } },
                    { PartRole.Face,  new[] { "Squint",  "Blink",  "Peer",    "Gawk",    "Stare"   } },
                }
            },
            {
                PartSet.MoldMob, new Dictionary<PartRole, string[]>
                {
                    { PartRole.Body,  new[] { "Mold",    "Spore",  "Bloom",   "Fuzz",    "Myc"     } },
                    { PartRole.Horn,  new[] { "Spore",   "Stem",   "Stalk",   "Bloom",   "Sprout"  } },
                    { PartRole.Back,  new[] { "Mold",    "Fuzz",   "Myc",     "Crust",   "Bloom"   } },
                    { PartRole.Wing,  new[] { "Puff",    "Spray",  "Spew",    "Vent",    "Gust"    } },
                    { PartRole.Face,  new[] { "Spore",   "Spot",   "Speck",   "Dot",     "Gleam"   } },
                }
            },
            {
                PartSet.ZapZone, new Dictionary<PartRole, string[]>
                {
                    { PartRole.Body,  new[] { "Zap",     "Volt",   "Spark",   "Surge",   "Buzz"    } },
                    { PartRole.Horn,  new[] { "Spark",   "Volt",   "Prong",   "Jolt",    "Zing"    } },
                    { PartRole.Back,  new[] { "Surge",   "Buzz",   "Coil",    "Charge",  "Static"  } },
                    { PartRole.Wing,  new[] { "Flicker", "Strobe", "Zap",     "Pulse",   "Flash"   } },
                    { PartRole.Face,  new[] { "Glow",    "Gleam",  "Shine",   "Blaze",   "Glint"   } },
                }
            },
        };

    private static readonly Dictionary<PartRole, string[]> fallback =
        new Dictionary<PartRole, string[]>
        {
            { PartRole.Body,  new[] { "Glob",  "Blorp", "Gunk",   "Crud",   "Sludge"  } },
            { PartRole.Horn,  new[] { "Spike", "Nub",   "Barb",   "Point",  "Prong"   } },
            { PartRole.Back,  new[] { "Ridge", "Hump",  "Shell",  "Bulk",   "Mound"   } },
            { PartRole.Wing,  new[] { "Flap",  "Flit",  "Waft",   "Drift",  "Flutter" } },
            { PartRole.Face,  new[] { "Peer",  "Stare", "Gawk",   "Ogle",   "Watch"   } },
        };

    public static string GetRandomName(PartSet set, PartRole role)
    {
        if (bank.TryGetValue(set, out var roleMap) &&
            roleMap.TryGetValue(role, out var pool) &&
            pool.Length > 0)
            return pool[Random.Range(0, pool.Length)];

        if (fallback.TryGetValue(role, out var fallbackPool) && fallbackPool.Length > 0)
            return fallbackPool[Random.Range(0, fallbackPool.Length)];

        return "Bloop";
    }
}
}
