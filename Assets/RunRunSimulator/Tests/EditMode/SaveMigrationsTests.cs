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
        Assert.IsNotNull(data["EquipmentGrids"]);
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

        Assert.AreEqual(3, data.Count);

        JObject rex = (JObject)data["BS0-H0-BK2-W1-FC1-8AB3DF-639245585551461158"];
        Assert.AreEqual("Rex", rex["CustomName"].Value<string>());
        Assert.AreEqual(639245585551461158L, rex["Timestamp"].Value<long>());
        Assert.AreEqual(87.5, rex["Needs"]["Health"].Value<double>());
        Assert.AreEqual(false, rex["IsDead"].Value<bool>());

        JObject mila = (JObject)data["BS1-H1-BK0-W0-FC0-112233-639245585551461200"];
        Assert.AreEqual("Mila", mila["CustomName"].Value<string>());
        Assert.AreEqual(639245585551461200L, mila["Timestamp"].Value<long>());
        Assert.AreEqual(42.0, mila["Needs"]["Health"].Value<double>());
        Assert.AreEqual(true, mila["IsDead"].Value<bool>());

        JObject toby = (JObject)data["BS0-H0-BK1-W1-FC1-AABBCC-639245585551461300"];
        Assert.AreEqual("Toby", toby["CustomName"].Value<string>());
        Assert.AreEqual(639245585551461300L, toby["Timestamp"].Value<long>());
        Assert.AreEqual(100.0, toby["Needs"]["Health"].Value<double>());
        Assert.AreEqual(false, toby["IsDead"].Value<bool>());
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
}
