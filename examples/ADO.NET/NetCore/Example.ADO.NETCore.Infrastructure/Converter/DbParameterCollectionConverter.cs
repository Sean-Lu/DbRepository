using System;
using System.Collections.Generic;
using System.Data.Common;
using Newtonsoft.Json;

namespace Example.ADO.NETCore.Infrastructure.Converter
{
    public class DbParameterCollectionConverter : JsonConverter<DbParameterCollection>
    {
        public override void WriteJson(JsonWriter writer, DbParameterCollection value, JsonSerializer serializer)
        {
            if (value == null)
            {
                return;
            }

            var dic = new Dictionary<string, object>();
            foreach (DbParameter dbParameter in value)
            {
                dic.Add(dbParameter.ParameterName, dbParameter.Value);
            }

            serializer.Serialize(writer, dic);
        }

        public override DbParameterCollection ReadJson(JsonReader reader, Type objectType, DbParameterCollection existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}