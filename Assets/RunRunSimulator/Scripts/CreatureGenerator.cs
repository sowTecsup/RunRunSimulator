using UnityEngine;

public static class CreatureGenerator
{
    public static CreatureDNA GenerateRandom(CreatureDatabaseSO database, Rarity? rarityFilter = null)
    {
        if (database == null)
        {
            Debug.LogError("[CreatureGenerator] Database reference is null.");
            return new CreatureDNA();
        }

        var bodyShape = database.BodyShapes?.GetRandomPart(rarityFilter);
        var arm       = database.Arms?.GetRandomPart(rarityFilter);
        var eye       = database.Eyes?.GetRandomPart(rarityFilter);
        var mouth     = database.Mouths?.GetRandomPart(rarityFilter);

        if (bodyShape == null || arm == null || eye == null || mouth == null)
            Debug.LogWarning("[CreatureGenerator] One or more databases returned null — ensure all databases are populated.");

        return new CreatureDNA
        {
            BodyShapeID = bodyShape?.ID ?? "",
            ArmID       = arm?.ID       ?? "",
            EyeID       = eye?.ID       ?? "",
            MouthID     = mouth?.ID     ?? "",
            PrimaryColor = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.6f, 1f),
        };
    }
}
