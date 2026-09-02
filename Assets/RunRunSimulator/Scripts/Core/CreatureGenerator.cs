using UnityEngine;
namespace MoriMonchiSimulator
{

public static class CreatureGenerator
{
    public const int StatBudget = 18;
    public const int StatMin    = 1;
    public const int StatMax    = 10;

    public const int PotentialMin     = 1;
    public const int PotentialMax     = 10;
    public const int MintPotentialMax = 3;

    public static CreatureDNA GenerateRandom(CreatureDatabaseSO database, FurTypeDatabaseSO furDb = null)
    {
        if (database == null)
        {
            Debug.LogError("[CreatureGenerator] Database reference is null.");
            return new CreatureDNA();
        }

        var bodyShape = Pick(database.BodyShapes);
        var horn      = Pick(database.Horns);
        var back      = Pick(database.Backs);
        var wing      = Pick(database.Wings);
        var face      = Pick(database.Faces);

        if (bodyShape == null || horn == null || back == null || wing == null || face == null)
            Debug.LogWarning("[CreatureGenerator] One or more part slots are empty — ensure all databases are populated.");

        var baseColor = ColorGenetics.RandomBase();
        var furValues = System.Enum.GetValues(typeof(FurType));

        return new CreatureDNA
        {
            BodyShapeID  = bodyShape?.ID ?? "",
            HornID       = horn?.ID       ?? "",
            BackID       = back?.ID       ?? "",
            WingID       = wing?.ID       ?? "",
            FaceID       = face?.ID       ?? "",
            BaseColor      = baseColor,
            SecondaryColor = ColorGenetics.DeriveSecondary(baseColor),
            FurType        = furDb != null ? furDb.RollMintFurType() : (FurType)furValues.GetValue(Random.Range(0, furValues.Length)),
            IsShiny        = ColorGenetics.RollShiny(),
            HornPotential  = RandomMintPotential(),
            BackPotential  = RandomMintPotential(),
            WingPotential  = RandomMintPotential(),
        };
    }

    public static Role RandomRole()
    {
        var values = System.Enum.GetValues(typeof(Role));
        return (Role)values.GetValue(Random.Range(0, values.Length));
    }

    public static Element RandomElement()
    {
        var values = System.Enum.GetValues(typeof(Element));
        return (Element)values.GetValue(Random.Range(0, values.Length));
    }

    public static float RandomDial() => Random.Range(0.15f, 0.85f);

    public static int RandomMintPotential() => Random.Range(PotentialMin, MintPotentialMax + 1);

    public static (float hp, float atk, float spd) RandomBaseStats()
    {
        int[] stats     = { StatMin, StatMin, StatMin };
        int   remaining = StatBudget - StatMin * stats.Length;
        while (remaining > 0)
        {
            int i = Random.Range(0, stats.Length);
            if (stats[i] >= StatMax) continue;
            stats[i]++;
            remaining--;
        }
        return (stats[0], stats[1], stats[2]);
    }

    private static T Pick<T>(PartDatabaseSO<T> db) where T : BodyPart
    {
        return db?.GetRandomPart();
    }
}
}
