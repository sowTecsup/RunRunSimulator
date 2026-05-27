using System.Collections.Generic;
using UnityEngine;

// Word pools keyed by (PartSet, PartRole). Used by BodyPart.RollName() and PartDatabaseSO.RollAllNames().
// Creature display name = Body + Arm + Eye + Mouth words in that order.
public static class PartNameBank
{
    private static readonly Dictionary<PartSet, Dictionary<PartRole, string[]>> _bank =
        new Dictionary<PartSet, Dictionary<PartRole, string[]>>
        {
            {
                PartSet.GooGang, new Dictionary<PartRole, string[]>
                {
                    { PartRole.Body,  new[] { "Gloop",   "Slurp",  "Ooze",    "Blurp",   "Slime"   } },
                    { PartRole.Arm,   new[] { "Snaggle", "Drib",   "Squelch", "Drip",    "Glob"    } },
                    { PartRole.Eye,   new[] { "Blink",   "Splat",  "Goo",     "Seep",    "Drizzle" } },
                    { PartRole.Mouth, new[] { "Drool",   "Blorp",  "Gloop",   "Slurp",   "Gush"    } },
                }
            },
            {
                PartSet.BogBrigade, new Dictionary<PartRole, string[]>
                {
                    { PartRole.Body,  new[] { "Murk",    "Muck",   "Boggle",  "Marsh",   "Sludge"  } },
                    { PartRole.Arm,   new[] { "Squelch", "Grasp",  "Clutch",  "Wade",    "Drag"    } },
                    { PartRole.Eye,   new[] { "Fog",     "Haze",   "Mist",    "Dim",     "Murk"    } },
                    { PartRole.Mouth, new[] { "Croak",   "Burp",   "Gurgle",  "Bloop",   "Munch"   } },
                }
            },
            {
                PartSet.FuzzFactory, new Dictionary<PartRole, string[]>
                {
                    { PartRole.Body,  new[] { "Fluff",   "Puff",   "Snuggle", "Plush",   "Fuzzy"   } },
                    { PartRole.Arm,   new[] { "Nuzzle",  "Cuddle", "Tuck",    "Pat",     "Wrap"    } },
                    { PartRole.Eye,   new[] { "Twinkle", "Blink",  "Glimmer", "Peep",    "Gleam"   } },
                    { PartRole.Mouth, new[] { "Nibble",  "Nuzzle", "Smooch",  "Munch",   "Gnaw"    } },
                }
            },
            {
                PartSet.CosmicCreeps, new Dictionary<PartRole, string[]>
                {
                    { PartRole.Body,  new[] { "Void",    "Flux",   "Nebula",  "Glitch",  "Phase"   } },
                    { PartRole.Arm,   new[] { "Warp",    "Drift",  "Reach",   "Pluck",   "Snatch"  } },
                    { PartRole.Eye,   new[] { "Gaze",    "Peer",   "Stare",   "Scan",    "Glare"   } },
                    { PartRole.Mouth, new[] { "Howl",    "Shriek", "Hiss",    "Buzz",    "Moan"    } },
                }
            },
            {
                PartSet.NeonNightmares, new Dictionary<PartRole, string[]>
                {
                    { PartRole.Body,  new[] { "Neon",    "Blaze",  "Surge",   "Glitch",  "Flash"   } },
                    { PartRole.Arm,   new[] { "Jab",     "Slash",  "Swipe",   "Spike",   "Punch"   } },
                    { PartRole.Eye,   new[] { "Glare",   "Flare",  "Blaze",   "Shine",   "Pulse"   } },
                    { PartRole.Mouth, new[] { "Shout",   "Snarl",  "Snap",    "Hiss",    "Growl"   } },
                }
            },
            {
                PartSet.CrunchCrew, new Dictionary<PartRole, string[]>
                {
                    { PartRole.Body,  new[] { "Shell",   "Crunch", "Chitin",  "Husk",    "Casp"    } },
                    { PartRole.Arm,   new[] { "Pinch",   "Grasp",  "Snap",    "Claw",    "Grip"    } },
                    { PartRole.Eye,   new[] { "Stalk",   "Scope",  "Scan",    "Peer",    "Watch"   } },
                    { PartRole.Mouth, new[] { "Gnash",   "Chomp",  "Snap",    "Crunch",  "Grind"   } },
                }
            },
            {
                PartSet.GrimGlobs, new Dictionary<PartRole, string[]>
                {
                    { PartRole.Body,  new[] { "Grim",    "Murk",   "Dread",   "Gloom",   "Soot"    } },
                    { PartRole.Arm,   new[] { "Creep",   "Lurch",  "Drag",    "Claw",    "Crawl"   } },
                    { PartRole.Eye,   new[] { "Hollow",  "Gleam",  "Gloom",   "Peer",    "Leer"    } },
                    { PartRole.Mouth, new[] { "Moan",    "Wail",   "Hiss",    "Croak",   "Rasp"    } },
                }
            },
            {
                PartSet.SpudSquad, new Dictionary<PartRole, string[]>
                {
                    { PartRole.Body,  new[] { "Spud",    "Chunk",  "Blob",    "Knob",    "Lump"    } },
                    { PartRole.Arm,   new[] { "Thump",   "Bump",   "Stub",    "Nudge",   "Shove"   } },
                    { PartRole.Eye,   new[] { "Squint",  "Blink",  "Peer",    "Gawk",    "Stare"   } },
                    { PartRole.Mouth, new[] { "Chomp",   "Munch",  "Gnaw",    "Chew",    "Crunch"  } },
                }
            },
            {
                PartSet.MoldMob, new Dictionary<PartRole, string[]>
                {
                    { PartRole.Body,  new[] { "Mold",    "Spore",  "Bloom",   "Fuzz",    "Myc"     } },
                    { PartRole.Arm,   new[] { "Spread",  "Creep",  "Grow",    "Branch",  "Sprout"  } },
                    { PartRole.Eye,   new[] { "Spore",   "Spot",   "Speck",   "Dot",     "Gleam"   } },
                    { PartRole.Mouth, new[] { "Puff",    "Spray",  "Spew",    "Vent",    "Gust"    } },
                }
            },
            {
                PartSet.ZapZone, new Dictionary<PartRole, string[]>
                {
                    { PartRole.Body,  new[] { "Zap",     "Volt",   "Spark",   "Surge",   "Buzz"    } },
                    { PartRole.Arm,   new[] { "Jolt",    "Shock",  "Singe",   "Crackle", "Zing"    } },
                    { PartRole.Eye,   new[] { "Flash",   "Flicker","Strobe",  "Pulse",   "Glow"    } },
                    { PartRole.Mouth, new[] { "Crackle", "Hiss",   "Pop",     "Sizzle",  "Zap"     } },
                }
            },
        };

    private static readonly Dictionary<PartRole, string[]> _fallback =
        new Dictionary<PartRole, string[]>
        {
            { PartRole.Body,  new[] { "Glob",  "Blorp", "Gunk",   "Crud",   "Sludge" } },
            { PartRole.Arm,   new[] { "Grab",  "Swipe", "Flail",  "Prod",   "Poke"   } },
            { PartRole.Eye,   new[] { "Peer",  "Stare", "Gawk",   "Ogle",   "Watch"  } },
            { PartRole.Mouth, new[] { "Grunt", "Mutter","Mumble", "Growl",  "Groan"  } },
        };

    public static string GetRandomName(PartSet set, PartRole role)
    {
        if (_bank.TryGetValue(set, out var roleMap) &&
            roleMap.TryGetValue(role, out var pool) &&
            pool.Length > 0)
            return pool[Random.Range(0, pool.Length)];

        if (_fallback.TryGetValue(role, out var fallbackPool) && fallbackPool.Length > 0)
            return fallbackPool[Random.Range(0, fallbackPool.Length)];

        return "Bloop";
    }
}
