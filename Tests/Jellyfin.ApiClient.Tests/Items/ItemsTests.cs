using Jellyfin.ApiClient.Auth;
using Jellyfin.ApiClient.Model;
using Jellyfin.ApiClient.Tests.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Jellyfin.ApiClient.Tests.Items
{
    [TestClass]
    public class ItemsTests : BaseTests
    {
        [TestMethod]
        public async Task GetItemsAsync()
        {
            (JellyfinClient client, Mock<MockHandler> handlerMock) = CreateMockClient();

            var responseContent = MockUtils.GetFileContents(Path.Combine("Items","GetItemsAsync.json"));
            handlerMock.Setup(p => p.SendAsync(It.IsAny<HttpMethod>(), It.IsAny<string>())).Returns(MockUtils.Success(responseContent));
            var items = await client.GetItemsAsync(Guid.NewGuid().ToString());

            responseContent = MockUtils.GetFileContents(Path.Combine("Items", "GetItemsAsync2.json"));
            handlerMock.Setup(p => p.SendAsync(It.IsAny<HttpMethod>(), It.IsAny<string>())).Returns(MockUtils.Success(responseContent));
            ItemFilters filters = ItemFilters.Create().FilterByParentId(items.Items[0].Id);
            items = await client.GetItemsAsync(Guid.NewGuid().ToString(), filters);

            Assert.AreEqual(1, items.Items.OfType<FolderItem>().Count());
            Assert.AreEqual(7, items.Items.OfType<MovieItem>().Count());

            var fMovie = items.Items.OfType<MovieItem>().Where(p => p.Name == "Ford v Ferrari").SingleOrDefault();
            Assert.IsNotNull(fMovie);
            Assert.AreEqual(1, fMovie.ImageTags.Count());
        }

    }
}
