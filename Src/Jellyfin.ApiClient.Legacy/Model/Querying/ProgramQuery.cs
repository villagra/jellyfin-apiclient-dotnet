﻿using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using System;

namespace Jellyfin.ApiClient.Model.Querying
{
    /// <summary>
    /// Class ProgramQuery.
    /// </summary>
    public class ProgramQuery
    {
        public ProgramQuery()
        {
            ChannelIds = new string[] { };
            OrderBy = new Tuple<string, SortOrder>[] { };
            Genres = new string[] { };
            GenreIds = new string[] { };
            EnableTotalRecordCount = true;
            EnableUserData = true;
        }

        public bool EnableTotalRecordCount { get; set; }
        public bool EnableUserData { get; set; }

        /// <summary>
        /// Fields to return within the items, in addition to basic information
        /// </summary>
        /// <value>The fields.</value>
        public ItemFields[] Fields { get; set; }
        public bool? EnableImages { get; set; }
        public int? ImageTypeLimit { get; set; }
        public ImageType[] EnableImageTypes { get; set; }

        /// <summary>
        /// Gets or sets the channel ids.
        /// </summary>
        /// <value>The channel ids.</value>
        public string[] ChannelIds { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public string UserId { get; set; }
        public string SeriesTimerId { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// The earliest date for which a program starts to return
        /// </summary>
        public DateTime? MinStartDate { get; set; }

        /// <summary>
        /// The latest date for which a program starts to return
        /// </summary>
        public DateTime? MaxStartDate { get; set; }

        /// <summary>
        /// The earliest date for which a program ends to return
        /// </summary>
        public DateTime? MinEndDate { get; set; }

        /// <summary>
        /// The latest date for which a program ends to return
        /// </summary>
        public DateTime? MaxEndDate { get; set; }

        /// <summary>
        /// Used to specific whether to return news or not
        /// </summary>
        /// <remarks>If set to null, all programs will be returned</remarks>
        public bool? IsNews { get; set; }

        /// <summary>
        /// Used to specific whether to return movies or not
        /// </summary>
        /// <remarks>If set to null, all programs will be returned</remarks>
        public bool? IsMovie { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is kids.
        /// </summary>
        /// <value><c>null</c> if [is kids] contains no value, <c>true</c> if [is kids]; otherwise, <c>false</c>.</value>
        public bool? IsKids { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is sports.
        /// </summary>
        /// <value><c>null</c> if [is sports] contains no value, <c>true</c> if [is sports]; otherwise, <c>false</c>.</value>
        public bool? IsSports { get; set; }

        /// <summary>
        /// Skips over a given number of items within the results. Use for paging.
        /// </summary>
        public int? StartIndex { get; set; }
        public bool? IsSeries { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has aired.
        /// </summary>
        /// <value><c>null</c> if [has aired] contains no value, <c>true</c> if [has aired]; otherwise, <c>false</c>.</value>
        public bool? HasAired { get; set; }

        /// <summary>
        /// The maximum number of items to return
        /// </summary>
        public int? Limit { get; set; }

        public Tuple<string, SortOrder>[] OrderBy { get; set; }

        /// <summary>
        /// Limit results to items containing specific genres
        /// </summary>
        /// <value>The genres.</value>
        public string[] GenreIds { get; set; }
        public string[] Genres { get; set; }
    }
}
