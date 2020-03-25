using Jellyfin.ApiClient.Model.Dto;
using MediaBrowser.Model.Dto;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.ApiClient.Model
{
    public class AuthenticationResult
    {
        public UserDto User { get; set; }

        public SessionInfoDto SessionInfo { get; set; }

        public string AccessToken { get; set; }

        public string ServerId { get; set; }
    }
}
