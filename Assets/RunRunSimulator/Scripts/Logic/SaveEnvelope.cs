using Newtonsoft.Json.Linq;
namespace MoriMonchiSimulator
{

public class SaveEnvelope
{
    public int Version;
    public long SavedAtTicks;
    public JToken Data;
}

}
