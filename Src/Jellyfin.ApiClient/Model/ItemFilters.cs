using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.ApiClient.Model
{
    public class ItemFilters : IFilters
    {
        static string FILTER_PARENTID = "ParentId";
        static string FILTER_RECURSIVE = "Recursive";
        static string FILTER_ITEMTYPE = "IncludeItemTypes";
        static string FILTER_GROUPITEMS = "GroupItems";
        static string FILTER_IS_PLAYED = "IsPlayed";
        static string FILTER_LIMIT = "Limit";
        static string FILTER_START_INDEX = "StartIndex";
        

        Dictionary<string, string> filters = new Dictionary<string, string>();

        public static ItemFilters Create()
        {
            return new ItemFilters();
        }

        public ItemFilters FilterByParentId(String parentId)
        {
            AddValue(FILTER_PARENTID, parentId);
            return this;
        }

        public ItemFilters Recursive(bool recursiveSearch = true)
        {
            AddValue(FILTER_RECURSIVE, recursiveSearch);
            return this;
        }

        public ItemFilters IsPlayed(bool isPlayed = true)
        {
            AddValue(FILTER_IS_PLAYED, isPlayed);
            return this;
        }

        public ItemFilters GroupItems(bool groupItems = true)
        {
            AddValue(FILTER_GROUPITEMS, groupItems);
            return this;
        }

        public ItemFilters IncludeItemType(ItemTypes type)
        {
            AddOrAppendValue(FILTER_ITEMTYPE, type.ToString());
            return this;
        }

        public ItemFilters Take(int value)
        {
            AddValue(FILTER_LIMIT, value.ToString());
            return this;
        }

        /// <summary>
        /// Skips over a given number of items within the results. Use for paging.
        /// </summary>
        /// <value>The start index.</value>
        public ItemFilters Skip(int value)
        {
            AddValue(FILTER_START_INDEX, value.ToString());
            return this;
        }

        public Dictionary<string, string> GetFilters()
        {
            return filters;
        }

        private void AddValue(string key, bool value)
        {
            AddValue(key, value.ToString().ToLower());
        }
        private void AddValue(string key, string value)
        {
            if (filters.ContainsKey(key))
            {
                filters[key] = value;
            }
            else
            {
                filters.Add(key, value);
            }
        }
        private void AddOrAppendValue(string key, string value)
        {
            if (filters.ContainsKey(key))
            {
                if (String.IsNullOrWhiteSpace(filters[key]))
                {
                    filters[key] = value;
                }
                else
                {
                    filters[key] += $",{value}";
                }
            }
            else
            {
                filters.Add(key, value);
            }
        }
    }
}
