using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.ApiClient.Model
{
    public interface IFilters
    {
        Dictionary<string, string> GetFilters();
    }
}
