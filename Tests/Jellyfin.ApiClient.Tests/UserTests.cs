using Jellyfin.ApiClient.Auth;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Jellyfin.ApiClient.Tests
{
    [TestClass]
    public class UserTests
    {
        [TestMethod]
        public async Task GetViews()
        {
            JellyfinClient client = new JellyfinClient(new Uri("http://192.168.1.230:32770"), new UserAuthentication("unitests", "pc-dev", "pc-dev-id", "0.0.1"));
            var result = await client.AuthenticateUserAsync("admin", "");
            var views = await client.GetUserViews(result.User.Id.ToString());

        }
    }
}
