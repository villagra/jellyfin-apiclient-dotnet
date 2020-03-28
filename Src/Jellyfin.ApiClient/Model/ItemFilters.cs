using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.ApiClient.Model
{
    public class ItemFilters : IFilters
    {
        static string FILTER_PARENTID = "ParentId";

        Dictionary<string, string> filters = new Dictionary<string, string>();

        public static ItemFilters Create()
        {
            return new ItemFilters();
        }

        public ItemFilters FilterByParentId(String parentId)
        {
            if (filters.ContainsKey(FILTER_PARENTID))
            {
                filters[FILTER_PARENTID] = parentId;
            }
            else
            {
                filters.Add(FILTER_PARENTID, parentId);
            }

            return this;
        }

        public Dictionary<string, string> GetFilters()
        {
            return filters;
        }
    }
}
