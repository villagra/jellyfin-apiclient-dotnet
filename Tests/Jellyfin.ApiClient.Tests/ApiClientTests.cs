//using Jellyfin.ApiClient.Model;
//using MediaBrowser.Model.Entities;
//using MediaBrowser.Model.Querying;
//using MediaBrowser.Model.Session;
//using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Jellyfin.ApiClient.Tests
{
    [TestClass]
    public class ApiClientTests
    {
        [TestMethod]
        public async Task AuthenticateUserAsyncTest()
        {
            Console.WriteLine("Hello World!");

            /*
            var device = new Device
            {
                DeviceName = "My Device Name",
                DeviceId = "My Device Id"
            };
            
            ApiClient client = new ApiClient(NullLogger.Instance, new Uri("https://demo.jellyfin.org/stable/"), "test", device, "5.0.1");            

            var result = await client.AuthenticateUserAsync("demo", String.Empty);
            var views = await client.GetUserViews(result.SessionInfo.UserId);

            // Get the ten most recently added items for the current user
            var items = await client.GetItemsAsync(new ItemQuery
            {
                UserId = client.CurrentUserId,
                SortBy = new[] { ItemSortBy.DateCreated },
                SortOrder = SortOrder.Descending,

                // Get media only, don't return folder items
                Filters = new[] { ItemFilter.IsNotFolder },

                Limit = 10,

                // Search recursively through the user's library
                Recursive = true
            });*/

        }
    }
}
