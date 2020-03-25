﻿using Jellyfin.ApiClient.Model;
using Jellyfin.ApiClient.Model.Dto;
using Jellyfin.ApiClient.Model.Notifications;
using Jellyfin.ApiClient.Model.Querying;
using Jellyfin.ApiClient.Net;
using Jellyfin.ApiClient.Extensions;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Devices;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Playlists;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Search;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Diagnostics;

namespace Jellyfin.ApiClient
{
    /// <summary>
    /// Provides api methods centered around an HttpClient
    /// </summary>
    public partial class ApiClient : BaseApiClient, IApiClient
    {
        public event EventHandler<GenericEventArgs<RemoteLogoutReason>> RemoteLoggedOut;
        public event EventHandler<GenericEventArgs<AuthenticationResult>> Authenticated;

        protected IAsyncHttpClient HttpClient { get; private set; }
        internal ServerInfo ServerInfo { get; set; }
        private INetworkConnection NetworkConnection { get; set; }
        private readonly SemaphoreSlim _validateConnectionSemaphore = new SemaphoreSlim(1, 1);
        private DateTime _lastConnectionValidationTime = DateTime.MinValue;

        public ApiClient(ILogger logger, Uri serverAddress, string accessToken)
            : base(logger, new NewtonsoftJsonSerializer(), serverAddress, accessToken)
        {
            CreateHttpClient(logger);
            ResetHttpHeaders();
        }

        public ApiClient(ILogger logger, Uri serverAddress, string clientName, IDevice device, string applicationVersion)
            : base(logger, new NewtonsoftJsonSerializer(), serverAddress, clientName, device, applicationVersion)
        {
            CreateHttpClient(logger);
            ResetHttpHeaders();
        }

        private void CreateHttpClient(ILogger logger)
        {
            HttpClient = AsyncHttpClientFactory.Create(logger);
            HttpClient.HttpResponseReceived += HttpClient_HttpResponseReceived;
        }

        #region validated 

