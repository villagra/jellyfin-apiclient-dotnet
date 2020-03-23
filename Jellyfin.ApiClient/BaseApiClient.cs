using Jellyfin.ApiClient.Model;
using Jellyfin.ApiClient.Model.Dto;
using Jellyfin.ApiClient.Model.Querying;
using Jellyfin.ApiClient.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;

namespace Jellyfin.ApiClient
{
    /// <summary>
    /// Provides api methods that are usable on all platforms
    /// </summary>
    public abstract class BaseApiClient : IDisposable
    {
        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        internal ILogger Logger { get; private set; }

        /// <summary>
        /// Gets the json serializer.
        /// </summary>
        /// <value>The json serializer.</value>
        public IJsonSerializer JsonSerializer { get; set; }

        public IDevice Device { get; private set; }

        /// <summary>
        ///  If specified this will be used as a default when an explicit value is not specified.
        /// </summary>
        public int? ImageQuality { get; set; }

        protected BaseApiClient(ILogger logger, IJsonSerializer jsonSerializer, Uri serverAddress, string clientName, IDevice device, string applicationVersion)
        {
            if (serverAddress == null)
            {
                throw new ArgumentNullException(nameof(serverAddress));
            }

            if (jsonSerializer == null)
            {
                throw new ArgumentNullException(nameof(jsonSerializer));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            ClientName = clientName;
            Device = device;
            ApplicationVersion = applicationVersion;
            ServerAddress = serverAddress;
        }

        protected BaseApiClient(ILogger logger, IJsonSerializer jsonSerializer, Uri serverAddress, string accessToken)
        {
            if (serverAddress == null)
            {
                throw new ArgumentNullException("serverAddress");
            }

            if (jsonSerializer == null)
            {
                throw new ArgumentNullException("jsonSerializer");
            }

            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            AccessToken = accessToken;
            ServerAddress = serverAddress;
        }

        /// <summary>
        /// Gets the name of the server host.
        /// </summary>
        /// <value>The name of the server host.</value>
        public Uri ServerAddress { get; protected set; }

        /// <summary>
        /// Changes the server location.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="keepExistingAuth"></param>
        public void ChangeServerLocation(Uri address, bool keepExistingAuth = false)
        {
            ServerAddress = address;

            if (!keepExistingAuth)
            {
                SetAuthenticationInfo(null, Guid.Empty);
            }
        }

        /// <summary>
        /// Gets or sets the type of the client.
        /// </summary>
        /// <value>The type of the client.</value>
        public string ClientName { get; set; }

        /// <summary>
        /// Gets or sets the name of the device.
        /// </summary>
        /// <value>The name of the device.</value>
        public string DeviceName
        {
            get { return Device?.DeviceName; }
        }


        /// <summary>
        /// Gets or sets the application version.
        /// </summary>
        /// <value>The application version.</value>
        public string ApplicationVersion { get; set; }

        /// <summary>
        /// Gets or sets the device id.
        /// </summary>
        /// <value>The device id.</value>
        public string DeviceId
        {
            get { return Device?.DeviceId; }
        }

        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        /// <value>The access token.</value>
        public string AccessToken { get; private set; }

        /// <summary>
        /// Gets or sets the current user id.
        /// </summary>
        /// <value>The current user id.</value>
        public Guid CurrentUserId { get; private set; }

        /// <summary>
        /// Gets the current api url based on hostname and port.
        /// </summary>
        /// <value>The API URL.</value>
        public Uri ApiUrl
        {
            get
            {
                return new Uri(ServerAddress, "/emby");
            }
        }

        /// <summary>
        /// Gets the name of the authorization scheme.
        /// </summary>
        /// <value>The name of the authorization scheme.</value>
        protected string AuthorizationScheme
        {
            get { return "MediaBrowser"; }
        }

        /// <summary>
        /// Gets the authorization header parameter.
        /// </summary>
        /// <value>The authorization header parameter.</value>
        protected string AuthorizationParameter
        {
            get
            {
                if (string.IsNullOrEmpty(ClientName) && string.IsNullOrEmpty(DeviceId) && string.IsNullOrEmpty(DeviceName))
                {
                    return string.Empty;
                }

                var header = string.Format("Client=\"{0}\", DeviceId=\"{1}\", Device=\"{2}\", Version=\"{3}\"", ClientName, DeviceId, DeviceName, ApplicationVersion);

                if (CurrentUserId != Guid.Empty)
                {
                    header += string.Format(", UserId=\"{0}\"", CurrentUserId);
                }

                return header;
            }
        }

        /// <summary>
        /// Gets the API URL.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">handler</exception>
        public Uri GetApiUrl(Uri handler)
        {
            return GetApiUrl(handler, new NameValueCollection());
        }

        public void SetAuthenticationInfo(string accessToken, Guid userId)
        {
            CurrentUserId = userId;
            AccessToken = accessToken;
            ResetHttpHeaders();
        }

        public void ClearAuthenticationInfo()
        {
            CurrentUserId = Guid.Empty;
            AccessToken = null;
            ResetHttpHeaders();
        }

        public void SetAuthenticationInfo(string accessToken)
        {
            CurrentUserId = Guid.Empty;
            AccessToken = accessToken;
            ResetHttpHeaders();
        }

        protected void ResetHttpHeaders()
        {
            HttpHeaders.SetAccessToken(AccessToken);

            var authValue = AuthorizationParameter;

            if (string.IsNullOrEmpty(authValue))
            {
                ClearHttpRequestHeader("Authorization");
                SetAuthorizationHttpRequestHeader(null, null);
            }
            else
            {
                SetAuthorizationHttpRequestHeader(AuthorizationScheme, authValue);
            }
        }

        protected readonly HttpHeaders HttpHeaders = new HttpHeaders();
        private void SetAuthorizationHttpRequestHeader(string scheme, string parameter)
        {
            HttpHeaders.AuthorizationScheme = scheme;
            HttpHeaders.AuthorizationParameter = parameter;
        }

        private void ClearHttpRequestHeader(string name)
        {
            HttpHeaders.Remove(name);
        }

        /// <summary>
        /// Gets the API URL.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="queryString">The query string.</param>
        /// <returns>System.Uri.</returns>
        /// <exception cref="System.ArgumentNullException">handler</exception>
        protected Uri GetApiUrl(Uri handler, NameValueCollection queryString)
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            var uriBuilder = new UriBuilder(new Uri(ApiUrl, handler))
            {
                Query = queryString.ToQueryString(),
            };

            return uriBuilder.Uri;
        }

