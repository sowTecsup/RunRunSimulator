using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace MoriMonchiSimulator
{

public enum SaveKind
{
    Registry,
    Furniture,
    Inventory,
    Social,
    World
}

public static class SaveMigrations
{
    public const int CurrentVersion = 4;

    private static readonly string[] LegacyCreatureStatFields =
    {
        "BaseConstitution", "BaseAttack", "BaseSpeed", "BaseDefense", "BaseLuck", "BaseEvasion", "Equipped"
    };

    public static SaveEnvelope Read(string json, SaveKind kind) => Read(json, kind, DateTime.UtcNow.Ticks);

    public static SaveEnvelope Read(string json, SaveKind kind, long nowTicks)
    {
        JToken root = TryParse(json);
        if (root == null)
            return new SaveEnvelope { Version = CurrentVersion, SavedAtTicks = 0, Data = null };

        SaveEnvelope envelope = Unwrap(root);

        if (envelope.Version > CurrentVersion)
            return envelope;

        for (int fromVersion = envelope.Version; fromVersion < CurrentVersion; fromVersion++)
            Migrate(kind, fromVersion, envelope, nowTicks);

        envelope.Version = CurrentVersion;
        return envelope;
    }

    public static string Write(JToken data, long savedAtTicks)
    {
        JObject envelope = new JObject
        {
            ["Version"] = CurrentVersion,
            ["SavedAtTicks"] = savedAtTicks,
            ["Data"] = data ?? JValue.CreateNull()
        };
        return envelope.ToString(Formatting.Indented);
    }

    private static JToken TryParse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JToken.Parse(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static SaveEnvelope Unwrap(JToken root)
    {
        if (root is JObject obj)
        {
            JToken versionToken = obj["Version"];
            JToken dataToken = obj["Data"];
            if (versionToken != null && versionToken.Type == JTokenType.Integer && dataToken != null)
            {
                long savedAtTicks = 0;
                JToken savedAtToken = obj["SavedAtTicks"];
                if (savedAtToken != null && savedAtToken.Type == JTokenType.Integer)
                    savedAtTicks = savedAtToken.Value<long>();

                return new SaveEnvelope
                {
                    Version = versionToken.Value<int>(),
                    SavedAtTicks = savedAtTicks,
                    Data = dataToken
                };
            }
        }

        return new SaveEnvelope { Version = 1, SavedAtTicks = 0, Data = root };
    }

    private static void Migrate(SaveKind kind, int fromVersion, SaveEnvelope envelope, long nowTicks)
    {
        switch (kind, fromVersion)
        {
            case (SaveKind.Inventory, 1):
                InventoryV1ToV2(envelope.Data as JObject);
                break;
            case (SaveKind.Inventory, 2):
                InventoryV2ToV3(envelope.Data as JObject);
                break;
            case (SaveKind.Registry, 2):
                envelope.Data = RegistryV2ToV3(envelope.Data as JObject);
                break;
            case (SaveKind.Registry, 3):
                RegistryV3ToV4(envelope.Data as JObject, nowTicks);
                break;
        }
    }

    private static void InventoryV1ToV2(JObject data)
    {
        if (data == null)
            return;

        JToken adventureMaterial = data["AdventureMaterial"];
        if (adventureMaterial != null)
        {
            data.Remove("AdventureMaterial");
            data["Minerita"] = adventureMaterial;
        }

        data.Remove("PassiveMaterial");
        data.Remove("EvolutionEssence");
    }

    private static void InventoryV2ToV3(JObject data)
    {
        if (data == null)
            return;

        data.Remove("EquipmentGrids");
    }

    private static JToken RegistryV2ToV3(JObject data)
    {
        if (data == null)
            return null;

        JObject alive = new JObject();
        JObject departed = new JObject();

        foreach (JProperty property in data.Properties())
        {
            JObject creature = property.Value as JObject;
            if (creature == null)
                continue;

            foreach (string field in LegacyCreatureStatFields)
                creature.Remove(field);

            bool isDead = creature["IsDead"]?.Value<bool>() ?? false;
            bool isSold = creature["BusyState"]?.Value<string>() == "Sold";

            if (isDead || isSold)
                departed[property.Name] = creature;
            else
                alive[property.Name] = creature;
        }

        return new JObject { ["Alive"] = alive, ["Departed"] = departed };
    }

    private static void RegistryV3ToV4(JObject data, long nowTicks)
    {
        if (data == null)
            return;

        MigrateCreatureBucket(data["Alive"] as JObject, nowTicks);
        MigrateCreatureBucket(data["Departed"] as JObject, nowTicks);
    }

    private static void MigrateCreatureBucket(JObject bucket, long nowTicks)
    {
        if (bucket == null)
            return;

        foreach (JProperty property in bucket.Properties())
        {
            JObject creature = property.Value as JObject;
            if (creature == null)
                continue;

            int ageRealDays = RealDaysSinceBirth(creature["BirthDate"], nowTicks);
            creature["BirthDay"] = 1 - ageRealDays;

            long breedReadyAt = creature["BreedReadyAt"]?.Value<long>() ?? 0;
            if (breedReadyAt > 0)
                creature["BreedReadyAt"] = 1;
        }
    }

    private static int RealDaysSinceBirth(JToken birthDateToken, long nowTicks)
    {
        DateTime? birthDate = ParseBirthDate(birthDateToken);
        if (birthDate == null)
            return 0;

        int days = (int)(new DateTime(nowTicks, DateTimeKind.Utc) - birthDate.Value).TotalDays;
        return days < 0 ? 0 : days;
    }

    private static DateTime? ParseBirthDate(JToken token)
    {
        if (token == null)
            return null;

        if (token.Type == JTokenType.Date)
            return token.Value<DateTime>();

        if (token.Type == JTokenType.String &&
            DateTime.TryParse(token.Value<string>(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsed))
            return parsed;

        return null;
    }
}

}
