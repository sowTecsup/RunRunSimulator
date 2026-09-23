using System;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using MoriMonchiSimulator;

public class SaveMigrationsTests
{
    [Test]
    public void LegacyInventory_V1ToV2_RenamesAdventureMaterialToMinerita()
    {
        string json = "{\"FurnitureOwned\":[],\"WorldPropsStored\":[\"I1\",\"I1\"],\"EquipmentGrids\":{\"Weapon\":[]},\"HotbarSlots\":[null,null],\"Dabloons\":84,\"AdventureMaterial\":93,\"PassiveMaterial\":0,\"EvolutionEssence\":0}";

        SaveEnvelope envelope = SaveMigrations.Read(json, SaveKind.Inventory);

        Assert.AreEqual(SaveMigrations.CurrentVersion, envelope.Version);
        JObject data = (JObject)envelope.Data;
        Assert.AreEqual(93, data["Minerita"].Value<int>());
        Assert.AreEqual(84, data["Dabloons"].Value<int>());
        Assert.IsNull(data["AdventureMaterial"]);
        Assert.IsNull(data["PassiveMaterial"]);
        Assert.IsNull(data["EvolutionEssence"]);
        Assert.IsNotNull(data["FurnitureOwned"]);
        Assert.IsNull(data["EquipmentGrids"]);
    }

    [Test]
    public void LegacyInventory_V1_ReachesV3WithMineritaAndWithoutEquipmentGrids()
    {
        string json = "{\"FurnitureOwned\":[],\"WorldPropsStored\":[\"I1\",\"I1\"],\"EquipmentGrids\":{\"Weapon\":[]},\"HotbarSlots\":[null,null],\"Dabloons\":84,\"AdventureMaterial\":93,\"PassiveMaterial\":0,\"EvolutionEssence\":0}";

        SaveEnvelope envelope = SaveMigrations.Read(json, SaveKind.Inventory);
        JObject data = (JObject)envelope.Data;

        Assert.AreEqual(SaveMigrations.CurrentVersion, envelope.Version);
        Assert.AreEqual(93, data["Minerita"].Value<int>());
        Assert.IsNull(data["EquipmentGrids"]);
        Assert.IsNull(data["AdventureMaterial"]);
    }

    [Test]
    public void CurrentVersionEnvelope_RoundTrip_KeepsMineritaAndSavedAtTicks()
    {
        JObject data = new JObject { ["Minerita"] = 5, ["Dabloons"] = 10 };
        string json = SaveMigrations.Write(data, 987654321L);

        SaveEnvelope envelope = SaveMigrations.Read(json, SaveKind.Inventory);

        Assert.AreEqual(SaveMigrations.CurrentVersion, envelope.Version);
        Assert.AreEqual(987654321L, envelope.SavedAtTicks);
        Assert.AreEqual(5, envelope.Data["Minerita"].Value<int>());
    }

    [Test]
    public void CurrentVersionEnvelope_WithoutAdventureMaterial_DoesNotInventMinerita()
    {
        JObject data = new JObject { ["Dabloons"] = 10 };
        string json = SaveMigrations.Write(data, 0);

        SaveEnvelope envelope = SaveMigrations.Read(json, SaveKind.Inventory);

        Assert.IsNull(envelope.Data["Minerita"]);
    }