        public Uri GetSubtitleUrl(SubtitleDownloadOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }
            if (string.IsNullOrEmpty(options.MediaSourceId))
            {
                throw new ArgumentNullException("options");
            }
            if (string.IsNullOrEmpty(options.ItemId))
            {
                throw new ArgumentNullException("options");
            }
            if (string.IsNullOrEmpty(options.Format))
            {
                throw new ArgumentNullException("options");
            }

            return GetApiUrl(new Uri("Videos/" + options.ItemId + "/" + options.MediaSourceId + "/Subtitles/" + options.StreamIndex + "/Stream." + options.Format, UriKind.Relative));
        }

        /// <summary>
        /// Creates a url to return a list of items
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">query</exception>
        protected Uri GetItemListUrl(ItemQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var dict = new NameValueCollection { };

            dict.AddIfNotNullOrEmpty("ParentId", query.ParentId);

            dict.AddIfNotNull("StartIndex", query.StartIndex);

            dict.AddIfNotNull("Limit", query.Limit);

            dict.AddIfNotNull("SortBy", query.SortBy);

            if (!query.EnableTotalRecordCount)
            {
                dict.Add("EnableTotalRecordCount", query.EnableTotalRecordCount);
            }

            if (query.SortOrder.HasValue)
            {
                dict["sortOrder"] = query.SortOrder.ToString();
            }

            if (query.SeriesStatuses != null)
            {
                dict.Add("SeriesStatuses", query.SeriesStatuses.Select(f => f.ToString()));
            }

            if (query.Fields != null)
            {
                dict.Add("fields", query.Fields.Select(f => f.ToString()));
            }
            if (query.Filters != null)
            {
                dict.Add("Filters", query.Filters.Select(f => f.ToString()));
            }
            if (query.ImageTypes != null)
            {
                dict.Add("ImageTypes", query.ImageTypes.Select(f => f.ToString()));
            }

            dict.AddIfNotNull("Is3D", query.Is3D);
            if (query.VideoTypes != null)
            {
                dict.Add("VideoTypes", query.VideoTypes.Select(f => f.ToString()));
            }
            if (query.AirDays != null)
            {
                dict.Add("AirDays", query.AirDays.Select(f => f.ToString()));
            }

            dict.AddIfNotNullOrEmpty("MinOfficialRating", query.MinOfficialRating);
            dict.AddIfNotNullOrEmpty("MaxOfficialRating", query.MaxOfficialRating);

            dict.Add("recursive", query.Recursive);

            dict.AddIfNotNull("MinIndexNumber", query.MinIndexNumber);

            dict.AddIfNotNull("EnableImages", query.EnableImages);
            if (query.EnableImageTypes != null)
            {
                dict.Add("EnableImageTypes", query.EnableImageTypes.Select(f => f.ToString()));
            }
            dict.AddIfNotNull("ImageTypeLimit", query.ImageTypeLimit);
            dict.AddIfNotNull("CollapseBoxSetItems", query.CollapseBoxSetItems);
            dict.AddIfNotNull("MediaTypes", query.MediaTypes);
            dict.AddIfNotNull("Genres", query.Genres, "|");
            dict.AddIfNotNull("Ids", query.Ids);
            dict.AddIfNotNull("StudioIds", query.StudioIds, "|");
            dict.AddIfNotNull("ExcludeItemTypes", query.ExcludeItemTypes);
            dict.AddIfNotNull("IncludeItemTypes", query.IncludeItemTypes);
            dict.AddIfNotNull("ArtistIds", query.ArtistIds);

            dict.AddIfNotNull("IsPlayed", query.IsPlayed);
            dict.AddIfNotNull("IsInBoxSet", query.IsInBoxSet);

            dict.AddIfNotNull("PersonIds", query.PersonIds);
            dict.AddIfNotNull("PersonTypes", query.PersonTypes);

            dict.AddIfNotNull("Years", query.Years);

            dict.AddIfNotNull("ParentIndexNumber", query.ParentIndexNumber);
            dict.AddIfNotNull("IsHD", query.IsHD);
            dict.AddIfNotNull("HasParentalRating", query.HasParentalRating);

            dict.AddIfNotNullOrEmpty("SearchTerm", query.SearchTerm);

            dict.AddIfNotNull("MinCriticRating", query.MinCriticRating);
            dict.AddIfNotNull("MinCommunityRating", query.MinCommunityRating);

            dict.AddIfNotNull("MinPlayers", query.MinPlayers);
            dict.AddIfNotNull("MaxPlayers", query.MaxPlayers);
            dict.AddIfNotNullOrEmpty("NameStartsWithOrGreater", query.NameStartsWithOrGreater);
            dict.AddIfNotNullOrEmpty("AlbumArtistStartsWithOrGreater", query.AlbumArtistStartsWithOrGreater);

            if (query.LocationTypes != null && query.LocationTypes.Length > 0)
            {
                dict.Add("LocationTypes", query.LocationTypes.Select(f => f.ToString()));
            }
            if (query.ExcludeLocationTypes != null && query.ExcludeLocationTypes.Length > 0)
            {
                dict.Add("ExcludeLocationTypes", query.ExcludeLocationTypes.Select(f => f.ToString()));
            }

            dict.AddIfNotNull("IsMissing", query.IsMissing);
            dict.AddIfNotNull("IsUnaired", query.IsUnaired);
            dict.AddIfNotNull("IsVirtualUnaired", query.IsVirtualUnaired);

            dict.AddIfNotNull("AiredDuringSeason", query.AiredDuringSeason);

            return GetApiUrl(new Uri("Users/" + query.UserId + "/Items", UriKind.Relative), dict);
        }

