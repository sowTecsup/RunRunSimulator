using UnityEngine;

public static class CreatureGenerator
{
    public static CreatureDNA GenerateRandom(CreatureDatabaseSO database, RarityOddsTableSO oddsTable = null)
    {
        if (database == null)
        {
            Debug.LogError("[CreatureGenerator] Database reference is null.");
            return new CreatureDNA();
        }

        var bodyShape = Pick(database.BodyShapes, oddsTable);
        var arm       = Pick(database.Arms,       oddsTable);
        var eye       = Pick(database.Eyes,        oddsTable);
        var mouth     = Pick(database.Mouths,      oddsTable);

        if (bodyShape == null || arm == null || eye == null || mouth == null)
            Debug.LogWarning("[CreatureGenerator] One or more part slots are empty — ensure all databases are populated.");

        return new CreatureDNA
        {
            BodyShapeID  = bodyShape?.ID ?? "",
            ArmID        = arm?.ID       ?? "",
            EyeID        = eye?.ID       ?? "",
            MouthID      = mouth?.ID     ?? "",
            PrimaryColor = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.6f, 1f),
        };
    }

    // Each slot rolls its own rarity independently.
    // Falls back to any random part if no parts match the rolled rarity.
    private static T Pick<T>(PartDatabaseSO<T> db, RarityOddsTableSO odds) where T : BodyPart
    {
        if (odds != null)
        {
            var filtered = db?.GetRandomPart(odds.Roll());
            if (filtered != null) return filtered;
        }
        return db?.GetRandomPart();
    }
}
