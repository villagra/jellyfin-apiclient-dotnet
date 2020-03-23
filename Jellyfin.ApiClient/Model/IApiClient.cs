using Jellyfin.ApiClient.Model.Dto;
using Jellyfin.ApiClient.Model.Notifications;
using Jellyfin.ApiClient.Model.Querying;
using MediaBrowser.Controller.Authentication;
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
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.ApiClient.Model
{
    /// <summary>
    /// Interface IApiClient
    /// </summary>
    public interface IApiClient : IServerEvents, IDisposable
    {
        /// <summary>
        /// Occurs when [remote logged out].
        /// </summary>
        event EventHandler<GenericEventArgs<RemoteLogoutReason>> RemoteLoggedOut;

        /// <summary>
        /// Gets the API URL.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <returns>System.Uri.</returns>
        Uri GetApiUrl(Uri handler);

        /// <summary>
        /// Gets the async.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{``0}.</returns>
        Task<T> GetAsync<T>(Uri url, CancellationToken cancellationToken = default)
            where T : class;

        /// <summary>
        /// Reports the capabilities.
        /// </summary>
        /// <param name="capabilities">The capabilities.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task ReportCapabilities(ClientCapabilities capabilities, CancellationToken cancellationToken = default);

        /// <summary>
        /// Logouts this instance.
        /// </summary>
        /// <returns>Task.</returns>
        Task Logout();

        /// <summary>
        /// Gets the index of the game players.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{List{ItemIndex}}.</returns>
        Task<List<ItemIndex>> GetGamePlayerIndex(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the index of the year.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="includeItemTypes">The include item types.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{List{ItemIndex}}.</returns>
        Task<List<ItemIndex>> GetYearIndex(string userId, string[] includeItemTypes, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the critic reviews.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The limit.</param>
        /// <returns>Task{ItemReviewsResult}.</returns>
        Task<QueryResult<ItemReview>> GetCriticReviews(string itemId, CancellationToken cancellationToken = default, int? startIndex = null, int? limit = null);

        /// <summary>
        /// Gets the theme songs async.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="itemId">The item id.</param>
        /// <param name="inheritFromParents">if set to <c>true</c> [inherit from parents].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{ThemeMediaResult}.</returns>
        Task<ThemeMediaResult> GetThemeSongsAsync(string userId, string itemId, bool inheritFromParents, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the search hints async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{SearchHintResult}.</returns>
        Task<SearchHintResult> GetSearchHintsAsync(SearchQuery query);

        /// <summary>
        /// Gets the filters.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="parentId">The parent identifier.</param>
        /// <param name="mediaTypes">The media types.</param>
        /// <param name="itemTypes">The item types.</param>
        /// <returns>Task&lt;QueryFilters&gt;.</returns>
        Task<QueryFilters> GetFilters(string userId, string parentId, string[] mediaTypes, string[] itemTypes);

        /// <summary>
        /// Gets the theme videos async.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="itemId">The item id.</param>
        /// <param name="inheritFromParents">if set to <c>true</c> [inherit from parents].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{ThemeMediaResult}.</returns>
        Task<ThemeMediaResult> GetThemeVideosAsync(string userId, string itemId, bool inheritFromParents, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all theme media async.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="itemId">The item id.</param>
        /// <param name="inheritFromParents">if set to <c>true</c> [inherit from parents].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{AllThemeMediaResult}.</returns>
        Task<AllThemeMediaResult> GetAllThemeMediaAsync(string userId, string itemId, bool inheritFromParents, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks the notifications read.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="notificationIdList">The notification id list.</param>
        /// <param name="isRead">if set to <c>true</c> [is read].</param>
        /// <returns>Task.</returns>
        Task MarkNotificationsRead(string userId, IEnumerable<string> notificationIdList, bool isRead);

        /// <summary>
        /// Gets the notifications summary.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{NotificationsSummary}.</returns>
        Task<NotificationsSummary> GetNotificationsSummary(string userId);

        /// <summary>
        /// Gets the notifications async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{NotificationResult}.</returns>
        Task<NotificationResult> GetNotificationsAsync(NotificationQuery query);

        /// <summary>
        /// Gets an image stream based on a url
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Stream}.</returns>
        /// <exception cref="ArgumentNullException">url</exception>
        Task<Stream> GetImageStreamAsync(Uri url, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the stream.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;Stream&gt;.</returns>
        Task<Stream> GetStream(Uri url, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the response.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;HttpResponse&gt;.</returns>
        Task<HttpWebResponse> GetResponse(Uri url, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the user configuration.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>Task.</returns>
        Task UpdateUserConfiguration(string userId, UserConfiguration configuration);

        /// <summary>
        /// Gets a BaseItem
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        /// <exception cref="ArgumentNullException">id</exception>
        Task<BaseItemDto> GetItemAsync(string id, string userId);

        /// <summary>
        /// Gets the latest items.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task&lt;QueryResult&lt;BaseItemDto&gt;&gt;.</returns>
        Task<BaseItemDto[]> GetLatestItems(LatestItemsQuery query);

        /// <summary>
        /// Gets the intros async.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<QueryResult<BaseItemDto>> GetIntrosAsync(string itemId, string userId);

        /// <summary>
        /// Gets a BaseItem
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        /// <exception cref="ArgumentNullException">userId</exception>
        Task<BaseItemDto> GetRootFolderAsync(string userId);

        /// <summary>
        /// Gets the additional parts.
        /// </summary>
        /// <param name="itemId">The item identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <returns>Task{BaseItemDto[]}.</returns>
        Task<QueryResult<BaseItemDto>> GetAdditionalParts(string itemId, string userId);

        /// <summary>
        /// Gets the playback information.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task&lt;LiveMediaInfoResult&gt;.</returns>
        Task<PlaybackInfoResponse> GetPlaybackInfo(PlaybackInfoRequest request);

        /// <summary>
        /// Gets the users async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{UserDto[]}.</returns>
        Task<UserDto[]> GetUsersAsync(UserQuery query);

        /// <summary>
        /// Gets the public users async.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{UserDto[]}.</returns>
        Task<UserDto[]> GetPublicUsersAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets active client sessions.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{SessionInfoDto[]}.</returns>
        Task<SessionInfoDto[]> GetClientSessionsAsync(SessionQuery query);

        /// <summary>
        /// Gets the client session asynchronous.
        /// </summary>
        /// <returns>Task{SessionInfoDto}.</returns>
        Task<SessionInfoDto> GetCurrentSessionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the item counts async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemCounts}.</returns>
        Task<ItemCounts> GetItemCountsAsync(ItemCountsQuery query);

        /// <summary>
        /// Gets the episodes asynchronous.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<QueryResult<BaseItemDto>> GetEpisodesAsync(EpisodeQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the seasons asynchronous.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<QueryResult<BaseItemDto>> GetSeasonsAsync(SeasonQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Queries for items
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{ItemsResult}.</returns>
        /// <exception cref="ArgumentNullException">query</exception>
        Task<QueryResult<BaseItemDto>> GetItemsAsync(ItemQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the user views.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;ItemsResult&gt;.</returns>
        Task<QueryResult<BaseItemDto>> GetUserViews(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the instant mix from item asynchronous.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task&lt;ItemsResult&gt;.</returns>
        Task<QueryResult<BaseItemDto>> GetInstantMixFromItemAsync(SimilarItemsQuery query);

        /// <summary>
        /// Gets the similar movies async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<QueryResult<BaseItemDto>> GetSimilarItemsAsync(SimilarItemsQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the people async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{ItemsResult}.</returns>
        /// <exception cref="ArgumentNullException">userId</exception>
        Task<QueryResult<BaseItemDto>> GetPeopleAsync(PersonsQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the artists.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        /// <exception cref="ArgumentNullException">userId</exception>
        Task<QueryResult<BaseItemDto>> GetArtistsAsync(ArtistsQuery query);

        /// <summary>
        /// Gets the album artists asynchronous.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<QueryResult<BaseItemDto>> GetAlbumArtistsAsync(ArtistsQuery query);

        /// <summary>
        /// Gets the next up async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<QueryResult<BaseItemDto>> GetNextUpEpisodesAsync(NextUpQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the upcoming episodes asynchronous.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<QueryResult<BaseItemDto>> GetUpcomingEpisodesAsync(UpcomingEpisodesQuery query);

        /// <summary>
        /// Gets the genres async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<QueryResult<BaseItemDto>> GetGenresAsync(ItemsByNameQuery query);

        /// <summary>
        /// Gets the studios async.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        Task<QueryResult<BaseItemDto>> GetStudiosAsync(ItemsByNameQuery query);

        /// <summary>
        /// Restarts the server.
        /// </summary>
        /// <returns>Task.</returns>
        Task RestartServerAsync();

        /// <summary>
        /// Gets the system status async.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{SystemInfo}.</returns>
        Task<SystemInfo> GetSystemInfoAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the public system information asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;PublicSystemInfo&gt;.</returns>
        Task<PublicSystemInfo> GetPublicSystemInfoAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a list of plugins installed on the server
        /// </summary>
        /// <returns>Task{PluginInfo[]}.</returns>
        Task<PluginInfo[]> GetInstalledPluginsAsync();

        /// <summary>
        /// Gets the current server configuration
        /// </summary>
        /// <returns>Task{ServerConfiguration}.</returns>
        Task<ServerConfiguration> GetServerConfigurationAsync();

        /// <summary>
        /// Gets the scheduled tasks.
        /// </summary>
        /// <returns>Task{TaskInfo[]}.</returns>
        Task<TaskInfo[]> GetScheduledTasksAsync();

        /// <summary>
        /// Gets the scheduled task async.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>Task{TaskInfo}.</returns>
        /// <exception cref="ArgumentNullException">id</exception>
        Task<TaskInfo> GetScheduledTaskAsync(string id);

        /// <summary>
        /// Gets a user by id
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>Task{UserDto}.</returns>
        /// <exception cref="ArgumentNullException">id</exception>
        Task<UserDto> GetUserAsync(string id);

        /// <summary>
        /// Gets the offline user asynchronous.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Task&lt;UserDto&gt;.</returns>
        Task<UserDto> GetOfflineUserAsync(string id);

        /// <summary>
        /// Gets the parental ratings async.
        /// </summary>
        /// <returns>Task{List{ParentalRating}}.</returns>
        Task<List<ParentalRating>> GetParentalRatingsAsync();

        /// <summary>
        /// Gets local trailers for an item
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="itemId">The item id.</param>
        /// <returns>Task{ItemsResult}.</returns>
        /// <exception cref="ArgumentNullException">query</exception>
        Task<BaseItemDto[]> GetLocalTrailersAsync(string userId, string itemId);

        /// <summary>
        /// Gets special features for an item
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="itemId">The item id.</param>
        /// <returns>Task{BaseItemDto[]}.</returns>
        /// <exception cref="ArgumentNullException">userId</exception>
        Task<BaseItemDto[]> GetSpecialFeaturesAsync(string userId, string itemId);

        /// <summary>
        /// Gets the cultures async.
        /// </summary>
        /// <returns>Task{CultureDto[]}.</returns>
        Task<CultureDto[]> GetCulturesAsync();

        /// <summary>
        /// Gets the countries async.
        /// </summary>
        /// <returns>Task{CountryInfo[]}.</returns>
        Task<CountryInfo[]> GetCountriesAsync();

        /// <summary>
        /// Marks the played async.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="datePlayed">The date played.</param>
        /// <returns>Task{UserItemDataDto}.</returns>
        Task<UserItemDataDto> MarkPlayedAsync(string itemId, string userId, DateTime? datePlayed);

        /// <summary>
        /// Marks the unplayed async.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{UserItemDataDto}.</returns>
        Task<UserItemDataDto> MarkUnplayedAsync(string itemId, string userId);

        /// <summary>
        /// Updates the favorite status async.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="isFavorite">if set to <c>true</c> [is favorite].</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">itemId</exception>
        Task<UserItemDataDto> UpdateFavoriteStatusAsync(string itemId, string userId, bool isFavorite);

        /// <summary>
        /// Reports to the server that the user has begun playing an item
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns>Task{UserItemDataDto}.</returns>
        /// <exception cref="ArgumentNullException">itemId</exception>
        Task ReportPlaybackStartAsync(PlaybackStartInfo info);

        /// <summary>
        /// Reports playback progress to the server
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns>Task{UserItemDataDto}.</returns>
        /// <exception cref="ArgumentNullException">itemId</exception>
        Task ReportPlaybackProgressAsync(PlaybackProgressInfo info);

        /// <summary>
        /// Reports to the server that the user has stopped playing an item
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns>Task{UserItemDataDto}.</returns>
        /// <exception cref="ArgumentNullException">itemId</exception>
        Task ReportPlaybackStoppedAsync(PlaybackStopInfo info);

        /// <summary>
        /// Instructs another client to browse to a library item.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="itemId">The id of the item to browse to.</param>
        /// <param name="itemName">The name of the item to browse to.</param>
        /// <param name="itemType">The type of the item to browse to.</param>
        /// <returns>Task.</returns>
        Task SendBrowseCommandAsync(string sessionId, string itemId, string itemName, string itemType);

        /// <summary>
        /// Sends the playstate command async.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="request">The request.</param>
        /// <returns>Task.</returns>
        Task SendPlaystateCommandAsync(string sessionId, PlaystateRequest request);

        /// <summary>
        /// Sends the play command async.
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="request">The request.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">sessionId
        /// or
        /// request</exception>
        Task SendPlayCommandAsync(string sessionId, PlayRequest request);

        /// <summary>
        /// Sends the command asynchronous.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="command">The command.</param>
        /// <returns>Task.</returns>
        Task SendCommandAsync(string sessionId, GeneralCommand command);

        /// <summary>
        /// Sends the string.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="text">The text.</param>
        /// <returns>Task.</returns>
        Task SendString(string sessionId, string text);

        /// <summary>
        /// Sets the volume.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="volume">The volume.</param>
        /// <returns>Task.</returns>
        Task SetVolume(string sessionId, int volume);

        /// <summary>
        /// Stops the transcoding processes.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="streamId">The stream identifier.</param>
        /// <returns>Task.</returns>
        Task StopTranscodingProcesses(string deviceId, string streamId);

        /// <summary>
        /// Sets the index of the audio stream.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="index">The index.</param>
        /// <returns>Task.</returns>
        Task SetAudioStreamIndex(string sessionId, int index);

        /// <summary>
        /// Sets the index of the subtitle stream.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="index">The index.</param>
        /// <returns>Task.</returns>
        Task SetSubtitleStreamIndex(string sessionId, int? index);

        /// <summary>
        /// Instructs the client to display a message to the user
        /// </summary>
        /// <param name="sessionId">The session id.</param>
        /// <param name="command">The command.</param>
        /// <returns>Task.</returns>
        Task SendMessageCommandAsync(string sessionId, MessageCommand command);

        /// <summary>
        /// Clears a user's rating for an item
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{UserItemDataDto}.</returns>
        /// <exception cref="ArgumentNullException">itemId</exception>
        Task<UserItemDataDto> ClearUserItemRatingAsync(string itemId, string userId);

        /// <summary>
        /// Updates a user's rating for an item, based on likes or dislikes
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="likes">if set to <c>true</c> [likes].</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">itemId</exception>
        Task<UserItemDataDto> UpdateUserItemRatingAsync(string itemId, string userId, bool likes);


        event EventHandler<GenericEventArgs<AuthenticationResult>> Authenticated;

        /// <summary>
        /// Authenticates a user and returns the result
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">userId</exception>
        Task<AuthenticationResult> AuthenticateUserAsync(string username,
            string password);

        /// <summary>
        /// Updates the server configuration async.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">configuration</exception>
        Task UpdateServerConfigurationAsync(ServerConfiguration configuration);

        /// <summary>
        /// Updates the scheduled task triggers.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="triggers">The triggers.</param>
        /// <returns>Task{RequestResult}.</returns>
        /// <exception cref="ArgumentNullException">id</exception>
        Task UpdateScheduledTaskTriggersAsync(string id, TaskTriggerInfo[] triggers);

        /// <summary>
        /// Gets the display preferences.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="client">The client.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{BaseItemDto}.</returns>
        Task<DisplayPreferences> GetDisplayPreferencesAsync(string id, string userId, string client, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates display preferences for a user
        /// </summary>
        /// <param name="displayPreferences">The display preferences.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="client">The client.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{DisplayPreferences}.</returns>
        /// <exception cref="System.ArgumentNullException">userId</exception>
        Task UpdateDisplayPreferencesAsync(DisplayPreferences displayPreferences, string userId, string client, CancellationToken cancellationToken = default);

        /// <summary>
        /// Posts a set of data to a url, and deserializes the return stream into T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url">The URL.</param>
        /// <param name="args">The args.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{``0}.</returns>
        Task<T> PostAsync<T>(Uri url, NameValueCollection args, CancellationToken cancellationToken = default)
            where T : class;

        /// <summary>
        /// This is a helper around getting a stream from the server that contains serialized data
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>Task{Stream}.</returns>
        Task<Stream> GetSerializedStreamAsync(Uri url);

        /// <summary>
        /// Gets the json serializer.
        /// </summary>
        /// <value>The json serializer.</value>
        IJsonSerializer JsonSerializer { get; set; }

        /// <summary>
        /// Gets or sets the server address
        /// </summary>
        /// <value>The server address.</value>
        Uri ServerAddress { get; }

        /// <summary>
        /// Gets or sets the type of the client.
        /// </summary>
        /// <value>The type of the client.</value>
        string ClientName { get; set; }

        /// <summary>
        /// Gets the device.
        /// </summary>
        /// <value>The device.</value>
        IDevice Device { get; }

        /// <summary>
        /// Gets or sets the name of the device.
        /// </summary>
        /// <value>The name of the device.</value>
        string DeviceName { get; }

        /// <summary>
        /// Gets or sets the device id.
        /// </summary>
        /// <value>The device id.</value>
        string DeviceId { get; }

        /// <summary>
        /// Gets or sets the current user id.
        /// </summary>
        /// <value>The current user id.</value>
        Guid CurrentUserId { get; }

        /// <summary>
        /// Gets the access token.
        /// </summary>
        /// <value>The access token.</value>
        string AccessToken { get; }

        /// <summary>
        /// Sets the authentication information.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        /// <param name="userId">The user identifier.</param>
        void SetAuthenticationInfo(string accessToken, Guid userId);

        /// <summary>
        /// Sets the authentication information.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        void SetAuthenticationInfo(string accessToken);

        /// <summary>
        /// Clears the authentication information.
        /// </summary>
        void ClearAuthenticationInfo();

        /// <summary>
        /// Changes the server location.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="keepExistingAuth">if set to <c>true</c> [keep existing authentication].</param>
        void ChangeServerLocation(Uri address, bool keepExistingAuth = false);

        /// <summary>
        /// Starts the receiving session updates.
        /// </summary>
        /// <param name="intervalMs">The interval ms.</param>
        /// <returns>Task.</returns>
        Task StartReceivingSessionUpdates(int intervalMs);

        /// <summary>
        /// Stops the receiving session updates.
        /// </summary>
        /// <returns>Task.</returns>
        Task StopReceivingSessionUpdates();

        /// <summary>
        /// Gets the image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentNullException">item</exception>
        Uri GetImageUrl(BaseItemDto item, ImageOptions options);

        /// <summary>
        /// Gets the image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        Uri GetImageUrl(ChannelInfoDto item, ImageOptions options);

        /// <summary>
        /// Gets the subtitle URL.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        Uri GetSubtitleUrl(SubtitleDownloadOptions options);

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="itemId">The Id of the item</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentNullException">itemId</exception>
        Uri GetImageUrl(string itemId, ImageOptions options);

        /// <summary>
        /// Gets the user image URL.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        Uri GetUserImageUrl(UserDto user, ImageOptions options);

        /// <summary>
        /// Gets an image url that can be used to download an image from the api
        /// </summary>
        /// <param name="userId">The Id of the user</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentNullException">userId</exception>
        Uri GetUserImageUrl(Guid userId, ImageOptions options);

        /// <summary>
        /// Gets the person image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentNullException">item</exception>
        Uri GetPersonImageUrl(BaseItemPerson item, ImageOptions options);

        /// <summary>
        /// This is a helper to get a list of backdrop url's from a given ApiBaseItemWrapper. If the actual item does not have any backdrops it will return backdrops from the first parent that does.
        /// </summary>
        /// <param name="item">A given item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String[][].</returns>
        /// <exception cref="ArgumentNullException">item</exception>
        Uri[] GetBackdropImageUrls(BaseItemDto item, ImageOptions options);

        /// <summary>
        /// This is a helper to get the logo image url from a given ApiBaseItemWrapper. If the actual item does not have a logo, it will return the logo from the first parent that does, or null.
        /// </summary>
        /// <param name="item">A given item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentNullException">item</exception>
        Uri GetLogoImageUrl(BaseItemDto item, ImageOptions options);

        /// <summary>
        /// Gets the art image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        Uri GetArtImageUrl(BaseItemDto item, ImageOptions options);

        /// <summary>
        /// Gets the thumb image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        Uri GetThumbImageUrl(BaseItemDto item, ImageOptions options);

        /// <summary>
        /// Gets the live tv information asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{LiveTvInfo}.</returns>
        Task<LiveTvInfo> GetLiveTvInfoAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the live tv channels asynchronous.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{LiveTvInfo}.</returns>
        Task<QueryResult<ChannelInfoDto>> GetLiveTvChannelsAsync(LiveTvChannelQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the live tv channel asynchronous.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{ChannelInfoDto}.</returns>
        Task<ChannelInfoDto> GetLiveTvChannelAsync(string id, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the live tv recordings asynchronous.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{RecordingInfoDto}}.</returns>
        Task<QueryResult<BaseItemDto>> GetLiveTvRecordingsAsync(RecordingQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the live tv recording asynchronous.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{RecordingInfoDto}.</returns>
        Task<BaseItemDto> GetLiveTvRecordingAsync(string id, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the live tv recording groups asynchronous.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{RecordingGroupDto}}.</returns>
        Task<QueryResult<BaseItemDto>> GetLiveTvRecordingGroupsAsync(RecordingGroupQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the live tv recording group asynchronous.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{RecordingGroupDto}.</returns>
        Task<BaseItemDto> GetLiveTvRecordingGroupAsync(string id, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the live tv timers asynchronous.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{TimerInfoDto}}.</returns>
        Task<QueryResult<TimerInfoDto>> GetLiveTvTimersAsync(TimerQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the live tv programs asynchronous.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{ProgramInfoDto}}.</returns>
        Task<QueryResult<BaseItemDto>> GetLiveTvProgramsAsync(ProgramQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the live tv program asynchronous.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{ProgramInfoDto}.</returns>
        Task<BaseItemDto> GetLiveTvProgramAsync(string id, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the recommended live tv programs asynchronous.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{ProgramInfoDto}}.</returns>
        Task<QueryResult<BaseItemDto>> GetRecommendedLiveTvProgramsAsync(RecommendedProgramQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates the live tv timer asynchronous.
        /// </summary>
        /// <param name="timer">The timer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task CreateLiveTvTimerAsync(BaseTimerInfoDto timer, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the live tv timer asynchronous.
        /// </summary>
        /// <param name="timer">The timer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task UpdateLiveTvTimerAsync(TimerInfoDto timer, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates the live tv series timer asynchronous.
        /// </summary>
        /// <param name="timer">The timer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task CreateLiveTvSeriesTimerAsync(SeriesTimerInfoDto timer, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the live tv series timer asynchronous.
        /// </summary>
        /// <param name="timer">The timer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task UpdateLiveTvSeriesTimerAsync(SeriesTimerInfoDto timer, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the live tv timer asynchronous.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{TimerInfoDto}.</returns>
        Task<TimerInfoDto> GetLiveTvTimerAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the live tv series timers asynchronous.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{SeriesTimerInfoDto}}.</returns>
        Task<QueryResult<SeriesTimerInfoDto>> GetLiveTvSeriesTimersAsync(SeriesTimerQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the live tv series timer asynchronous.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{SeriesTimerInfoDto}.</returns>
        Task<SeriesTimerInfoDto> GetLiveTvSeriesTimerAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels the live tv timer asynchronous.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task CancelLiveTvTimerAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels the live tv series timer asynchronous.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task CancelLiveTvSeriesTimerAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the default timer information.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{SeriesTimerInfoDto}.</returns>
        Task<SeriesTimerInfoDto> GetDefaultLiveTvTimerInfo(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the live tv guide information.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{GuideInfo}.</returns>
        Task<GuideInfo> GetLiveTvGuideInfo(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the default timer information.
        /// </summary>
        /// <param name="programId">The program identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{SeriesTimerInfoDto}.</returns>
        Task<SeriesTimerInfoDto> GetDefaultLiveTvTimerInfo(string programId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the channel features.
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{ChannelFeatures}.</returns>
        Task<ChannelFeatures> GetChannelFeatures(string channelId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the channel items.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{BaseItemDto}}.</returns>
        Task<QueryResult<BaseItemDto>> GetChannelItems(ChannelItemQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the channels.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{BaseItemDto}}.</returns>
        Task<QueryResult<BaseItemDto>> GetChannels(ChannelQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the latest channel items.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{BaseItemDto}}.</returns>
        Task<QueryResult<BaseItemDto>> GetLatestChannelItems(AllChannelMediaQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates the playlist.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task&lt;PlaylistCreationResult&gt;.</returns>
        Task<PlaylistCreationResult> CreatePlaylist(PlaylistCreationRequest request);

        /// <summary>
        /// Adds to playlist.
        /// </summary>
        /// <param name="playlistId">The playlist identifier.</param>
        /// <param name="itemIds">The item ids.</param>
        /// <param name="userId">The user identifier.</param>
        /// <returns>Task.</returns>
        Task AddToPlaylist(string playlistId, IEnumerable<string> itemIds, string userId);

        /// <summary>
        /// Removes from playlist.
        /// </summary>
        /// <param name="playlistId">The playlist identifier.</param>
        /// <param name="entryIds">The entry ids.</param>
        /// <returns>Task.</returns>
        Task RemoveFromPlaylist(string playlistId, IEnumerable<string> entryIds);

        /// <summary>
        /// Gets the playlist items.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task&lt;QueryResult&lt;BaseItemDto&gt;&gt;.</returns>
        Task<QueryResult<BaseItemDto>> GetPlaylistItems(PlaylistItemQuery query);

        /// <summary>
        /// Sends the context message asynchronous.
        /// </summary>
        /// <param name="itemType">Type of the item.</param>
        /// <param name="itemId">The item identifier.</param>
        /// <param name="itemName">Name of the item.</param>
        /// <param name="context">The context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendContextMessageAsync(string itemType, string itemId, string itemName, string context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the content upload history.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <returns>Task&lt;ContentUploadHistory&gt;.</returns>
        Task<ContentUploadHistory> GetContentUploadHistory(string deviceId);

        /// <summary>
        /// Uploads the file.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="file">The file.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task UploadFile(Stream stream,
            LocalFileInfo file,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the devices options options.
        /// </summary>
        /// <returns>Task&lt;DevicesOptions&gt;.</returns>
        Task<DevicesOptions> GetDevicesOptions();

        /// <summary>
        /// Updates the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>Task.</returns>
        Task UpdateItem(BaseItemDto item);

        /// <summary>
        /// Opens the web socket.
        /// </summary>
        void OpenWebSocket(Func<IClientWebSocket> webSocketFactory);

        /// <summary>
        /// Gets the movie recommendations.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task&lt;List&lt;RecommendationDto&gt;&gt;.</returns>
        Task<List<RecommendationDto>> GetMovieRecommendations(MovieRecommendationQuery query);
        /// <summary>
        /// Opens the live stream.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;LiveStreamResponse&gt;.</returns>
        Task<LiveStreamResponse> OpenLiveStream(LiveStreamRequest request, CancellationToken cancellationToken);
        /// <summary>
        /// Gets the supported bitrate.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;System.Int32&gt;.</returns>
        Task<int> DetectMaxBitrate(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the end point information.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>System.Threading.Tasks.Task&lt;MediaBrowser.Model.Net.EndPointInfo&gt;.</returns>
        Task<EndPointInfo> GetEndPointInfo(CancellationToken cancellationToken);
    }
}