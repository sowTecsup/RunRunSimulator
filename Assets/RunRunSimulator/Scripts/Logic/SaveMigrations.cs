using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace MoriMonchiSimulator
{

public enum SaveKind
{
    Registry,
    Furniture,
    Inventory,
    Social
}

public static class SaveMigrations
{
    public const int CurrentVersion = 2;

    public static SaveEnvelope Read(string json, SaveKind kind)
    {
        JToken root = TryParse(json);
        if (root == null)
            return new SaveEnvelope { Version = CurrentVersion, SavedAtTicks = 0, Data = null };

        SaveEnvelope envelope = Unwrap(root);

        if (envelope.Version > CurrentVersion)
            return envelope;

        for (int fromVersion = envelope.Version; fromVersion < CurrentVersion; fromVersion++)
            Migrate(kind, fromVersion, envelope);

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

    private static void Migrate(SaveKind kind, int fromVersion, SaveEnvelope envelope)
    {
        switch (kind, fromVersion)
        {
            case (SaveKind.Inventory, 1):
                InventoryV1ToV2(envelope.Data as JObject);
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
}

}
