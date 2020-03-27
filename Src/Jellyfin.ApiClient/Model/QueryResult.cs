using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.ApiClient.Model
{
    public class QueryResult<T> where T : BaseItem
    {
        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        public IReadOnlyList<T> Items { get; set; }

        /// <summary>
        /// The total number of records available
        /// </summary>
        public int TotalRecordCount { get; set; }

        public QueryResult()
        {

        }
    }
}
