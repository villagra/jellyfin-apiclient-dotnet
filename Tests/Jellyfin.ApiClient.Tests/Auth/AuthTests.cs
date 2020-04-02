//using Jellyfin.ApiClient.Model;;
//using MediaBrowser.Model.Entities;
//using MediaBrowser.Model.Querying;
//using MediaBrowser.Model.Session;
//using Microsoft.Extensions.Logging.Abstractions;
using Jellyfin.ApiClient.Auth;
using Jellyfin.ApiClient.Exceptions;
using Jellyfin.ApiClient.Tests.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace Jellyfin.ApiClient.Tests.Auth
{
    [TestClass]
    public class AuthTests : BaseTests
    {
        [TestMethod]
        public async Task InvalidServerTest()
        {
            (JellyfinClient client, Mock<MockHandler> handlerMock) = CreateMockClient();

            handlerMock.Setup(p => p.SendAsync(It.IsAny<HttpMethod>(), It.IsAny<string>())).Throws(new HttpRequestException());
            await Assert.ThrowsExceptionAsync<HttpRequestException>(async () => await client.AuthenticateUserAsync("admin", "aaaaaaaaaa"));
        }

        [TestMethod]
        public async Task AuthenticateInvalidCredentialsTest()
        {
            (JellyfinClient client, Mock<MockHandler> handlerMock) = CreateMockClient();

            handlerMock.Setup(p => p.SendAsync(It.IsAny<HttpMethod>(), It.IsAny<string>())).Throws(new RequestFailedException(System.Net.HttpStatusCode.InternalServerError));            
            await Assert.ThrowsExceptionAsync<AuthenticationException>(async () => await client.AuthenticateUserAsync("admin", "aaaaaaaaaa"));

            handlerMock.Setup(p => p.SendAsync(It.IsAny<HttpMethod>(), It.IsAny<string>())).Throws(new RequestFailedException(System.Net.HttpStatusCode.Unauthorized));
            await Assert.ThrowsExceptionAsync<AuthenticationException>(async () => await client.AuthenticateUserAsync("admin", "aaaaaaaaaa"));
        }

        [TestMethod]
        public async Task AuthenticateUserAsyncTest()
        {
            (JellyfinClient client, Mock<MockHandler> handlerMock) = CreateMockClient();

            var responseContent = MockUtils.GetFileContents(Path.Combine("Auth", "AuthenticateUserAsyncTest.json"));
            handlerMock.Setup(p => p.SendAsync(It.IsAny<HttpMethod>(), It.IsAny<string>())).Returns(MockUtils.Success(responseContent));            
            
            var result = await client.AuthenticateUserAsync("admin ", "password");
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.User);
        }
    }
}
