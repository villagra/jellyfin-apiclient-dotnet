//using Jellyfin.ApiClient.Model;
//using MediaBrowser.Model.Entities;
//using MediaBrowser.Model.Querying;
//using MediaBrowser.Model.Session;
//using Microsoft.Extensions.Logging.Abstractions;
using Jellyfin.ApiClient.Auth;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace Jellyfin.ApiClient.Tests
{
    [TestClass]
    public class AuthTests
    {
        [TestMethod]
        public async Task InvalidServerTest()
        {
            JellyfinClient client = new JellyfinClient(new Uri("http://1.1.1.1:1"), new UserAuthentication("unitests", "pc-dev", "pc-dev-id", "0.0.1"));
            await Assert.ThrowsExceptionAsync<HttpRequestException>(async () => await client.AuthenticateUserAsync("admin", "aaaaaaaaaa"));
        }

        [TestMethod]
        public async Task AuthenticateInvalidCredentialsTest()
        {
            JellyfinClient client = new JellyfinClient(new Uri("http://192.168.1.230:32770"), new UserAuthentication("unitests", "pc-dev", "pc-dev-id", "0.0.1"));                                    
            await Assert.ThrowsExceptionAsync<AuthenticationException>(async () => await client.AuthenticateUserAsync("admin", "aaaaaaaaaa"));
        }

        [TestMethod]
        public async Task AuthenticateUserAsyncTest()
        {
            JellyfinClient client = new JellyfinClient(new Uri("http://192.168.1.230:32770"), new UserAuthentication("unitests", "pc-dev", "pc-dev-id", "0.0.1"));            
            var result = await client.AuthenticateUserAsync("admin", "");
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.User);
        }
    }
}
