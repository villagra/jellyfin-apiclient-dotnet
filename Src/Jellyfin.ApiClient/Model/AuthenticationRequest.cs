using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Jellyfin.ApiClient.Model
{
    public class AuthenticationRequest
    {
        public String Username { get; private set; }

        [JsonProperty("PW")]
        public String Password { get; private set; }

        public AuthenticationRequest(string username, string password)
        {
            this.Username = username;
            this.Password = password;
        }
    }
}