    [Test]
    public void LegacyRegistry_ThreeCreatures_FieldsStayIntact()
    {
        string json = "{" +
            "\"BS0-H0-BK2-W1-FC1-8AB3DF-639245585551461158\":{\"CustomName\":\"Rex\",\"Timestamp\":639245585551461158,\"Needs\":{\"Health\":87.5},\"IsDead\":false}," +
            "\"BS1-H1-BK0-W0-FC0-112233-639245585551461200\":{\"CustomName\":\"Mila\",\"Timestamp\":639245585551461200,\"Needs\":{\"Health\":42.0},\"IsDead\":true}," +
            "\"BS0-H0-BK1-W1-FC1-AABBCC-639245585551461300\":{\"CustomName\":\"Toby\",\"Timestamp\":639245585551461300,\"Needs\":{\"Health\":100.0},\"IsDead\":false}" +
            "}";

        SaveEnvelope envelope = SaveMigrations.Read(json, SaveKind.Registry);
        JObject data = (JObject)envelope.Data;
        JObject alive = (JObject)data["Alive"];
        JObject departed = (JObject)data["Departed"];

        Assert.AreEqual(2, alive.Count);
        Assert.AreEqual(1, departed.Count);

        JObject rex = (JObject)alive["BS0-H0-BK2-W1-FC1-8AB3DF-639245585551461158"];
        Assert.AreEqual("Rex", rex["CustomName"].Value<string>());
        Assert.AreEqual(639245585551461158L, rex["Timestamp"].Value<long>());
        Assert.AreEqual(87.5, rex["Needs"]["Health"].Value<double>());
        Assert.AreEqual(false, rex["IsDead"].Value<bool>());

        JObject mila = (JObject)departed["BS1-H1-BK0-W0-FC0-112233-639245585551461200"];
        Assert.AreEqual("Mila", mila["CustomName"].Value<string>());
        Assert.AreEqual(639245585551461200L, mila["Timestamp"].Value<long>());
        Assert.AreEqual(42.0, mila["Needs"]["Health"].Value<double>());
        Assert.AreEqual(true, mila["IsDead"].Value<bool>());

        JObject toby = (JObject)alive["BS0-H0-BK1-W1-FC1-AABBCC-639245585551461300"];
        Assert.AreEqual("Toby", toby["CustomName"].Value<string>());
        Assert.AreEqual(639245585551461300L, toby["Timestamp"].Value<long>());
        Assert.AreEqual(100.0, toby["Needs"]["Health"].Value<double>());
        Assert.AreEqual(false, toby["IsDead"].Value<bool>());
    }

    [Test]
    public void LegacyRegistry_V1_SplitsIntoAliveAndDepartedAndKeepsGeneration()
    {
        string json = "{" +
            "\"BS0-H0-BK0-W0-FC0-111111-1\":{\"CustomName\":\"Alive1\",\"IsDead\":false,\"Generation\":2}," +
            "\"BS0-H0-BK0-W0-FC0-222222-2\":{\"CustomName\":\"Alive2\",\"IsDead\":false}," +
            "\"BS0-H0-BK0-W0-FC0-333333-3\":{\"CustomName\":\"Dead1\",\"IsDead\":true}," +
            "\"BS0-H0-BK0-W0-FC0-444444-4\":{\"CustomName\":\"Sold1\",\"IsDead\":false,\"BusyState\":\"Sold\"}" +
            "}";

        SaveEnvelope envelope = SaveMigrations.Read(json, SaveKind.Registry);
        JObject data = (JObject)envelope.Data;
        JObject alive = (JObject)data["Alive"];
        JObject departed = (JObject)data["Departed"];

        Assert.AreEqual(2, alive.Count);
        Assert.AreEqual(2, departed.Count);
        Assert.AreEqual(2, alive["BS0-H0-BK0-W0-FC0-111111-1"]["Generation"].Value<int>());
        Assert.IsNull(alive["BS0-H0-BK0-W0-FC0-222222-2"]["Generation"]);
        Assert.IsNotNull(departed["BS0-H0-BK0-W0-FC0-333333-3"]);
        Assert.IsNotNull(departed["BS0-H0-BK0-W0-FC0-444444-4"]);
    }

    [Test]
    public void RegistryV2_WithLegacyStatsAndEquipped_RemovesThemFromBothShelves()
    {
        string json = "{\"Version\":2,\"SavedAtTicks\":0,\"Data\":{" +
            "\"BS0-H0-BK0-W0-FC0-111111-1\":{\"CustomName\":\"Alive1\",\"IsDead\":false,\"BaseConstitution\":5.0,\"BaseAttack\":3.0,\"BaseSpeed\":2.0,\"BaseDefense\":1.0,\"BaseLuck\":0.5,\"BaseEvasion\":0.2,\"Equipped\":{\"Weapon\":\"E1\"}}," +
            "\"BS0-H0-BK0-W0-FC0-222222-2\":{\"CustomName\":\"Dead1\",\"IsDead\":true,\"BaseConstitution\":5.0,\"BaseAttack\":3.0,\"BaseSpeed\":2.0,\"BaseDefense\":1.0,\"BaseLuck\":0.5,\"BaseEvasion\":0.2,\"Equipped\":{\"Weapon\":\"E1\"}}" +
            "}}";

        SaveEnvelope envelope = SaveMigrations.Read(json, SaveKind.Registry);
        JObject data = (JObject)envelope.Data;
        JObject alive = (JObject)data["Alive"];
        JObject departed = (JObject)data["Departed"];

        string[] legacyFields = { "BaseConstitution", "BaseAttack", "BaseSpeed", "BaseDefense", "BaseLuck", "BaseEvasion", "Equipped" };
        JObject aliveCreature = (JObject)alive["BS0-H0-BK0-W0-FC0-111111-1"];
        JObject deadCreature = (JObject)departed["BS0-H0-BK0-W0-FC0-222222-2"];
        foreach (string field in legacyFields)
        {
            Assert.IsNull(aliveCreature[field]);
            Assert.IsNull(deadCreature[field]);
        }
    }

