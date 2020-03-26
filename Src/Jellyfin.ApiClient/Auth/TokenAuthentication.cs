using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.ApiClient.Auth
{
    public class TokenAuthentication : IAuthenticationMethod
    {
        public String Token { get; set; }

        public TokenAuthentication(string token)
        {
            Token = String.IsNullOrWhiteSpace(token) ? throw new ArgumentException(nameof(token)) : token;
        }
    }
}
