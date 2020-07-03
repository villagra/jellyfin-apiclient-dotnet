using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.ApiClient.Model
{
    public class FiltersResponse
    {
        public List<string> Genres { get; set; }
        public List<string> Tags { get; set; }
        public List<string> OfficialRatings { get; set; }
        public List<string> Years { get; set; }
    }
}
