using Newtonsoft.Json;
using Sean.Utility.Contracts;

namespace Example.Dapper.Infrastructure.Impls
{
    public class NewJsonSerializer : IJsonSerializer
    {
        public static IJsonSerializer Instance { get; } = new NewJsonSerializer();

        public string Serialize<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
