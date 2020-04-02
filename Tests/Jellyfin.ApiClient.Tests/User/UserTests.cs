using Jellyfin.ApiClient.Auth;
using Jellyfin.ApiClient.Model;
using Jellyfin.ApiClient.Tests.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Jellyfin.ApiClient.Tests.User
{
    [TestClass]
    public class UserTests : BaseTests
    {
        [TestMethod]
        public async Task GetUserViewsTest()
        {
            (JellyfinClient client, Mock<MockHandler> handlerMock) = CreateMockClient();

            var responseContent = MockUtils.GetFileContents(Path.Combine("Items", "GetUserViewsTest.json"));
            handlerMock.Setup(p => p.SendAsync(It.IsAny<HttpMethod>(), It.IsAny<string>())).Returns(MockUtils.Success(responseContent));
            
            var views = await client.GetUserViews(Guid.NewGuid().ToString());
            Assert.IsNotNull(views);
        }
    }
}
