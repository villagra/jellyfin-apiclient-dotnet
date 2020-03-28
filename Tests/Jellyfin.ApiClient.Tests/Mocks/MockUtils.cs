using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text;

namespace Jellyfin.ApiClient.Tests.Mocks
{
    public static class MockUtils
    {
        public static HttpResponseMessage Success(string content)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            response.Content = new StringContent(content);
            return response;
        }

        public static string GetFileContents(string fileName)
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), fileName);
            return System.IO.File.ReadAllText(path);
        }

    }
}
