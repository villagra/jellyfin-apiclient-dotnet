using MediaBrowser.Model.Dto;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.ApiClient.Model
{
    public class AuthenticationResult
    {
        public string AccessToken { get; set; }

        public string ServerId { get; set; }

        public UserDto User { get; set; }

        public SessionInfo SessionInfo { get; set; }
    }
}
