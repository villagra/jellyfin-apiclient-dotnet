using Jellyfin.ApiClient.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.ApiClient.Serialization
{
    public class ItemConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.Name == "BaseItem";
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Load JObject from stream
            JObject jObject = JObject.Load(reader);

            var type = jObject["Type"].ToString();

            // Create target object based on JObject            
            if (type == "CollectionFolder")
            {
                var target = new CollectionFolderItem();
                serializer.Populate(jObject.CreateReader(), target);
                return target;
            }

            var t = new BaseItem();
            serializer.Populate(jObject.CreateReader(), t);
            return t;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
