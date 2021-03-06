﻿using MediaBrowser.Model.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.ApiClient.Model
{
    public class ItemFilters : IFilters
    {        
        static string FILTER_SERIESID = "SeriesId";
        static string FILTER_SEASONID = "seasonId";
        static string FILTER_USERID = "userId";
        static string FILTER_PARENTID = "ParentId";
        static string FILTER_RECURSIVE = "Recursive";
        static string FILTER_ITEMTYPE = "IncludeItemTypes";
        static string FILTER_GROUPITEMS = "GroupItems";
        static string FILTER_IS_PLAYED = "IsPlayed";
        static string FILTER_LIMIT = "Limit";
        static string FILTER_START_INDEX = "StartIndex";
        static string FILTER_FIELDS = "Fields";
        static string SEARCH = "searchTerm";
        static string SORT = "SortBy";
        static string SORT_ORDER = "SortOrder";



        Dictionary<string, string> filters = new Dictionary<string, string>();

        public static ItemFilters Create()
        {
            return new ItemFilters();
        }

        internal ItemFilters FilterBySeriesId(string seriesId)
        {
            AddValue(FILTER_SERIESID, seriesId);
            return this;
        }

        public ItemFilters SearchBy(string query)
        {
            AddValue(SEARCH, query);
            return this;
        }

        public ItemFilters FilterBySeasonId(String seasonId)
        {
            AddValue(FILTER_SEASONID, seasonId);
            return this;
        }
        
        public ItemFilters FilterByUserId(String userId)
        {
            AddValue(FILTER_USERID, userId);
            return this;
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

        public ItemFilters Add(Dictionary<string, List<string>> filters)
        {
            if (filters != null)
            {
                foreach (var filter in filters)
                {
                    foreach (var filterValue in filter.Value)
                    {
                        AddOrAppendValue(filter.Key, filterValue);
                    }
                }
            }
            return this;
        }

        public ItemFilters IncludeField(string field)
        {
            AddOrAppendValue(FILTER_FIELDS, field);
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

        public ItemFilters SortBy(SortOptions sort)
        {
            AddValue(SORT, sort.ToString());
            return this;
        }

        public ItemFilters Order(SortOrder order)
        {
            AddValue(SORT_ORDER, order.ToString());
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