        /// <summary>
        /// Gets the next up.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">query</exception>
        protected Uri GetNextUpUrl(NextUpQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var dict = new NameValueCollection { };

            if (query.Fields != null)
            {
                dict.Add("fields", query.Fields.Select(f => f.ToString()));
            }

            dict.Add("ParentId", query.ParentId);

            dict.AddIfNotNull("Limit", query.Limit);

            dict.AddIfNotNull("StartIndex", query.StartIndex);

            dict.AddIfNotNullOrEmpty("SeriesId", query.SeriesId);

            dict.Add("UserId", query.UserId.ToString("N", CultureInfo.InvariantCulture));

            dict.AddIfNotNull("EnableImages", query.EnableImages);
            if (query.EnableImageTypes != null)
            {
                dict.Add("EnableImageTypes", query.EnableImageTypes.Select(f => f.ToString()));
            }
            dict.AddIfNotNull("ImageTypeLimit", query.ImageTypeLimit);
            
            return GetApiUrl(new Uri("Shows/NextUp", UriKind.Relative), dict);
        }

        /// <summary>
        /// Gets the similar item list URL.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="type">The type.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// query
        /// or
        /// type
        /// </exception>
        protected Uri GetSimilarItemListUrl(SimilarItemsQuery query, string type)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }
            if (string.IsNullOrEmpty(type))
            {
                throw new ArgumentNullException("type");
            }

            var dict = new NameValueCollection { };

            dict.AddIfNotNull("Limit", query.Limit);
            dict.AddIfNotNullOrEmpty("UserId", query.UserId);

            if (query.Fields != null)
            {
                dict.Add("fields", query.Fields.Select(f => f.ToString()));
            }

            if (string.IsNullOrEmpty(query.Id))
            {
                throw new ArgumentNullException("query");
            }

            return GetApiUrl(new Uri(type + "/" + query.Id + "/Similar", UriKind.Relative), dict);
        }

        /// <summary>
        /// Gets the instant mix URL.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="type">The type.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// query
        /// or
        /// type
        /// </exception>
        protected Uri GetInstantMixUrl(SimilarItemsQuery query, string type)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }
            if (string.IsNullOrEmpty(type))
            {
                throw new ArgumentNullException("type");
            }

            var dict = new NameValueCollection { };

            dict.AddIfNotNull("Limit", query.Limit);
            dict.AddIfNotNullOrEmpty("UserId", query.UserId);

            if (query.Fields != null)
            {
                dict.Add("fields", query.Fields.Select(f => f.ToString()));
            }

            if (string.IsNullOrEmpty(query.Id))
            {
                throw new ArgumentNullException("query");
            }

            return GetApiUrl(new Uri(type + "/" + query.Id + "/InstantMix", UriKind.Relative), dict);
        }

        /// <summary>
        /// Gets the item by name list URL.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="query">The query.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">query</exception>
        protected Uri GetItemByNameListUrl(string type, ItemsByNameQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var dict = new NameValueCollection { };

            dict.AddIfNotNullOrEmpty("ParentId", query.ParentId);

            dict.Add("UserId", query.UserId);
            dict.AddIfNotNull("StartIndex", query.StartIndex);

            dict.AddIfNotNull("Limit", query.Limit);

            dict.AddIfNotNull("SortBy", query.SortBy);

            if (query.SortOrder.HasValue)
            {
                dict["sortOrder"] = query.SortOrder.ToString();
            }

            dict.AddIfNotNull("IsPlayed", query.IsPlayed);

            if (query.Fields != null)
            {
                dict.Add("fields", query.Fields.Select(f => f.ToString()));
            }

            if (query.Filters != null)
            {
                dict.Add("Filters", query.Filters.Select(f => f.ToString()));
            }

            if (query.ImageTypes != null)
            {
                dict.Add("ImageTypes", query.ImageTypes.Select(f => f.ToString()));
            }

            dict.Add("recursive", query.Recursive);

            dict.AddIfNotNull("MediaTypes", query.MediaTypes);
            dict.AddIfNotNull("ExcludeItemTypes", query.ExcludeItemTypes);
            dict.AddIfNotNull("IncludeItemTypes", query.IncludeItemTypes);

            dict.AddIfNotNullOrEmpty("NameLessThan", query.NameLessThan);
            dict.AddIfNotNullOrEmpty("NameStartsWithOrGreater", query.NameStartsWithOrGreater);

            dict.AddIfNotNull("EnableImages", query.EnableImages);
            if (query.EnableImageTypes != null)
            {
                dict.Add("EnableImageTypes", query.EnableImageTypes.Select(f => f.ToString()));
            }
            dict.AddIfNotNull("ImageTypeLimit", query.ImageTypeLimit);
            
            return GetApiUrl(new Uri(type, UriKind.Relative), dict);
        }

        /// <summary>
        /// Gets the image URL.
        /// </summary>
        /// <param name="baseUrl">The base URL.</param>
        /// <param name="options">The options.</param>
        /// <param name="queryParams">The query params.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">options</exception>
        private Uri GetImageUrl(Uri baseUrl, ImageOptions options, NameValueCollection queryParams)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            if (queryParams == null)
            {
                throw new ArgumentNullException("queryParams");
            }

            if (options.ImageIndex.HasValue)
            {
                baseUrl = new Uri(baseUrl, "/" + options.ImageIndex.Value);
            }

            queryParams.AddIfNotNull("Width", options.Width);
            queryParams.AddIfNotNull("Height", options.Height);
            queryParams.AddIfNotNull("MaxWidth", options.MaxWidth);
            queryParams.AddIfNotNull("MaxHeight", options.MaxHeight);
            queryParams.AddIfNotNull("Quality", options.Quality ?? ImageQuality);

            queryParams.AddIfNotNullOrEmpty("Tag", options.Tag);

            queryParams.AddIfNotNull("CropWhitespace", options.CropWhitespace);
            queryParams.Add("EnableImageEnhancers", options.EnableImageEnhancers);

            if (options.Format.HasValue)
            {
                queryParams.Add("Format", options.Format.ToString());
            }

            if (options.AddPlayedIndicator)
            {
                queryParams.Add("AddPlayedIndicator", true);
            }
            queryParams.AddIfNotNull("PercentPlayed", options.PercentPlayed);
            queryParams.AddIfNotNullOrEmpty("BackgroundColor", options.BackgroundColor);

            return GetApiUrl(baseUrl, queryParams);
        }

        /// <summary>
        /// Gets the image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public Uri GetImageUrl(BaseItemDto item, ImageOptions options)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            options.Tag = GetImageTag(item, options);

            return GetImageUrl(item, options);
        }

        public Uri GetImageUrl(ChannelInfoDto item, ImageOptions options)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            options.Tag = item.ImageTags[options.ImageType];

            return GetImageUrl(item.Id, options);
        }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="itemId">The Id of the item</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">itemId</exception>
        public Uri GetImageUrl(string itemId, ImageOptions options)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException("itemId");
            }

            var url = new Uri("Items/" + itemId + "/Images/" + options.ImageType, UriKind.Relative);

            return GetImageUrl(url, options, new NameValueCollection());
        }

        /// <summary>
        /// Gets the user image URL.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        public Uri GetUserImageUrl(UserDto user, ImageOptions options)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            options.Tag = user.PrimaryImageTag;

            return GetUserImageUrl(user.Id, options);
        }

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="userId">The Id of the user</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">userId</exception>
        public Uri GetUserImageUrl(Guid userId, ImageOptions options)
        {
            if (userId.Equals(Guid.Empty))
            {
                throw new ArgumentNullException("userId");
            }

            var url = new Uri("Users/" + userId.ToString("N", CultureInfo.InvariantCulture) + "/Images/" + options.ImageType, UriKind.Relative);

            return GetImageUrl(url, options, new NameValueCollection());
        }

        /// <summary>
        /// Gets the person image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public Uri GetPersonImageUrl(BaseItemPerson item, ImageOptions options)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            options.Tag = item.PrimaryImageTag;

            return GetImageUrl(item.Id, options);
        }

        /// <summary>
        /// Gets the image tag.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        private string GetImageTag(BaseItemDto item, ImageOptions options)
        {
            if (options.ImageType == ImageType.Backdrop)
            {
                return item.BackdropImageTags[options.ImageIndex ?? 0];
            }

            if (options.ImageType == ImageType.Screenshot)
            {
                return item.ScreenshotImageTags[options.ImageIndex ?? 0];
            }

            if (options.ImageType == ImageType.Chapter)
            {
                return item.Chapters[options.ImageIndex ?? 0].ImageTag;
            }

            return item.ImageTags[options.ImageType];
        }

        /// <summary>
        /// This is a helper to get a list of backdrop url's from a given ApiBaseItemWrapper. If the actual item does not have any backdrops it will return backdrops from the first parent that does.
        /// </summary>
        /// <param name="item">A given item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String[][].</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public Uri[] GetBackdropImageUrls(BaseItemDto item, ImageOptions options)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            options.ImageType = ImageType.Backdrop;

            string backdropItemId;
            string[] backdropImageTags;

            if (item.BackdropImageTags == null || item.BackdropImageTags.Length == 0)
            {
                backdropItemId = item.ParentBackdropItemId;
                backdropImageTags = item.ParentBackdropImageTags;
            }
            else
            {
                backdropItemId = item.Id.ToString("N", CultureInfo.InvariantCulture);
                backdropImageTags = item.BackdropImageTags;
            }

            if (string.IsNullOrEmpty(backdropItemId))
            {
                return new Uri[] { };
            }

            var files = new Uri[backdropImageTags.Length];

            for (var i = 0; i < backdropImageTags.Length; i++)
            {
                options.ImageIndex = i;
                options.Tag = backdropImageTags[i];

                files[i] = GetImageUrl(backdropItemId, options);
            }

            return files;
        }

        /// <summary>
        /// This is a helper to get the logo image url from a given ApiBaseItemWrapper. If the actual item does not have a logo, it will return the logo from the first parent that does, or null.
        /// </summary>
        /// <param name="item">A given item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public Uri GetLogoImageUrl(BaseItemDto item, ImageOptions options)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            options.ImageType = ImageType.Logo;

            var logoItemId = HasLogo(item) ? item.Id.ToString("N", CultureInfo.InvariantCulture) : item.ParentLogoItemId;
            var imageTag = HasLogo(item) ? item.ImageTags[ImageType.Logo] : item.ParentLogoImageTag;

            if (!string.IsNullOrEmpty(logoItemId))
            {
                options.Tag = imageTag;

                return GetImageUrl(logoItemId, options);
            }

            return null;
        }

        public Uri GetThumbImageUrl(BaseItemDto item, ImageOptions options)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            options.ImageType = ImageType.Thumb;

            var itemId = HasThumb(item) ? item.Id.ToString("N", CultureInfo.InvariantCulture) : item.SeriesThumbImageTag != null ? item.SeriesId.ToString("N", CultureInfo.InvariantCulture) : item.ParentThumbItemId;
            var imageTag = HasThumb(item) ? item.ImageTags[ImageType.Thumb] : item.SeriesThumbImageTag ?? item.ParentThumbImageTag;

            if (!string.IsNullOrEmpty(itemId))
            {
                options.Tag = imageTag;

                return GetImageUrl(itemId, options);
            }

            return null;
        }

        /// <summary>
        /// This is a helper to get the art image url from a given BaseItemDto. If the actual item does not have a logo, it will return the logo from the first parent that does, or null.
        /// </summary>
        /// <param name="item">A given item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public Uri GetArtImageUrl(BaseItemDto item, ImageOptions options)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            options.ImageType = ImageType.Art;

            var artItemId = HasArtImage(item) ? item.Id.ToString("N", CultureInfo.InvariantCulture) : item.ParentArtItemId;
            var imageTag = HasArtImage(item) ? item.ImageTags[ImageType.Art] : item.ParentArtImageTag;

            if (!string.IsNullOrEmpty(artItemId))
            {
                options.Tag = imageTag;

                return GetImageUrl(artItemId, options);
            }

            return null;
        }

        public bool HasArtImage(BaseItemDto item)
        {
            return item.ImageTags != null && item.ImageTags.ContainsKey(ImageType.Art);
        }
        public bool HasLogo(BaseItemDto item)
        {
            return item.ImageTags != null && item.ImageTags.ContainsKey(ImageType.Logo);
        }
        public bool HasThumb(BaseItemDto item)
        {
            return item.ImageTags != null && item.ImageTags.ContainsKey(ImageType.Thumb);
        }

        /// <summary>
        /// Deserializes from stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream">The stream.</param>
        /// <returns>``0.</returns>
        protected T DeserializeFromStream<T>(Stream stream)
            where T : class
        {
            return (T)DeserializeFromStream(stream, typeof(T));
        }

        /// <summary>
        /// Deserializes from stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="type">The type.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        protected object DeserializeFromStream(Stream stream, Type type)
        {
            return JsonSerializer.DeserializeFromStream(stream, type);
        }

        /// <summary>
        /// Serializers to json.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.String.</returns>
        protected string SerializeToJson(object obj)
        {
            return JsonSerializer.SerializeToString(obj);
        }

        /// <summary>
        /// Adds the data format.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>System.Uri.</returns>
        protected Uri AddDataFormat(Uri url)
        {
            const string format = "json";

            UriBuilder uriBuilder = new UriBuilder(url);
            NameValueCollection query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["format"] = format;
            uriBuilder.Query = query.ToQueryString();
            return uriBuilder.Uri;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {

        }
    }
}
