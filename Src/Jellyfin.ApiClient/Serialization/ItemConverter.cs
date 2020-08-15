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
            BaseItem obj;

            var type = jObject["Type"].ToString();
            switch (type)
            {
                case "CollectionFolder":
                    obj = new CollectionFolderItem();
                    break;
                case "Folder":
                    obj = new FolderItem();
                    break;
                case "Movie":
                    obj = new MovieItem();
                    break;
                case "Episode":
                    obj = new EpisodeItem();
                    break;
                case "Audio":
                    obj = new AudioItem();
                    break;
                case "Series":
                    obj = new SeriesItem();
                    break;
                case "Season":
                    obj = new SeasonItem();
                    break;
                case "MusicAlbum":
                    obj = new MusicAlbumItem();
                    break;
                case "Actor":
                case "Director":
                case "Writer":
                case "Producer":
                case "Person":
                    obj = new Person();
                    break;
                default:
                    obj = new BaseItem();
                    break;
            }            
            
            serializer.Populate(jObject.CreateReader(), obj);
            return obj;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