    [Test]
    public void RegistryV2_NullData_DoesNotThrow()
    {
        string json = "{\"Version\":2,\"SavedAtTicks\":0,\"Data\":null}";
        SaveEnvelope envelope = null;
        Assert.DoesNotThrow(() => envelope = SaveMigrations.Read(json, SaveKind.Registry));
        Assert.AreEqual(SaveMigrations.CurrentVersion, envelope.Version);
    }

    [Test]
    public void CorruptRegistryJson_DoesNotThrow()
    {
        SaveEnvelope envelope = null;
        Assert.DoesNotThrow(() => envelope = SaveMigrations.Read("{\"Version\":2,\"Data\":{not valid", SaveKind.Registry));
        Assert.AreEqual(SaveMigrations.CurrentVersion, envelope.Version);
        Assert.IsNull(envelope.Data);
    }

    [Test]
    public void Read_NullJson_ReturnsEmptyEnvelopeWithoutThrowing()
    {
        SaveEnvelope envelope = null;
        Assert.DoesNotThrow(() => envelope = SaveMigrations.Read(null, SaveKind.Registry));
        Assert.AreEqual(SaveMigrations.CurrentVersion, envelope.Version);
        Assert.IsNull(envelope.Data);
    }

    [Test]
    public void Read_EmptyJson_ReturnsEmptyEnvelopeWithoutThrowing()
    {
        SaveEnvelope envelope = null;
        Assert.DoesNotThrow(() => envelope = SaveMigrations.Read("", SaveKind.Registry));
        Assert.AreEqual(SaveMigrations.CurrentVersion, envelope.Version);
        Assert.IsNull(envelope.Data);
    }

    [Test]
    public void Read_WhitespaceJson_ReturnsEmptyEnvelopeWithoutThrowing()
    {
        SaveEnvelope envelope = null;
        Assert.DoesNotThrow(() => envelope = SaveMigrations.Read("   ", SaveKind.Registry));
        Assert.AreEqual(SaveMigrations.CurrentVersion, envelope.Version);
        Assert.IsNull(envelope.Data);
    }

    [Test]
    public void Read_MalformedJson_ReturnsEmptyEnvelopeWithoutThrowing()
    {
        SaveEnvelope envelope = null;
        Assert.DoesNotThrow(() => envelope = SaveMigrations.Read("{no es json", SaveKind.Registry));
        Assert.AreEqual(SaveMigrations.CurrentVersion, envelope.Version);
        Assert.IsNull(envelope.Data);
    }

    [Test]
    public void Read_JsonArrayInsteadOfObject_DoesNotThrowAndDoesNotCorrupt()
    {
        SaveEnvelope envelope = null;
        Assert.DoesNotThrow(() => envelope = SaveMigrations.Read("[1,2,3]", SaveKind.Inventory));
        Assert.AreEqual(SaveMigrations.CurrentVersion, envelope.Version);
        JArray data = (JArray)envelope.Data;
        Assert.AreEqual(3, data.Count);
        Assert.AreEqual(1, data[0].Value<int>());
        Assert.AreEqual(2, data[1].Value<int>());
        Assert.AreEqual(3, data[2].Value<int>());
    }

    [Test]
    public void WriteThenRead_NullData_DoesNotThrow()
    {
        string json = SaveMigrations.Write(null, 0);
        Assert.DoesNotThrow(() => SaveMigrations.Read(json, SaveKind.Registry));
    }

    [Test]
    public void RegistryV3_BirthDateAsIsoString_BirthDayFromRealAgeWithFixedNow()
    {
        long nowTicks = new DateTime(2024, 1, 11, 0, 0, 0, DateTimeKind.Utc).Ticks;
        string json = "{\"Version\":3,\"SavedAtTicks\":0,\"Data\":{\"Alive\":{" +
            "\"BS0-H0-BK0-W0-FC0-111111-1\":{\"CustomName\":\"Rex\",\"BirthDate\":\"2024-01-01T00:00:00Z\",\"IsDead\":false}" +
            "},\"Departed\":{}}}";

        SaveEnvelope envelope = SaveMigrations.Read(json, SaveKind.Registry, nowTicks);
        JObject alive = (JObject)((JObject)envelope.Data)["Alive"];
        JObject rex = (JObject)alive["BS0-H0-BK0-W0-FC0-111111-1"];

        Assert.AreEqual(SaveMigrations.CurrentVersion, envelope.Version);
        Assert.AreEqual(-9, rex["BirthDay"].Value<int>());
    }