        /// <summary>
        /// Queries for items
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        /// <exception cref="System.ArgumentNullException">query</exception>
        public async Task<QueryResult<BaseItemDto>> GetItemsAsync(ItemQuery query, CancellationToken cancellationToken = default)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var url = GetItemListUrl(query);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(stream))
                {
                    Debug.WriteLine(reader.ReadToEnd());
                }
                return DeserializeFromStream<QueryResult<BaseItemDto>>(stream);
            }
        }

        #endregion

        private void HttpClient_HttpResponseReceived(object sender, HttpWebResponse e)
        {
            if (e.StatusCode == HttpStatusCode.Unauthorized)
            {
                RemoteLoggedOut?.Invoke(this, new GenericEventArgs<RemoteLogoutReason>());
            }
        }           

        private async Task<Stream> SendAsync(HttpRequest request, bool enableFailover = true)
        {
            // If not using automatic connection, execute the request directly
            if (NetworkConnection == null || !enableFailover)
            {
                return await HttpClient.SendAsync(request).ConfigureAwait(false);
            }

            var originalRequestTime = DateTime.UtcNow;
            Exception timeoutException;

            try
            {
                return await HttpClient.SendAsync(request).ConfigureAwait(false);
            }
            catch (HttpException ex)
            {
                if (!ex.IsTimedOut)
                {
                    throw;
                }

                timeoutException = ex;
            }

            try
            {
                await ValidateConnection(originalRequestTime, request.CancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Unable to re-establish connection with the server. 
                // Throw the original exception
                throw timeoutException;
            }

            request.Url = ReplaceServerAddress(request.Url);

            return await HttpClient.SendAsync(request).ConfigureAwait(false);
        }

        private async Task ValidateConnection(DateTime originalRequestTime, CancellationToken cancellationToken)
        {
            await _validateConnectionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (originalRequestTime > _lastConnectionValidationTime)
                {
                    await ValidateConnectionInternal(cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                _validateConnectionSemaphore.Release();
            }
        }

        private async Task ValidateConnectionInternal(CancellationToken cancellationToken)
        {
            Logger.LogDebug("Connection to server dropped. Attempting to reconnect.");

            const int maxWaitMs = 10000;
            const int waitIntervalMs = 100;
            var totalWaitMs = 0;
            var networkStatus = NetworkConnection.GetNetworkStatus();

            while (!networkStatus.IsNetworkAvailable)
            {
                if (totalWaitMs >= maxWaitMs)
                {
                    throw new Exception("Network unavailable.");
                }

                await Task.Delay(waitIntervalMs, cancellationToken).ConfigureAwait(false);

                totalWaitMs += waitIntervalMs;
                networkStatus = NetworkConnection.GetNetworkStatus();
            }

            var urlList = new List<Uri>
			{
				ServerInfo.Address,
			};

            if (!networkStatus.GetIsAnyLocalNetworkAvailable())
            {
                urlList.Reverse();
            }

            if (ServerInfo.Address != null)
            {
                urlList.Insert(0, ServerInfo.Address);
            }

            foreach (var url in urlList)
            {
                var connected = await TryConnect(url, cancellationToken).ConfigureAwait(false);

                if (connected)
                {
                    break;
                }
            }

            _lastConnectionValidationTime = DateTime.UtcNow;
        }

        private async Task<bool> TryConnect(Uri baseUrl, CancellationToken cancellationToken)
        {
            var fullUrl = new Uri(baseUrl, new Uri("/system/info/public", UriKind.Relative));

            fullUrl = AddDataFormat(fullUrl);

            var request = new HttpRequest
            {
                Url = fullUrl,
                RequestHeaders = HttpHeaders,
                CancellationToken = cancellationToken,
                Method = "GET"
            };

            try
            {
                using (var stream = await HttpClient.SendAsync(request).ConfigureAwait(false))
                {
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private Uri ReplaceServerAddress(Uri url)
        {
            var baseUrl = ServerInfo.Address;

            var index = url.ToString().IndexOf("/mediabrowser", StringComparison.OrdinalIgnoreCase);

            if (index != -1)
            {
                return new Uri(baseUrl, url.ToString().Substring(index));
            }

            return url;
        }

        public void EnableAutomaticNetworking(ServerInfo info, INetworkConnection networkConnection)
        {
            NetworkConnection = networkConnection;
            ServerInfo = info;
            ServerAddress = info.Address;
        }

        public Task<Stream> GetStream(Uri url, CancellationToken cancellationToken = default)
        {
            return SendAsync(new HttpRequest
            {
                CancellationToken = cancellationToken,
                Method = "GET",
                RequestHeaders = HttpHeaders,
                Url = url
            });
        }

        public Task<HttpWebResponse> GetResponse(Uri url, CancellationToken cancellationToken = default)
        {
            return HttpClient.GetResponse(new HttpRequest
            {
                CancellationToken = cancellationToken,
                Method = "GET",
                RequestHeaders = HttpHeaders,
                Url = url
            });
        }

        /// <summary>
        /// Gets an image stream based on a url
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Stream}.</returns>
        /// <exception cref="System.ArgumentNullException">url</exception>
        public Task<Stream> GetImageStreamAsync(Uri url, CancellationToken cancellationToken = default)
        {
            if (url == null)
            {
                throw new ArgumentNullException("url");
            }

            return GetStream(url, cancellationToken);
        }

        /// <summary>
        /// Gets a BaseItem
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        /// <exception cref="System.ArgumentNullException">id</exception>
        public async Task<BaseItemDto> GetItemAsync(string id, string userId)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException("userId");
            }

            var url = GetApiUrl(new Uri("Users/" + userId + "/Items/" + id, UriKind.Relative));

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<BaseItemDto>(stream);
            }
        }

        /// <summary>
        /// Gets the intros async.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{System.String[]}.</returns>
        /// <exception cref="System.ArgumentNullException">id</exception>
        public async Task<QueryResult<BaseItemDto>> GetIntrosAsync(string itemId, string userId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException("itemId");
            }

            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException("userId");
            }

            var url = GetApiUrl(new Uri("Users/" + userId + "/Items/" + itemId + "/Intros", UriKind.Relative));
            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<QueryResult<BaseItemDto>>(stream);
            }
        }

        /// <summary>
        /// Gets the item counts async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemCounts}.</returns>
        /// <exception cref="System.ArgumentNullException">query</exception>
        public async Task<ItemCounts> GetItemCountsAsync(ItemCountsQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var dict = new NameValueCollection();

            dict.AddIfNotNullOrEmpty("UserId", query.UserId);
            dict.AddIfNotNull("IsFavorite", query.IsFavorite);

            var url = GetApiUrl(new Uri("Items/Counts", UriKind.Relative), dict);

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<ItemCounts>(stream);
            }
        }

        /// <summary>
        /// Gets a BaseItem
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        /// <exception cref="System.ArgumentNullException">userId</exception>
        public async Task<BaseItemDto> GetRootFolderAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException("userId");
            }

            var url = GetApiUrl(new Uri("Users/" + userId + "/Items/Root", UriKind.Relative));

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<BaseItemDto>(stream);
            }
        }

        /// <summary>
        /// Gets the users async.
        /// </summary>
        /// <returns>Task{UserDto[]}.</returns>
        public async Task<UserDto[]> GetUsersAsync(UserQuery query)
        {
            var queryString = new NameValueCollection();

            queryString.AddIfNotNull("IsDisabled", query.IsDisabled);
            queryString.AddIfNotNull("IsHidden", query.IsHidden);

            var url = GetApiUrl(new Uri("Users", UriKind.Relative), queryString);

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<UserDto[]>(stream);
            }
        }

        public async Task<UserDto[]> GetPublicUsersAsync(CancellationToken cancellationToken = default)
        {
            var url = GetApiUrl(new Uri("Users/Public", UriKind.Relative));

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<UserDto[]>(stream);
            }
        }

        /// <summary>
        /// Gets active client sessions.
        /// </summary>
        /// <returns>Task{SessionInfoDto[]}.</returns>
        public async Task<SessionInfoDto[]> GetClientSessionsAsync(SessionQuery query)
        {
            var queryString = new NameValueCollection();

            queryString.AddIfNotNullOrEmpty("ControllableByUserId", query.ControllableByUserId);

            var url = GetApiUrl(new Uri("Sessions", UriKind.Relative), queryString);

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<SessionInfoDto[]>(stream);
            }
        }

        /// <summary>
        /// Gets the next up async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        /// <exception cref="System.ArgumentNullException">query</exception>
        public async Task<QueryResult<BaseItemDto>> GetNextUpEpisodesAsync(NextUpQuery query, CancellationToken cancellationToken = default)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var url = GetNextUpUrl(query);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<QueryResult<BaseItemDto>>(stream);
            }
        }

        public async Task<QueryResult<BaseItemDto>> GetUpcomingEpisodesAsync(UpcomingEpisodesQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var dict = new NameValueCollection();

            if (query.Fields != null)
            {
                dict.Add("fields", query.Fields.Select(f => f.ToString()));
            }

            dict.Add("ParentId", query.ParentId);

            dict.AddIfNotNull("Limit", query.Limit);

            dict.AddIfNotNull("StartIndex", query.StartIndex);

            dict.Add("UserId", query.UserId);

            dict.AddIfNotNull("EnableImages", query.EnableImages);
            if (query.EnableImageTypes != null)
            {
                dict.Add("EnableImageTypes", query.EnableImageTypes.Select(f => f.ToString()));
            }
            dict.AddIfNotNull("ImageTypeLimit", query.ImageTypeLimit);

            var url = GetApiUrl(new Uri("Shows/Upcoming", UriKind.Relative), dict);

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<QueryResult<BaseItemDto>>(stream);
            }
        }

        public async Task<QueryResult<BaseItemDto>> GetEpisodesAsync(EpisodeQuery query, CancellationToken cancellationToken = default)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var dict = new NameValueCollection();

            dict.AddIfNotNull("StartIndex", query.StartIndex);
            dict.AddIfNotNull("Limit", query.Limit);

            dict.AddIfNotNullOrEmpty("StartItemId", query.StartItemId);
            dict.AddIfNotNull("Season", query.SeasonNumber);
            dict.AddIfNotNullOrEmpty("UserId", query.UserId);

            dict.AddIfNotNullOrEmpty("SeasonId", query.SeasonId);

            if (query.Fields != null)
            {
                dict.Add("Fields", query.Fields.Select(f => f.ToString()));
            }

            dict.AddIfNotNull("IsMissing", query.IsMissing);
            dict.AddIfNotNull("IsVirtualUnaired", query.IsVirtualUnaired);

            var url = GetApiUrl(new Uri("Shows/" + query.SeriesId + "/Episodes", UriKind.Relative), dict);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<QueryResult<BaseItemDto>>(stream);
            }
        }

        public async Task<QueryResult<BaseItemDto>> GetSeasonsAsync(SeasonQuery query, CancellationToken cancellationToken = default)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var dict = new NameValueCollection();

            dict.AddIfNotNullOrEmpty("UserId", query.UserId);

            if (query.Fields != null)
            {
                dict.Add("Fields", query.Fields.Select(f => f.ToString()));
            }

            dict.AddIfNotNull("IsMissing", query.IsMissing);
            dict.AddIfNotNull("IsVirtualUnaired", query.IsVirtualUnaired);
            dict.AddIfNotNull("IsSpecialSeason", query.IsSpecialSeason);

            var url = GetApiUrl(new Uri("Shows/" + query.SeriesId + "/Seasons", UriKind.Relative), dict);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<QueryResult<BaseItemDto>>(stream);
            }
        }

        /// <summary>
        /// Gets the people async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        /// <exception cref="System.ArgumentNullException">userId</exception>
        public async Task<QueryResult<BaseItemDto>> GetPeopleAsync(PersonsQuery query, CancellationToken cancellationToken = default)
        {
            var url = GetItemByNameListUrl("Persons", query);

            if (query.PersonTypes != null && query.PersonTypes.Length > 0)
            {
                var uriBuilder = new UriBuilder(url);
                var uriQuery = HttpUtility.ParseQueryString(uriBuilder.Query);
                uriQuery["PersonTypes"] = string.Join(",", query.PersonTypes);
                uriBuilder.Query = uriQuery.ToQueryString();
                url = uriBuilder.Uri;
            }

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<QueryResult<BaseItemDto>>(stream);
            }
        }

        /// <summary>
        /// Gets the genres async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        public async Task<QueryResult<BaseItemDto>> GetGenresAsync(ItemsByNameQuery query)
        {
            var url = GetItemByNameListUrl("Genres", query);

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<QueryResult<BaseItemDto>>(stream);
            }
        }

        /// <summary>
        /// Gets the studios async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        public async Task<QueryResult<BaseItemDto>> GetStudiosAsync(ItemsByNameQuery query)
        {
            var url = GetItemByNameListUrl("Studios", query);

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<QueryResult<BaseItemDto>>(stream);
            }
        }

        /// <summary>
        /// Gets the artists.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        /// <exception cref="System.ArgumentNullException">userId</exception>
        public async Task<QueryResult<BaseItemDto>> GetArtistsAsync(ArtistsQuery query)
        {
            var url = GetItemByNameListUrl("Artists", query);

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<QueryResult<BaseItemDto>>(stream);
            }
        }

        /// <summary>
        /// Gets the artists.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        /// <exception cref="System.ArgumentNullException">userId</exception>
        public async Task<QueryResult<BaseItemDto>> GetAlbumArtistsAsync(ArtistsQuery query)
        {
            var url = GetItemByNameListUrl("Artists/AlbumArtists", query);

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<QueryResult<BaseItemDto>>(stream);
            }
        }

        /// <summary>
        /// Restarts the server async.
        /// </summary>
        /// <returns>Task.</returns>
        public Task RestartServerAsync()
        {
            var url = GetApiUrl(new Uri("System/Restart", UriKind.Relative));

            return PostAsync<EmptyRequestResult>(url, new NameValueCollection(), CancellationToken.None);
        }

        /// <summary>
        /// Gets the system status async.
        /// </summary>
        /// <returns>Task{SystemInfo}.</returns>
        public async Task<SystemInfo> GetSystemInfoAsync(CancellationToken cancellationToken = default)
        {
            var url = GetApiUrl(new Uri("System/Info", UriKind.Relative));

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<SystemInfo>(stream);
            }
        }

        /// <summary>
        /// get public system information as an asynchronous operation.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;PublicSystemInfo&gt;.</returns>
        public async Task<PublicSystemInfo> GetPublicSystemInfoAsync(CancellationToken cancellationToken = default)
        {
            var url = GetApiUrl(new Uri("System/Info/Public", UriKind.Relative));

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<PublicSystemInfo>(stream);
            }
        }

        /// <summary>
        /// Gets a list of plugins installed on the server
        /// </summary>
        /// <returns>Task{PluginInfo[]}.</returns>
        public async Task<PluginInfo[]> GetInstalledPluginsAsync()
        {
            var url = GetApiUrl(new Uri("Plugins", UriKind.Relative));

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<PluginInfo[]>(stream);
            }
        }

        /// <summary>
        /// Gets the current server configuration
        /// </summary>
        /// <returns>Task{ServerConfiguration}.</returns>
        public async Task<ServerConfiguration> GetServerConfigurationAsync()
        {
            var url = GetApiUrl(new Uri("System/Configuration", UriKind.Relative));

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<ServerConfiguration>(stream);
            }
        }

        /// <summary>
        /// Gets the scheduled tasks.
        /// </summary>
        /// <returns>Task{TaskInfo[]}.</returns>
        public async Task<TaskInfo[]> GetScheduledTasksAsync()
        {
            var url = GetApiUrl(new Uri("ScheduledTasks", UriKind.Relative));

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<TaskInfo[]>(stream);
            }
        }

        /// <summary>
        /// Gets the scheduled task async.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>Task{TaskInfo}.</returns>
        /// <exception cref="System.ArgumentNullException">id</exception>
        public async Task<TaskInfo> GetScheduledTaskAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            var url = GetApiUrl(new Uri("ScheduledTasks/" + id, UriKind.Relative));

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<TaskInfo>(stream);
            }
        }

        /// <summary>
        /// Gets a user by id
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>Task{UserDto}.</returns>
        /// <exception cref="System.ArgumentNullException">id</exception>
        public async Task<UserDto> GetUserAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            var url = GetApiUrl(new Uri("Users/" + id, UriKind.Relative));

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<UserDto>(stream);
            }
        }

        /// <summary>
        /// Gets the parental ratings async.
        /// </summary>
        /// <returns>Task{List{ParentalRating}}.</returns>
        public async Task<List<ParentalRating>> GetParentalRatingsAsync()
        {
            var url = GetApiUrl(new Uri("Localization/ParentalRatings", UriKind.Relative));

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<List<ParentalRating>>(stream);
            }
        }

        /// <summary>
        /// Gets local trailers for an item
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="itemId">The item id.</param>
        /// <returns>Task{ItemsResult}.</returns>
        /// <exception cref="System.ArgumentNullException">query</exception>
        public async Task<BaseItemDto[]> GetLocalTrailersAsync(string userId, string itemId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException("userId");
            }
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException("itemId");
            }

            var url = GetApiUrl(new Uri("Users/" + userId + "/Items/" + itemId + "/LocalTrailers", UriKind.Relative));

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<BaseItemDto[]>(stream);
            }
        }

        /// <summary>
        /// Gets special features for an item
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="itemId">The item id.</param>
        /// <returns>Task{BaseItemDto[]}.</returns>
        /// <exception cref="System.ArgumentNullException">userId</exception>
        public async Task<BaseItemDto[]> GetSpecialFeaturesAsync(string userId, string itemId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException("userId");
            }
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException("itemId");
            }

            var url = GetApiUrl(new Uri("Users/" + userId + "/Items/" + itemId + "/SpecialFeatures", UriKind.Relative));

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<BaseItemDto[]>(stream);
            }
        }

        /// <summary>
        /// Gets the cultures async.
        /// </summary>
        /// <returns>Task{CultureDto[]}.</returns>
        public async Task<CultureDto[]> GetCulturesAsync()
        {
            var url = GetApiUrl(new Uri("Localization/Cultures", UriKind.Relative));

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<CultureDto[]>(stream);
            }
        }

        /// <summary>
        /// Gets the countries async.
        /// </summary>
        /// <returns>Task{CountryInfo[]}.</returns>
        public async Task<CountryInfo[]> GetCountriesAsync()
        {
            var url = GetApiUrl(new Uri("Localization/Countries", UriKind.Relative));

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<CountryInfo[]>(stream);
            }
        }

        public Task<UserItemDataDto> MarkPlayedAsync(string itemId, string userId, DateTime? datePlayed)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException("itemId");
            }
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException("userId");
            }

            var dict = new NameValueCollection();

            if (datePlayed.HasValue)
            {
                dict.Add("DatePlayed", datePlayed.Value.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture));
            }

            var url = GetApiUrl(new Uri("Users/" + userId + "/PlayedItems/" + itemId, UriKind.Relative), dict);

            return PostAsync<UserItemDataDto>(url, new NameValueCollection(), CancellationToken.None);
        }

        /// <summary>
        /// Marks the unplayed async.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{UserItemDataDto}.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// itemId
        /// or
        /// userId
        /// </exception>
        public Task<UserItemDataDto> MarkUnplayedAsync(string itemId, string userId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException("itemId");
            }
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException("userId");
            }

            var url = GetApiUrl(new Uri("Users/" + userId + "/PlayedItems/" + itemId, UriKind.Relative));

            return DeleteAsync<UserItemDataDto>(url, CancellationToken.None);
        }

        /// <summary>
        /// Updates the favorite status async.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="isFavorite">if set to <c>true</c> [is favorite].</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">itemId</exception>
        public Task<UserItemDataDto> UpdateFavoriteStatusAsync(string itemId, string userId, bool isFavorite)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException("itemId");
            }
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException("userId");
            }

            var url = GetApiUrl(new Uri("Users/" + userId + "/FavoriteItems/" + itemId, UriKind.Relative));

            if (isFavorite)
            {
                return PostAsync<UserItemDataDto>(url, new NameValueCollection(), CancellationToken.None);
            }

            return DeleteAsync<UserItemDataDto>(url, CancellationToken.None);
        }

        /// <summary>
        /// Reports to the server that the user has begun playing an item
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns>Task{UserItemDataDto}.</returns>
        /// <exception cref="System.ArgumentNullException">itemId</exception>
        public Task ReportPlaybackStartAsync(PlaybackStartInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            Logger.LogDebug("ReportPlaybackStart: Item {0}", info.ItemId);

            var url = GetApiUrl(new Uri("Sessions/Playing", UriKind.Relative));

            return PostAsync<PlaybackStartInfo, EmptyRequestResult>(url, info, CancellationToken.None);
        }

        /// <summary>
        /// Reports playback progress to the server
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns>Task{UserItemDataDto}.</returns>
        /// <exception cref="System.ArgumentNullException">itemId</exception>
        public Task ReportPlaybackProgressAsync(PlaybackProgressInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            if (IsWebSocketConnected)
            {
                return SendWebSocketMessage("ReportPlaybackProgress", JsonSerializer.SerializeToString(info));
            }

            var url = GetApiUrl(new Uri("Sessions/Playing/Progress", UriKind.Relative));

            return PostAsync<PlaybackProgressInfo, EmptyRequestResult>(url, info, CancellationToken.None);
        }

        /// <summary>
        /// Reports to the server that the user has stopped playing an item
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns>Task{UserItemDataDto}.</returns>
        /// <exception cref="System.ArgumentNullException">itemId</exception>
        public Task ReportPlaybackStoppedAsync(PlaybackStopInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            var url = GetApiUrl(new Uri("Sessions/Playing/Stopped", UriKind.Relative));

            return PostAsync<PlaybackStopInfo, EmptyRequestResult>(url, info, CancellationToken.None);
        }

        /// <summary>
        /// Instructs antoher client to browse to a library item.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="itemId">The id of the item to browse to.</param>
        /// <param name="itemName">The name of the item to browse to.</param>
        /// <param name="itemType">The type of the item to browse to.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">sessionId
        /// or
        /// itemId
        /// or
        /// itemName
        /// or
        /// itemType</exception>
        public Task SendBrowseCommandAsync(string sessionId, string itemId, string itemName, string itemType)
        {
            var cmd = new GeneralCommand
            {
                Name = "DisplayContent"
            };

            cmd.Arguments["ItemType"] = itemType;
            cmd.Arguments["ItemId"] = itemId;
            cmd.Arguments["ItemName"] = itemName;

            return SendCommandAsync(sessionId, cmd);
        }

        /// <summary>
        /// Sends the play command async.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="request">The request.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">sessionId
        /// or
        /// request</exception>
        public Task SendPlayCommandAsync(string sessionId, PlayRequest request)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                throw new ArgumentNullException("sessionId");
            }
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            var dict = new NameValueCollection
            {
                { "ItemIds", request.ItemIds.Select(o => o.ToString("N", CultureInfo.InvariantCulture)).ToList() },
                { "PlayCommand", request.PlayCommand.ToString() }
            };

            dict.AddIfNotNull("StartPositionTicks", request.StartPositionTicks);

            var url = GetApiUrl(new Uri("Sessions/" + sessionId + "/Playing", UriKind.Relative), dict);

            return PostAsync<EmptyRequestResult>(url, new NameValueCollection(), CancellationToken.None);
        }

        public Task SendMessageCommandAsync(string sessionId, MessageCommand command)
        {
            var cmd = new GeneralCommand
            {
                Name = "DisplayMessage"
            };

            cmd.Arguments["Header"] = command.Header;
            cmd.Arguments["Text"] = command.Text;

            if (command.TimeoutMs.HasValue)
            {
                cmd.Arguments["Timeout"] = command.TimeoutMs.Value.ToString(CultureInfo.InvariantCulture);
            }

            return SendCommandAsync(sessionId, cmd);
        }

        /// <summary>
        /// Sends the system command async.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="command">The command.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">sessionId</exception>
        public Task SendCommandAsync(string sessionId, GeneralCommand command)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                throw new ArgumentNullException("sessionId");
            }

            var url = GetApiUrl(new Uri("Sessions/" + sessionId + "/Command", UriKind.Relative));

            return PostAsync<GeneralCommand, EmptyRequestResult>(url, command, CancellationToken.None);
        }

        /// <summary>
        /// Sends the playstate command async.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="request">The request.</param>
        /// <returns>Task.</returns>
        public Task SendPlaystateCommandAsync(string sessionId, PlaystateRequest request)
        {
            var dict = new NameValueCollection();
            dict.AddIfNotNull("SeekPositionTicks", request.SeekPositionTicks);

            var url = GetApiUrl(new Uri("Sessions/" + sessionId + "/Playing/" + request.Command.ToString(), UriKind.Relative), dict);

            return PostAsync<EmptyRequestResult>(url, new NameValueCollection(), CancellationToken.None);
        }

        /// <summary>
        /// Clears a user's rating for an item
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{UserItemDataDto}.</returns>
        /// <exception cref="System.ArgumentNullException">itemId</exception>
        public Task<UserItemDataDto> ClearUserItemRatingAsync(string itemId, string userId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException("itemId");
            }

            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException("userId");
            }

            var url = GetApiUrl(new Uri("Users/" + userId + "/Items/" + itemId + "/Rating", UriKind.Relative));

            return DeleteAsync<UserItemDataDto>(url, CancellationToken.None);
        }

        /// <summary>
        /// Updates a user's rating for an item, based on likes or dislikes
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="likes">if set to <c>true</c> [likes].</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">itemId</exception>
        public Task<UserItemDataDto> UpdateUserItemRatingAsync(string itemId, string userId, bool likes)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException("itemId");
            }

            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException("userId");
            }

            var dict = new NameValueCollection();

            dict.Add("likes", likes);

            var url = GetApiUrl(new Uri("Users/" + userId + "/Items/" + itemId + "/Rating", UriKind.Relative), dict);

            return PostAsync<UserItemDataDto>(url, new NameValueCollection(), CancellationToken.None);
        }

        /// <summary>
        /// Authenticates a user and returns the result
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">username</exception>
        /// <exception cref="System.ArgumentNullException">userId</exception>
        public async Task<AuthenticationResult> AuthenticateUserAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentNullException("username");
            }

            var url = GetApiUrl(new Uri("Users/AuthenticateByName", UriKind.Relative));

            var authRequest = new AuthenticationRequest
            {
                Username = username,
                Pw = password
            };

            var result = await PostAsync<AuthenticationRequest,AuthenticationResult>(url, authRequest, CancellationToken.None);

            SetAuthenticationInfo(result.AccessToken, result.User.Id);

            Authenticated?.Invoke(this, new GenericEventArgs<AuthenticationResult>(result));

            return result;
        }

        /// <summary>
        /// Updates the server configuration async.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">configuration</exception>
        public Task UpdateServerConfigurationAsync(ServerConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            var url = GetApiUrl(new Uri("System/Configuration", UriKind.Relative));

            return PostAsync<ServerConfiguration, EmptyRequestResult>(url, configuration, CancellationToken.None);
        }

        /// <summary>
        /// Updates the scheduled task triggers.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="triggers">The triggers.</param>
        /// <returns>Task{RequestResult}.</returns>
        /// <exception cref="System.ArgumentNullException">id</exception>
        public Task UpdateScheduledTaskTriggersAsync(string id, TaskTriggerInfo[] triggers)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            if (triggers == null)
            {
                throw new ArgumentNullException("triggers");
            }

            var url = GetApiUrl(new Uri("ScheduledTasks/" + id + "/Triggers", UriKind.Relative));

            return PostAsync<TaskTriggerInfo[], EmptyRequestResult>(url, triggers, CancellationToken.None);
        }

        /// <summary>
        /// Gets the display preferences.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="client">The client.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        public async Task<DisplayPreferences> GetDisplayPreferencesAsync(string id, string userId, string client, CancellationToken cancellationToken = default)
        {
            var dict = new NameValueCollection
            {
                { "userId", userId },
                { "client", client }
            };

            var url = GetApiUrl(new Uri("DisplayPreferences/" + id, UriKind.Relative), dict);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<DisplayPreferences>(stream);
            }
        }

        /// <summary>
        /// Updates display preferences for a user
        /// </summary>
        /// <param name="displayPreferences">The display preferences.</param>
        /// <returns>Task{DisplayPreferences}.</returns>
        /// <exception cref="System.ArgumentNullException">userId</exception>
        public Task UpdateDisplayPreferencesAsync(DisplayPreferences displayPreferences, string userId, string client, CancellationToken cancellationToken = default)
        {
            if (displayPreferences == null)
            {
                throw new ArgumentNullException("displayPreferences");
            }

            var dict = new NameValueCollection
            {
                { "userId", userId },
                { "client", client }
            };

            var url = GetApiUrl(new Uri("DisplayPreferences/" + displayPreferences.Id, UriKind.Relative), dict);

            return PostAsync<DisplayPreferences, EmptyRequestResult>(url, displayPreferences, cancellationToken);
        }

        /// <summary>
        /// Posts a set of data to a url, and deserializes the return stream into T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url">The URL.</param>
        /// <param name="args">The args.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{``0}.</returns>
        public async Task<T> PostAsync<T>(Uri url, NameValueCollection query, CancellationToken cancellationToken = default)
            where T : class
        {
            url = AddDataFormat(url);

            // Create the post body
            var postContent = query.ToQueryString();

            const string contentType = "application/x-www-form-urlencoded";

            using (var stream = await SendAsync(new HttpRequest
            {
                Url = url,
                CancellationToken = cancellationToken,
                RequestHeaders = HttpHeaders,
                Method = "POST",
                RequestContentType = contentType,
                RequestContent = postContent
            }).ConfigureAwait(false))
            {
                return DeserializeFromStream<T>(stream);
            }
        }

        /// <summary>
        /// Deletes the async.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{``0}.</returns>
        private async Task<T> DeleteAsync<T>(Uri url, CancellationToken cancellationToken = default)
            where T : class
        {
            url = AddDataFormat(url);

            using (var stream = await SendAsync(new HttpRequest
            {
                Url = url,
                CancellationToken = cancellationToken,
                RequestHeaders = HttpHeaders,
                Method = "DELETE"

            }).ConfigureAwait(false))
            {
                return DeserializeFromStream<T>(stream);
            }
        }

        /// <summary>
        /// Posts an object of type TInputType to a given url, and deserializes the response into an object of type TOutputType
        /// </summary>
        /// <typeparam name="TInputType">The type of the T input type.</typeparam>
        /// <typeparam name="TOutputType">The type of the T output type.</typeparam>
        /// <param name="url">The URL.</param>
        /// <param name="obj">The obj.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{``1}.</returns>
        private async Task<TOutputType> PostAsync<TInputType, TOutputType>(Uri url, TInputType obj, CancellationToken cancellationToken = default)
            where TOutputType : class
        {
            url = AddDataFormat(url);

            const string contentType = "application/json";

            var postContent = SerializeToJson(obj);

            using (var stream = await SendAsync(new HttpRequest
            {
                Url = url,
                CancellationToken = cancellationToken,
                RequestHeaders = HttpHeaders,
                Method = "POST",
                RequestContentType = contentType,
                RequestContent = postContent
            }).ConfigureAwait(false))
            {
                return DeserializeFromStream<TOutputType>(stream);
            }
        }

        /// <summary>
        /// This is a helper around getting a stream from the server that contains serialized data
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Stream}.</returns>
        public Task<Stream> GetSerializedStreamAsync(Uri url, CancellationToken cancellationToken)
        {
            url = AddDataFormat(url);

            return GetStream(url, cancellationToken);
        }

        public Task<Stream> GetSerializedStreamAsync(Uri url)
        {
            return GetSerializedStreamAsync(url, CancellationToken.None);
        }

        public async Task<NotificationsSummary> GetNotificationsSummary(string userId)
        {
            var url = GetApiUrl(new Uri("Notifications/" + userId + "/Summary", UriKind.Relative));

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<NotificationsSummary>(stream);
            }
        }

        public Task MarkNotificationsRead(string userId, IEnumerable<string> notificationIdList, bool isRead)
        {
            Uri url = new Uri("Notifications/" + userId, UriKind.Relative);

            url = new Uri(url, isRead ? "/Read" : "/Unread");

            var dict = new NameValueCollection();

            var ids = notificationIdList.ToArray();

            dict.Add("Ids", string.Join(",", ids));

            url = GetApiUrl(url, dict);

            return PostAsync<EmptyRequestResult>(url, new NameValueCollection(), CancellationToken.None);
        }

        public async Task<NotificationResult> GetNotificationsAsync(NotificationQuery query)
        {
            var url = new Uri("Notifications/" + query.UserId, UriKind.Relative);

            var dict = new NameValueCollection();
            dict.AddIfNotNull("ItemIds", query.IsRead);
            dict.AddIfNotNull("StartIndex", query.StartIndex);
            dict.AddIfNotNull("Limit", query.Limit);

            url = GetApiUrl(url, dict);

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<NotificationResult>(stream);
            }
        }

        public async Task<AllThemeMediaResult> GetAllThemeMediaAsync(string userId, string itemId, bool inheritFromParent, CancellationToken cancellationToken = default)
        {
            var queryString = new NameValueCollection
            {
                { "InheritFromParent", inheritFromParent }
            };

            queryString.AddIfNotNullOrEmpty("UserId", userId);

            var url = GetApiUrl(new Uri("Items/" + itemId + "/ThemeMedia", UriKind.Relative), queryString);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<AllThemeMediaResult>(stream);
            }
        }

        public async Task<SearchHintResult> GetSearchHintsAsync(SearchQuery query)
        {
            if (query == null || string.IsNullOrEmpty(query.SearchTerm))
            {
                throw new ArgumentNullException("query");
            }

            var queryString = new NameValueCollection();

            queryString.AddIfNotNullOrEmpty("SearchTerm", query.SearchTerm);
            queryString.AddIfNotNullOrEmpty("UserId", query.UserId.ToString("N", CultureInfo.InvariantCulture));
            queryString.AddIfNotNullOrEmpty("ParentId", query.ParentId);
            queryString.AddIfNotNull("StartIndex", query.StartIndex);
            queryString.AddIfNotNull("Limit", query.Limit);

            queryString.Add("IncludeArtists", query.IncludeArtists);
            queryString.Add("IncludeGenres", query.IncludeGenres);
            queryString.Add("IncludeMedia", query.IncludeMedia);
            queryString.Add("IncludePeople", query.IncludePeople);
            queryString.Add("IncludeStudios", query.IncludeStudios);

            queryString.AddIfNotNull("ExcludeItemTypes", query.ExcludeItemTypes);
            queryString.AddIfNotNull("IncludeItemTypes", query.IncludeItemTypes);

            queryString.AddIfNotNull("IsKids", query.IsKids);
            queryString.AddIfNotNull("IsMovie", query.IsMovie);
            queryString.AddIfNotNull("IsNews", query.IsNews);
            queryString.AddIfNotNull("IsSeries", query.IsSeries);
            queryString.AddIfNotNull("IsSports", query.IsSports);
            queryString.AddIfNotNull("MediaTypes", query.MediaTypes);

            var url = GetApiUrl(new Uri("Search/Hints", UriKind.Relative), queryString);

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<SearchHintResult>(stream);
            }
        }

        public async Task<ThemeMediaResult> GetThemeSongsAsync(string userId, string itemId, bool inheritFromParent, CancellationToken cancellationToken = default)
        {
            var queryString = new NameValueCollection
            {
                { "InheritFromParent", inheritFromParent }
            };

            queryString.AddIfNotNullOrEmpty("UserId", userId);

            var url = GetApiUrl(new Uri("Items/" + itemId + "/ThemeSongs", UriKind.Relative), queryString);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<ThemeMediaResult>(stream);
            }
        }

        public async Task<ThemeMediaResult> GetThemeVideosAsync(string userId, string itemId, bool inheritFromParent, CancellationToken cancellationToken = default)
        {
            var queryString = new NameValueCollection
            {
                { "InheritFromParent", inheritFromParent }
            };

            queryString.AddIfNotNullOrEmpty("UserId", userId);

            var url = GetApiUrl(new Uri("Items/" + itemId + "/ThemeVideos", UriKind.Relative), queryString);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<ThemeMediaResult>(stream);
            }
        }

        /// <summary>
        /// Gets the critic reviews.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The limit.</param>
        /// <returns>Task{ItemReviewsResult}.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// id
        /// or
        /// userId
        /// </exception>
        public async Task<QueryResult<ItemReview>> GetCriticReviews(string itemId, CancellationToken cancellationToken = default, int? startIndex = null, int? limit = null)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                throw new ArgumentNullException("itemId");
            }

            var queryString = new NameValueCollection();

            queryString.AddIfNotNull("startIndex", startIndex);
            queryString.AddIfNotNull("limit", limit);

            var url = GetApiUrl(new Uri("Items/" + itemId + "/CriticReviews", UriKind.Relative), queryString);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<QueryResult<ItemReview>>(stream);
            }
        }

        public async Task<T> GetAsync<T>(Uri url, CancellationToken cancellationToken = default)
            where T : class
        {
            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<T>(stream);
            }
        }

        /// <summary>
        /// Gets the index of the game player.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{List{ItemIndex}}.</returns>
        public async Task<List<ItemIndex>> GetGamePlayerIndex(string userId, CancellationToken cancellationToken = default)
        {
            var queryString = new NameValueCollection();

            queryString.AddIfNotNullOrEmpty("UserId", userId);

            var url = GetApiUrl(new Uri("Games/PlayerIndex", UriKind.Relative), queryString);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<List<ItemIndex>>(stream);
            }
        }

        /// <summary>
        /// Gets the index of the year.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="includeItemTypes">The include item types.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{List{ItemIndex}}.</returns>
        public async Task<List<ItemIndex>> GetYearIndex(string userId, string[] includeItemTypes, CancellationToken cancellationToken = default)
        {
            var queryString = new NameValueCollection();

            queryString.AddIfNotNullOrEmpty("UserId", userId);
            queryString.AddIfNotNull("IncludeItemTypes", includeItemTypes);

            var url = GetApiUrl(new Uri("Items/YearIndex", UriKind.Relative), queryString);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<List<ItemIndex>>(stream);
            }
        }

        public Task ReportCapabilities(ClientCapabilities capabilities, CancellationToken cancellationToken = default)
        {
            if (capabilities == null)
            {
                throw new ArgumentNullException("capabilities");
            }

            var url = GetApiUrl(new Uri("Sessions/Capabilities/Full", UriKind.Relative));

            return PostAsync<ClientCapabilities, EmptyRequestResult>(url, capabilities, cancellationToken);
        }

        public async Task<LiveTvInfo> GetLiveTvInfoAsync(CancellationToken cancellationToken = default)
        {
            var url = GetApiUrl(new Uri("LiveTv/Info", UriKind.Relative));

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<LiveTvInfo>(stream);
            }
        }

        public async Task<QueryResult<BaseItemDto>> GetLiveTvRecordingGroupsAsync(RecordingGroupQuery query, CancellationToken cancellationToken = default)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var dict = new NameValueCollection();

            dict.AddIfNotNullOrEmpty("UserId", query.UserId);

            var url = GetApiUrl(new Uri("LiveTv/Recordings/Groups", UriKind.Relative), dict);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<QueryResult<BaseItemDto>>(stream);
            }
        }

        public async Task<QueryResult<BaseItemDto>> GetLiveTvRecordingsAsync(RecordingQuery query, CancellationToken cancellationToken = default)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var dict = new NameValueCollection();

            dict.AddIfNotNullOrEmpty("UserId", query.UserId.ToString("N", CultureInfo.InvariantCulture));
            dict.AddIfNotNullOrEmpty("ChannelId", query.ChannelId);
            dict.AddIfNotNullOrEmpty("Id", query.Id);
            dict.AddIfNotNullOrEmpty("SeriesTimerId", query.SeriesTimerId);
            dict.AddIfNotNull("IsInProgress", query.IsInProgress);
            dict.AddIfNotNull("StartIndex", query.StartIndex);
            dict.AddIfNotNull("Limit", query.Limit);

            if (!query.EnableTotalRecordCount)
            {
                dict.Add("EnableTotalRecordCount", query.EnableTotalRecordCount);
            }

            var url = GetApiUrl(new Uri("LiveTv/Recordings", UriKind.Relative), dict);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<QueryResult<BaseItemDto>>(stream);
            }
        }

        public async Task<QueryResult<ChannelInfoDto>> GetLiveTvChannelsAsync(LiveTvChannelQuery query, CancellationToken cancellationToken = default)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var dict = new NameValueCollection();

            dict.AddIfNotNullOrEmpty("UserId", query.UserId.ToString("N", CultureInfo.InvariantCulture));
            dict.AddIfNotNull("StartIndex", query.StartIndex);
            dict.AddIfNotNull("Limit", query.Limit);
            dict.AddIfNotNull("IsFavorite", query.IsFavorite);
            dict.AddIfNotNull("IsLiked", query.IsLiked);
            dict.AddIfNotNull("IsDisliked", query.IsDisliked);
            dict.AddIfNotNull("EnableFavoriteSorting", query.EnableFavoriteSorting);


            if (query.ChannelType.HasValue)
            {
                dict.Add("ChannelType", query.ChannelType.Value.ToString());
            }

            var url = GetApiUrl(new Uri("LiveTv/Channels", UriKind.Relative), dict);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<QueryResult<ChannelInfoDto>>(stream);
            }
        }

        public Task CancelLiveTvSeriesTimerAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            var dict = new NameValueCollection();

            var url = GetApiUrl(new Uri("LiveTv/SeriesTimers/" + id, UriKind.Relative), dict);

            return SendAsync(new HttpRequest
            {
                Url = url,
                CancellationToken = cancellationToken,
                RequestHeaders = HttpHeaders,
                Method = "DELETE"
            });
        }

        public Task CancelLiveTvTimerAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            var dict = new NameValueCollection();

            var url = GetApiUrl(new Uri("LiveTv/Timers/" + id, UriKind.Relative), dict);

            return SendAsync(new HttpRequest
            {
                Url = url,
                CancellationToken = cancellationToken,
                RequestHeaders = HttpHeaders,
                Method = "DELETE"
            });
        }

        public async Task<ChannelInfoDto> GetLiveTvChannelAsync(string id, string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            var dict = new NameValueCollection();
            dict.AddIfNotNullOrEmpty("userId", userId);

            var url = GetApiUrl(new Uri("LiveTv/Channels/" + id, UriKind.Relative), dict);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<ChannelInfoDto>(stream);
            }
        }

        public async Task<BaseItemDto> GetLiveTvRecordingAsync(string id, string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            var dict = new NameValueCollection();
            dict.AddIfNotNullOrEmpty("userId", userId);

            var url = GetApiUrl(new Uri("LiveTv/Recordings/" + id, UriKind.Relative), dict);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<BaseItemDto>(stream);
            }
        }

        public async Task<BaseItemDto> GetLiveTvRecordingGroupAsync(string id, string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            var dict = new NameValueCollection();
            dict.AddIfNotNullOrEmpty("userId", userId);

            var url = GetApiUrl(new Uri("LiveTv/Recordings/Groups/" + id, UriKind.Relative), dict);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<BaseItemDto>(stream);
            }
        }

        public async Task<SeriesTimerInfoDto> GetLiveTvSeriesTimerAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            var dict = new NameValueCollection();

            var url = GetApiUrl(new Uri("LiveTv/SeriesTimers/" + id, UriKind.Relative), dict);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<SeriesTimerInfoDto>(stream);
            }
        }

        public async Task<QueryResult<SeriesTimerInfoDto>> GetLiveTvSeriesTimersAsync(SeriesTimerQuery query, CancellationToken cancellationToken = default)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var dict = new NameValueCollection();

            dict.AddIfNotNullOrEmpty("SortBy", query.SortBy);
            dict.Add("SortOrder", query.SortOrder.ToString());

            var url = GetApiUrl(new Uri("LiveTv/SeriesTimers", UriKind.Relative), dict);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<QueryResult<SeriesTimerInfoDto>>(stream);
            }
        }

        public async Task<TimerInfoDto> GetLiveTvTimerAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            var dict = new NameValueCollection();

            var url = GetApiUrl(new Uri("LiveTv/Timers/" + id, UriKind.Relative), dict);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<TimerInfoDto>(stream);
            }
        }

        public async Task<QueryResult<TimerInfoDto>> GetLiveTvTimersAsync(TimerQuery query, CancellationToken cancellationToken = default)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var dict = new NameValueCollection();

            dict.AddIfNotNullOrEmpty("ChannelId", query.ChannelId);
            dict.AddIfNotNullOrEmpty("SeriesTimerId", query.SeriesTimerId);

            var url = GetApiUrl(new Uri("LiveTv/Timers", UriKind.Relative), dict);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<QueryResult<TimerInfoDto>>(stream);
            }
        }

        public async Task<QueryResult<BaseItemDto>> GetLiveTvProgramsAsync(ProgramQuery query, CancellationToken cancellationToken = default)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var dict = new NameValueCollection();

            const string isoDateFormat = "o";

            if (query.MaxEndDate.HasValue)
            {
                dict.Add("MaxEndDate", query.MaxEndDate.Value.ToUniversalTime().ToString(isoDateFormat, CultureInfo.InvariantCulture));
            }
            if (query.MaxStartDate.HasValue)
            {
                dict.Add("MaxStartDate", query.MaxStartDate.Value.ToUniversalTime().ToString(isoDateFormat, CultureInfo.InvariantCulture));
            }
            if (query.MinEndDate.HasValue)
            {
                dict.Add("MinEndDate", query.MinEndDate.Value.ToUniversalTime().ToString(isoDateFormat, CultureInfo.InvariantCulture));
            }
            if (query.MinStartDate.HasValue)
            {
                dict.Add("MinStartDate", query.MinStartDate.Value.ToUniversalTime().ToString(isoDateFormat, CultureInfo.InvariantCulture));
            }

            dict.AddIfNotNullOrEmpty("UserId", query.UserId);

            if (!query.EnableTotalRecordCount)
            {
                dict.Add("EnableTotalRecordCount", query.EnableTotalRecordCount);
            }

            if (query.ChannelIds != null)
            {
                dict.Add("ChannelIds", string.Join(",", query.ChannelIds));
            }

            // TODO: This endpoint supports POST if the query string is too long
            var url = GetApiUrl(new Uri("LiveTv/Programs", UriKind.Relative), dict);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<QueryResult<BaseItemDto>>(stream);
            }
        }

        public async Task<QueryResult<BaseItemDto>> GetRecommendedLiveTvProgramsAsync(RecommendedProgramQuery query, CancellationToken cancellationToken = default)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var dict = new NameValueCollection();

            dict.AddIfNotNullOrEmpty("UserId", query.UserId);
            dict.AddIfNotNull("Limit", query.Limit);
            dict.AddIfNotNull("HasAired", query.HasAired);
            dict.AddIfNotNull("IsAiring", query.IsAiring);

            if (!query.EnableTotalRecordCount)
            {
                dict.Add("EnableTotalRecordCount", query.EnableTotalRecordCount);
            }

            var url = GetApiUrl(new Uri("LiveTv/Programs/Recommended", UriKind.Relative), dict);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<QueryResult<BaseItemDto>>(stream);
            }
        }

        public Task CreateLiveTvSeriesTimerAsync(SeriesTimerInfoDto timer, CancellationToken cancellationToken = default)
        {
            if (timer == null)
            {
                throw new ArgumentNullException("timer");
            }

            var url = GetApiUrl(new Uri("LiveTv/SeriesTimers", UriKind.Relative));

            return PostAsync<SeriesTimerInfoDto, EmptyRequestResult>(url, timer, cancellationToken);
        }

        public Task CreateLiveTvTimerAsync(BaseTimerInfoDto timer, CancellationToken cancellationToken = default)
        {
            if (timer == null)
            {
                throw new ArgumentNullException("timer");
            }

            var url = GetApiUrl(new Uri("LiveTv/Timers", UriKind.Relative));

            return PostAsync<BaseTimerInfoDto, EmptyRequestResult>(url, timer, cancellationToken);
        }

        public async Task<SeriesTimerInfoDto> GetDefaultLiveTvTimerInfo(string programId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(programId))
            {
                throw new ArgumentNullException("programId");
            }

            var dict = new NameValueCollection();

            dict.AddIfNotNullOrEmpty("programId", programId);

            var url = GetApiUrl(new Uri("LiveTv/Timers/Defaults", UriKind.Relative), dict);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<SeriesTimerInfoDto>(stream);
            }
        }

        public async Task<SeriesTimerInfoDto> GetDefaultLiveTvTimerInfo(CancellationToken cancellationToken = default)
        {
            var url = GetApiUrl(new Uri("LiveTv/Timers/Defaults", UriKind.Relative));

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<SeriesTimerInfoDto>(stream);
            }
        }

        public async Task<GuideInfo> GetLiveTvGuideInfo(CancellationToken cancellationToken = default)
        {
            var url = GetApiUrl(new Uri("LiveTv/GuideInfo", UriKind.Relative));

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<GuideInfo>(stream);
            }
        }

        public async Task<BaseItemDto> GetLiveTvProgramAsync(string id, string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            var dict = new NameValueCollection();
            dict.AddIfNotNullOrEmpty("userId", userId);

            var url = GetApiUrl(new Uri("LiveTv/Programs/" + id, UriKind.Relative), dict);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<BaseItemDto>(stream);
            }
        }

        public Task UpdateLiveTvSeriesTimerAsync(SeriesTimerInfoDto timer, CancellationToken cancellationToken = default)
        {
            if (timer == null)
            {
                throw new ArgumentNullException("timer");
            }

            var url = GetApiUrl(new Uri("LiveTv/SeriesTimers/" + timer.Id, UriKind.Relative));

            return PostAsync<SeriesTimerInfoDto, EmptyRequestResult>(url, timer, cancellationToken);
        }

        public Task UpdateLiveTvTimerAsync(TimerInfoDto timer, CancellationToken cancellationToken = default)
        {
            if (timer == null)
            {
                throw new ArgumentNullException("timer");
            }

            var url = GetApiUrl(new Uri("LiveTv/Timers/" + timer.Id, UriKind.Relative));

            return PostAsync<TimerInfoDto, EmptyRequestResult>(url, timer, cancellationToken);
        }

        public Task SendString(string sessionId, string text)
        {
            var cmd = new GeneralCommand
            {
                Name = "SendString"
            };

            cmd.Arguments["String"] = text;

            return SendCommandAsync(sessionId, cmd);
        }

        public Task SetAudioStreamIndex(string sessionId, int index)
        {
            var cmd = new GeneralCommand
            {
                Name = "SetAudioStreamIndex"
            };

            cmd.Arguments["Index"] = index.ToString(CultureInfo.InvariantCulture);

            return SendCommandAsync(sessionId, cmd);
        }

        public Task SetSubtitleStreamIndex(string sessionId, int? index)
        {
            var cmd = new GeneralCommand
            {
                Name = "SetSubtitleStreamIndex"
            };

            cmd.Arguments["Index"] = (index ?? -1).ToString(CultureInfo.InvariantCulture);

            return SendCommandAsync(sessionId, cmd);
        }

        public Task SetVolume(string sessionId, int volume)
        {
            var cmd = new GeneralCommand
            {
                Name = "SetVolume"
            };

            cmd.Arguments["Volume"] = volume.ToString(CultureInfo.InvariantCulture);

            return SendCommandAsync(sessionId, cmd);
        }

        public async Task<QueryResult<BaseItemDto>> GetAdditionalParts(string itemId, string userId)
        {
            var queryString = new NameValueCollection();

            queryString.AddIfNotNullOrEmpty("UserId", userId);

            var url = GetApiUrl(new Uri("Videos/" + itemId + "/AdditionalParts", UriKind.Relative), queryString);

            using (var stream = await GetSerializedStreamAsync(url, CancellationToken.None).ConfigureAwait(false))
            {
                return DeserializeFromStream<QueryResult<BaseItemDto>>(stream);
            }
        }

        public async Task<ChannelFeatures> GetChannelFeatures(string channelId, CancellationToken cancellationToken = default)
        {
            var url = GetApiUrl(new Uri("Channels/" + channelId + "/Features", UriKind.Relative));

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<ChannelFeatures>(stream);
            }
        }

        public async Task<QueryResult<BaseItemDto>> GetChannelItems(ChannelItemQuery query, CancellationToken cancellationToken = default)
        {
            var queryString = new NameValueCollection();

            queryString.AddIfNotNullOrEmpty("UserId", query.UserId);
            queryString.AddIfNotNull("StartIndex", query.StartIndex);
            queryString.AddIfNotNull("Limit", query.Limit);
            queryString.AddIfNotNullOrEmpty("FolderId", query.FolderId);
            if (query.Fields != null)
            {
                queryString.Add("fields", query.Fields.Select(f => f.ToString()));
            }
            if (query.Filters != null)
            {
                queryString.Add("Filters", query.Filters.Select(f => f.ToString()));
            }

            var sortBy = new List<string>();
            var sortOrder = new List<string>();
            foreach (var order in query.OrderBy)
            {
                sortBy.Add(order.Item1);
                sortOrder.Add(order.Item2.ToString());
            }

            queryString.AddIfNotNull("SortBy", sortBy);
            queryString.AddIfNotNull("SortOrder", sortOrder);

            var url = GetApiUrl(new Uri("Channels/" + query.ChannelId + "/Items", UriKind.Relative), queryString);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<QueryResult<BaseItemDto>>(stream);
            }
        }

        public async Task<QueryResult<BaseItemDto>> GetChannels(ChannelQuery query, CancellationToken cancellationToken = default)
        {
            var queryString = new NameValueCollection();

            queryString.AddIfNotNullOrEmpty("UserId", query.UserId.ToString("N", CultureInfo.InvariantCulture));
            queryString.AddIfNotNull("SupportsLatestItems", query.SupportsLatestItems);
            queryString.AddIfNotNull("StartIndex", query.StartIndex);
            queryString.AddIfNotNull("Limit", query.Limit);
            queryString.AddIfNotNull("IsFavorite", query.IsFavorite);

            var url = GetApiUrl(new Uri("Channels", UriKind.Relative), queryString);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<QueryResult<BaseItemDto>>(stream);
            }
        }

        public async Task<SessionInfoDto> GetCurrentSessionAsync(CancellationToken cancellationToken = default)
        {
            var queryString = new NameValueCollection
            {
                { "DeviceId", DeviceId }
            };

            var url = GetApiUrl(new Uri("Sessions", UriKind.Relative), queryString);

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                var sessions = DeserializeFromStream<SessionInfoDto[]>(stream);

                return sessions.FirstOrDefault();
            }
        }

        public Task StopTranscodingProcesses(string deviceId, string playSessionId)
        {
            var queryString = new NameValueCollection
            {
                { "DeviceId", DeviceId }
            };

            queryString.AddIfNotNullOrEmpty("PlaySessionId", playSessionId);
            Uri url = GetApiUrl(new Uri("Videos/ActiveEncodings", UriKind.Relative), queryString);

            return SendAsync(new HttpRequest
            {
                Url = url,
                RequestHeaders = HttpHeaders,
                Method = "DELETE"
            });
        }

        public async Task<QueryResult<BaseItemDto>> GetLatestChannelItems(AllChannelMediaQuery query, CancellationToken cancellationToken = default)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            if (string.IsNullOrEmpty(query.UserId))
            {
                throw new ArgumentNullException("userId");
            }

            var queryString = new NameValueCollection
            {
                { "UserId", query.UserId }
            };

            queryString.AddIfNotNull("StartIndex", query.StartIndex);
            queryString.AddIfNotNull("Limit", query.Limit);

            if (query.Filters != null)
            {
                queryString.Add("Filters", query.Filters.Select(f => f.ToString()));
            }
            if (query.Fields != null)
            {
                queryString.Add("Fields", query.Fields.Select(f => f.ToString()));
            }

            queryString.AddIfNotNull("ChannelIds", query.ChannelIds);

            var url = GetApiUrl(new Uri("Channels/Items/Latest", UriKind.Relative));

            using (var stream = await GetSerializedStreamAsync(url, CancellationToken.None).ConfigureAwait(false))
            {
                return DeserializeFromStream<QueryResult<BaseItemDto>>(stream);
            }
        }

        public async Task Logout()
        {
            try
            {
                var url = GetApiUrl(new Uri("Sessions/Logout", UriKind.Relative));

                await PostAsync<EmptyRequestResult>(url, new NameValueCollection(), CancellationToken.None);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error logging out");
            }

            ClearAuthenticationInfo();
        }

        public async Task<QueryResult<BaseItemDto>> GetUserViews(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException("userId");
            }

            var url = GetApiUrl(new Uri($"Users/{userId}/Views", UriKind.Relative));

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                var result = DeserializeFromStream<QueryResult<BaseItemDto>>(stream);

                var serverInfo = ServerInfo;

                return result;
            }
        }

        public async Task<BaseItemDto[]> GetLatestItems(LatestItemsQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            if (string.IsNullOrEmpty(query.UserId.ToString("N", CultureInfo.InvariantCulture)))
            {
                throw new ArgumentNullException("userId");
            }

            var queryString = new NameValueCollection();
            queryString.AddIfNotNull("GroupItems", query.GroupItems);
            queryString.AddIfNotNull("IncludeItemTypes", query.IncludeItemTypes);
            queryString.AddIfNotNullOrEmpty("ParentId", query.ParentId.ToString("N", CultureInfo.InvariantCulture));
            queryString.AddIfNotNull("IsPlayed", query.IsPlayed);
            queryString.AddIfNotNull("StartIndex", query.StartIndex);
            queryString.AddIfNotNull("Limit", query.Limit);

            if (query.Fields != null)
            {
                queryString.Add("fields", query.Fields.Select(f => f.ToString()));
            }

            var url = GetApiUrl(new Uri("Users/" + query.UserId + "/Items/Latest", UriKind.Relative), queryString);

            using (var stream = await GetSerializedStreamAsync(url, CancellationToken.None).ConfigureAwait(false))
            {
                return DeserializeFromStream<BaseItemDto[]>(stream);
            }
        }

        public Task AddToPlaylist(string playlistId, IEnumerable<string> itemIds, string userId)
        {
            if (playlistId == null)
            {
                throw new ArgumentNullException("playlistId");
            }

            if (itemIds == null)
            {
                throw new ArgumentNullException("itemIds");
            }

            var dict = new NameValueCollection();

            dict.AddIfNotNull("Ids", itemIds);
            var url = GetApiUrl(new Uri(string.Format("Playlists/{0}/Items", playlistId), UriKind.Relative), dict);
            return PostAsync<EmptyRequestResult>(url, new NameValueCollection(), CancellationToken.None);
        }

        public async Task<PlaylistCreationResult> CreatePlaylist(PlaylistCreationRequest request)
        {
            if (string.IsNullOrEmpty(request.UserId.ToString("N", CultureInfo.InvariantCulture)))
            {
                throw new ArgumentNullException("userId");
            }

            if (string.IsNullOrEmpty(request.MediaType) && (request.ItemIdList == null || !request.ItemIdList.Any()))
            {
                throw new ArgumentNullException("must provide either MediaType or Ids");
            }

            var queryString = new NameValueCollection
            {
                { "UserId", request.UserId.ToString("N", CultureInfo.InvariantCulture) },
                { "Name", request.Name }
            };

            if (!string.IsNullOrEmpty(request.MediaType))
                queryString.Add("MediaType", request.MediaType);

            if (request.ItemIdList != null && request.ItemIdList.Any())
                queryString.Add("Ids", request.ItemIdList.Select(o => o.ToString("N", CultureInfo.InvariantCulture)).ToList());

            var url = GetApiUrl(new Uri("Playlists/", UriKind.Relative), queryString);

            return await PostAsync<PlaylistCreationResult>(url, new NameValueCollection(), CancellationToken.None);

        }

        public async Task<QueryResult<BaseItemDto>> GetPlaylistItems(PlaylistItemQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var dict = new NameValueCollection();

            dict.AddIfNotNull("StartIndex", query.StartIndex);

            dict.AddIfNotNull("Limit", query.Limit);
            dict.Add("UserId", query.UserId);

            if (query.Fields != null)
            {
                dict.Add("fields", query.Fields.Select(f => f.ToString()));
            }

            var url = GetApiUrl(new Uri("Playlists/" + query.Id + "/Items", UriKind.Relative), dict);

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<QueryResult<BaseItemDto>>(stream);
            }
        }

        public Task RemoveFromPlaylist(string playlistId, IEnumerable<string> entryIds)
        {
            if (playlistId == null)
            {
                throw new ArgumentNullException("playlistId");
            }

            if (entryIds == null)
            {
                throw new ArgumentNullException("entryIds");
            }

            var dict = new NameValueCollection();

            dict.AddIfNotNull("EntryIds", entryIds);
            var url = GetApiUrl(new Uri(string.Format("Playlists/{0}/Items", playlistId), UriKind.Relative), dict);
            return DeleteAsync<EmptyRequestResult>(url, CancellationToken.None);
        }

        public async Task<ContentUploadHistory> GetContentUploadHistory(string deviceId)
        {
            var dict = new NameValueCollection();

            dict.Add("DeviceId", deviceId);

            var url = GetApiUrl(new Uri("Devices/CameraUploads", UriKind.Relative), dict);

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<ContentUploadHistory>(stream);
            }
        }

        public async Task UploadFile(Stream stream, LocalFileInfo file, CancellationToken cancellationToken = default)
        {
            var dict = new NameValueCollection();

            dict.Add("DeviceId", DeviceId);
            dict.Add("Name", file.Name);
            dict.Add("Id", file.Id);
            dict.AddIfNotNullOrEmpty("Album", file.Album);

            var url = GetApiUrl(new Uri("Devices/CameraUploads", UriKind.Relative), dict);

            using (stream)
            {
                await SendAsync(new HttpRequest
                {
                    CancellationToken = cancellationToken,
                    Method = "POST",
                    RequestHeaders = HttpHeaders,
                    Url = url,
                    RequestContentType = file.MimeType,
                    RequestStream = stream

                }, false).ConfigureAwait(false);
            }
        }

        public async Task<DevicesOptions> GetDevicesOptions()
        {
            var dict = new NameValueCollection();

            var url = GetApiUrl(new Uri("System/Configuration/devices", UriKind.Relative), dict);

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<DevicesOptions>(stream);
            }
        }

        public async Task<PlaybackInfoResponse> GetPlaybackInfo(PlaybackInfoRequest request)
        {
            var dict = new NameValueCollection();

            dict.AddIfNotNullOrEmpty("UserId", request.UserId.ToString("N", CultureInfo.InvariantCulture));

            var url = GetApiUrl(new Uri("Items/" + request.Id + "/PlaybackInfo", UriKind.Relative), dict);

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<PlaybackInfoResponse>(stream);
            }
        }

        public Task<QueryFilters> GetFilters(string userId, string parentId, string[] mediaTypes, string[] itemTypes)
        {
            throw new NotImplementedException();
        }

        public Task UpdateItem(BaseItemDto item)
        {
            throw new NotImplementedException();
        }

        public Task UpdateUserConfiguration(string userId, UserConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            var url = GetApiUrl(new Uri("Users/" + userId + "/Configuration", UriKind.Relative));

            return PostAsync<UserConfiguration, EmptyRequestResult>(url, configuration, CancellationToken.None);
        }

        public async Task<UserDto> GetOfflineUserAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            var url = GetApiUrl(new Uri("Users/" + id + "/Offline", UriKind.Relative));

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<UserDto>(stream);
            }
        }

        public async Task<List<RecommendationDto>> GetMovieRecommendations(MovieRecommendationQuery query)
        {
            var dict = new NameValueCollection();

            dict.AddIfNotNullOrEmpty("UserId", query.UserId);
            dict.AddIfNotNullOrEmpty("ParentId", query.ParentId);
            dict.AddIfNotNull("ItemLimit", query.ItemLimit);
            dict.AddIfNotNull("CategoryLimit", query.CategoryLimit);

            if (query.Fields != null)
            {
                dict.Add("fields", query.Fields.Select(f => f.ToString()));
            }

            var url = GetApiUrl(new Uri("Movies/Recommendations", UriKind.Relative), dict);

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<List<RecommendationDto>>(stream);
            }
        }

        public Task<LiveStreamResponse> OpenLiveStream(LiveStreamRequest request, CancellationToken cancellationToken)
        {
            var url = GetApiUrl(new Uri("LiveStreams/Open", UriKind.Relative));

            return PostAsync<LiveStreamRequest, LiveStreamResponse>(url, request, cancellationToken);
        }

        public Task<int> DetectMaxBitrate(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<EndPointInfo> GetEndPointInfo(CancellationToken cancellationToken)
        {
            var url = GetApiUrl(new Uri("System/Endpoint", UriKind.Relative));

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<EndPointInfo>(stream);
            }
        }

        public async Task<QueryResult<BaseItemDto>> GetInstantMixFromItemAsync(SimilarItemsQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var url = GetInstantMixUrl(query, "Items");

            using (var stream = await GetSerializedStreamAsync(url).ConfigureAwait(false))
            {
                return DeserializeFromStream<QueryResult<BaseItemDto>>(stream);
            }
        }

        public async Task<QueryResult<BaseItemDto>> GetSimilarItemsAsync(SimilarItemsQuery query, CancellationToken cancellationToken = default)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var url = GetSimilarItemListUrl(query, "Items");

            using (var stream = await GetSerializedStreamAsync(url, cancellationToken).ConfigureAwait(false))
            {
                return DeserializeFromStream<QueryResult<BaseItemDto>>(stream);
            }
        }
    }
}
