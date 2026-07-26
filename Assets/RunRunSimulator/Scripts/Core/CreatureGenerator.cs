using UnityEngine;
namespace MoriMonchiSimulator
{

public static class CreatureGenerator
{
    public const int StatBudget = 18;
    public const int StatMin    = 1;
    public const int StatMax    = 10;

    public static CreatureDNA GenerateRandom(CreatureDatabaseSO database, FurTypeDatabaseSO furDb = null)
    {
        if (database == null)
        {
            Debug.LogError("[CreatureGenerator] Database reference is null.");
            return new CreatureDNA();
        }

        var bodyShape = Pick(database.BodyShapes);
        var arm       = Pick(database.Arms);
        var eye       = Pick(database.Eyes);
        var mouth     = Pick(database.Mouths);

        if (bodyShape == null || arm == null || eye == null || mouth == null)
            Debug.LogWarning("[CreatureGenerator] One or more part slots are empty — ensure all databases are populated.");

        var baseColor = ColorGenetics.RandomBase();
        var furValues = System.Enum.GetValues(typeof(FurType));

        return new CreatureDNA
        {
            BodyShapeID  = bodyShape?.ID ?? "",
            ArmID        = arm?.ID       ?? "",
            EyeID        = eye?.ID       ?? "",
            MouthID      = mouth?.ID     ?? "",
            BaseColor      = baseColor,
            SecondaryColor = ColorGenetics.DeriveSecondary(baseColor),
            FurType        = furDb != null ? furDb.RollMintFurType() : (FurType)furValues.GetValue(Random.Range(0, furValues.Length)),
            IsShiny        = ColorGenetics.RollShiny(),
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
