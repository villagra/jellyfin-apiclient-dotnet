using Jellyfin.ApiClient.Auth;
using Jellyfin.ApiClient.Tests.Mocks;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.ApiClient.Tests
{
    public abstract class BaseTests
    {
        protected (JellyfinClient, Mock<MockHandler>) CreateMockClient()
        {
            var handlerMock = new Mock<MockHandler>() { CallBase = true };

            JellyfinClientOptions options = new JellyfinClientOptions();
            options.Handler = handlerMock.Object;

            JellyfinClient client = new JellyfinClient(new Uri("http://jellyfinserver.example"), new UserAuthentication("unitests", "pc-dev", "pc-dev-id", "0.0.1"), options);
            return (client, handlerMock);
        }
    }
}
