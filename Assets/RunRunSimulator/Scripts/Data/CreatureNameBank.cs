using UnityEngine;

// Two-word name pool for auto-naming MoriMonchis on Mint and Breed.
// Names are editable post-assignment via CreatureDNA.CustomName.
public static class CreatureNameBank
{
    private static readonly string[] adjectives =
    {
        "Slimy",  "Fuzzy",   "Neon",    "Soggy",  "Crispy",
        "Moldy",  "Zappy",   "Chunky",  "Gooey",  "Wobbly",
        "Grumpy", "Sneaky",  "Bubbly",  "Stinky", "Bouncy",
        "Grubby", "Misty",   "Rusty",   "Glowy",  "Spooky",
        "Gloomy", "Crusty",  "Wiggly",  "Lumpy",  "Spongy",
    };

    private static readonly string[] nouns =
    {
        "Blob",  "Spore",  "Glob",  "Munch", "Zap",
        "Burp",  "Slug",   "Grub",  "Snort", "Blorp",
        "Chunk", "Gunk",   "Pudge", "Snag",  "Creep",
        "Fuzz",  "Volt",   "Murk",  "Snoot", "Lump",
        "Plop",  "Squish", "Droob", "Smudge","Gloop",
    };

    public static string GetRandomName() =>
        $"{adjectives[Random.Range(0, adjectives.Length)]} {nouns[Random.Range(0, nouns.Length)]}";
}
