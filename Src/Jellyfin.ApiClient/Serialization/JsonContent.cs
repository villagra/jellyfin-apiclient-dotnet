using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Jellyfin.ApiClient.Serialization
{
    public class JsonContent : StringContent
    {
        public JsonContent(object data) : base(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json")
        { }
    }
}