    [Test]
    public void RegistryV3_CreatureWithoutBirthDate_BirthDayDefaultsToOne()
    {
        string json = "{\"Version\":3,\"SavedAtTicks\":0,\"Data\":{\"Alive\":{" +
            "\"BS0-H0-BK0-W0-FC0-222222-2\":{\"CustomName\":\"Mila\",\"IsDead\":false}" +
            "},\"Departed\":{}}}";

        SaveEnvelope envelope = SaveMigrations.Read(json, SaveKind.Registry);
        JObject alive = (JObject)((JObject)envelope.Data)["Alive"];
        JObject mila = (JObject)alive["BS0-H0-BK0-W0-FC0-222222-2"];

        Assert.AreEqual(1, mila["BirthDay"].Value<int>());
    }

    [Test]
    public void RegistryV3_EggInProgress_BreedReadyAtBecomesOne()
    {
        string json = "{\"Version\":3,\"SavedAtTicks\":0,\"Data\":{\"Alive\":{" +
            "\"BS0-H0-BK0-W0-FC0-333333-3\":{\"CustomName\":\"Toby\",\"IsDead\":false,\"BreedReadyAt\":500000}" +
            "},\"Departed\":{}}}";

        SaveEnvelope envelope = SaveMigrations.Read(json, SaveKind.Registry);
        JObject alive = (JObject)((JObject)envelope.Data)["Alive"];
        JObject toby = (JObject)alive["BS0-H0-BK0-W0-FC0-333333-3"];

        Assert.AreEqual(1, toby["BreedReadyAt"].Value<long>());
    }

    [Test]
    public void RegistryV3_NoEgg_BreedReadyAtStaysZero()
    {
        string json = "{\"Version\":3,\"SavedAtTicks\":0,\"Data\":{\"Alive\":{" +
            "\"BS0-H0-BK0-W0-FC0-444444-4\":{\"CustomName\":\"Nina\",\"IsDead\":false,\"BreedReadyAt\":0}" +
            "},\"Departed\":{}}}";

        SaveEnvelope envelope = SaveMigrations.Read(json, SaveKind.Registry);
        JObject alive = (JObject)((JObject)envelope.Data)["Alive"];
        JObject nina = (JObject)alive["BS0-H0-BK0-W0-FC0-444444-4"];

        Assert.AreEqual(0, nina["BreedReadyAt"].Value<long>());
    }

    [Test]
    public void RegistryV3_DepartedCreatures_AlsoMigrate()
    {
        long nowTicks = new DateTime(2024, 1, 6, 0, 0, 0, DateTimeKind.Utc).Ticks;
        string json = "{\"Version\":3,\"SavedAtTicks\":0,\"Data\":{\"Alive\":{}," +
            "\"Departed\":{\"BS0-H0-BK0-W0-FC0-555555-5\":{\"CustomName\":\"Old\",\"IsDead\":true," +
            "\"BirthDate\":\"2024-01-01T00:00:00Z\",\"BreedReadyAt\":90}}}}";

        SaveEnvelope envelope = SaveMigrations.Read(json, SaveKind.Registry, nowTicks);
        JObject departed = (JObject)((JObject)envelope.Data)["Departed"];
        JObject old = (JObject)departed["BS0-H0-BK0-W0-FC0-555555-5"];

        Assert.AreEqual(-4, old["BirthDay"].Value<int>());
        Assert.AreEqual(1, old["BreedReadyAt"].Value<long>());
    }

    [Test]
    public void WorldEnvelope_AlreadyCurrentVersion_ReadsIntact()
    {
        string json = "{\"Version\":" + SaveMigrations.CurrentVersion + ",\"SavedAtTicks\":123,\"Data\":{\"Day\":5,\"MinuteOfDay\":720.0,\"TutorialStep\":2}}";

        SaveEnvelope envelope = SaveMigrations.Read(json, SaveKind.World);

        Assert.AreEqual(SaveMigrations.CurrentVersion, envelope.Version);
        Assert.AreEqual(123L, envelope.SavedAtTicks);
        Assert.AreEqual(5, envelope.Data["Day"].Value<int>());
        Assert.AreEqual(720.0, envelope.Data["MinuteOfDay"].Value<double>());
        Assert.AreEqual(2, envelope.Data["TutorialStep"].Value<int>());
    }
}
