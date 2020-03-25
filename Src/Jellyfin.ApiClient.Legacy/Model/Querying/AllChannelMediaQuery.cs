﻿using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace Jellyfin.ApiClient.Model.Querying
{
    public class AllChannelMediaQuery
    {
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

        /// <summary>
        /// Skips over a given number of items within the results. Use for paging.
        /// </summary>
        /// <value>The start index.</value>
        public int? StartIndex { get; set; }

        /// <summary>
        /// The maximum number of items to return
        /// </summary>
        /// <value>The limit.</value>
        public int? Limit { get; set; }

        /// <summary>
        /// Gets or sets the content types.
        /// </summary>
        /// <value>The content types.</value>
        public ChannelMediaContentType[] ContentTypes { get; set; }

        /// <summary>
        /// Gets or sets the extra types.
        /// </summary>
        /// <value>The extra types.</value>
        public ExtraType[] ExtraTypes { get; set; }
        public TrailerType[] TrailerTypes { get; set; }

        public AllChannelMediaQuery()
        {
            ChannelIds = new string[] { };

            ContentTypes = new ChannelMediaContentType[] { };
            ExtraTypes = new ExtraType[] { };
            TrailerTypes = new TrailerType[] { };

            Filters = new ItemFilter[] { };
            Fields = new ItemFields[] { };
        }

        public ItemFilter[] Filters { get; set; }
        public ItemFields[] Fields { get; set; }
    }
}
