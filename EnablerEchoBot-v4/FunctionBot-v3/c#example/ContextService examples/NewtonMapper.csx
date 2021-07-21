using Newtonsoft.Json;
using Jose;
/// <summary>
/// Custom mapper, such that we can use Newtonsoft and it's facility to ignore null values.
/// </summary>
public class NewtonsoftMapper : IJsonMapper
{
    public string Serialize(object obj)
    {
        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        return JsonConvert.SerializeObject(obj, Formatting.Indented, settings);
    }

    public T Parse<T>(string json)
    {
        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        return JsonConvert.DeserializeObject<T>(json, settings);
    }
}