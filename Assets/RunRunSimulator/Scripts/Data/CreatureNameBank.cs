using UnityEngine;

// Two-word name pool for auto-naming MoriMonchis on Mint and Breed.
// Names are editable post-assignment via CreatureDNA.CustomName.
public static class CreatureNameBank
{
    private static readonly string[] adjectives =
    {
        "Slimy",   "Fuzzy",   "Neon",    "Soggy",   "Crispy",
        "Moldy",   "Zappy",   "Chunky",  "Gooey",   "Wobbly",
        "Grumpy",  "Sneaky",  "Bubbly",  "Stinky",  "Bouncy",
        "Grubby",  "Misty",   "Rusty",   "Glowy",   "Spooky",
        "Gloomy",  "Crusty",  "Wiggly",  "Lumpy",   "Spongy",
        "Greasy",  "Squishy", "Cranky",  "Drippy",  "Funky",
        "Murky",   "Snotty",  "Prickly", "Twitchy", "Grimy",
        "Squeaky", "Smelly",  "Dizzy",   "Jumpy",   "Scruffy",
        "Goopy",   "Clammy",  "Frosty",  "Itchy",   "Munchy",
        "Pudgy",   "Raspy",   "Snappy",  "Vile",    "Yucky",
    };

    private static readonly string[] nouns =
    {
        "Blob",   "Spore",   "Glob",    "Munch",   "Zap",
        "Burp",   "Slug",    "Grub",    "Snort",   "Blorp",
        "Chunk",  "Gunk",    "Pudge",   "Snag",    "Creep",
        "Fuzz",   "Volt",    "Murk",    "Snoot",   "Lump",
        "Plop",   "Squish",  "Droob",   "Smudge",  "Gloop",
        "Goober", "Nugget",  "Wart",    "Booger",  "Critter",
        "Sprout", "Wisp",    "Bogey",   "Niblet",  "Squirt",
        "Tater",  "Dollop",  "Morsel",  "Pip",     "Runt",
        "Scrap",  "Tadpole", "Wedge",   "Bumble",  "Nibble",
        "Gloob",  "Mochi",   "Gremlin", "Fluff",   "Snibble",
    };

    public static string GetRandomName() =>
        $"{adjectives[Random.Range(0, adjectives.Length)]} {nouns[Random.Range(0, nouns.Length)]}";
}
