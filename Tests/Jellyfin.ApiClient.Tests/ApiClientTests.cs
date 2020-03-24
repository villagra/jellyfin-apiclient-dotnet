using Jellyfin.ApiClient.Model;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging.Abstractions;
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

            var device = new Device
            {
                DeviceName = "My Device Name",
                DeviceId = "My Device Id"
            };

            ApiClient client = new ApiClient(NullLogger.Instance, new Uri("http://192.168.1.230:32770"), "test", device, "5.0.1");
            var result = await client.AuthenticateUserAsync("", "");
        }
    }
}
