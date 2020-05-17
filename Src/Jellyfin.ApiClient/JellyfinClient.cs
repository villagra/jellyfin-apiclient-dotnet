using Jellyfin.ApiClient.Auth;
using Jellyfin.ApiClient.Exceptions;
using Jellyfin.ApiClient.Model;
using Jellyfin.ApiClient.Serialization;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.ApiClient
{
    /// <summary>
    /// 0
    /// </summary>
    public class JellyfinClient : JellyfinBaseClient, IApiClient
    {        
        public JellyfinClient(Uri serverAddress, IAuthenticationMethod authentication)
            : base (serverAddress, authentication, null)
        { }

        public JellyfinClient(Uri serverAddress, IAuthenticationMethod authentication, JellyfinClientOptions options)
            : base(serverAddress, authentication, options)
        { }

        public async Task<AuthenticationResult> AuthenticateUserAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentNullException(nameof(username));
            }

            AuthenticationResult result = null;

            try
            {
                result = await DoPost<AuthenticationResult>("Users/AuthenticateByName", new AuthenticationRequest(username, password));
            }
            catch (RequestFailedException ex)
            {
                if (ex.Status == System.Net.HttpStatusCode.InternalServerError || ex.Status == System.Net.HttpStatusCode.Unauthorized)
                {
                    throw new AuthenticationException();
                }

                throw;
            }
            catch (Exception)
            {
                throw;
            }

            if (result != null)
            {
                SetAuthenticationInfo(result.User, result.AccessToken);
            }

            return result;         
        }

        public async Task<QueryResult<BaseItem>> GetUserViews(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            return await DoGet<QueryResult<BaseItem>>($"Users/{userId}/Views");
        }

        /// <summary>
        /// Queries for items
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{ItemsResult}.</returns>
        /// <exception cref="System.ArgumentNullException">query</exception>
        public async Task<QueryResult<BaseItem>> GetItemsAsync(string userId, ItemFilters filters = null)
        {
            return await DoGet<QueryResult<BaseItem>>($"Users/{userId}/Items", filters);
        }

        public async Task<QueryResult<BaseItem>> GetItemsInProgressAsync(string userId, ItemFilters filters = null)
        {
            return await DoGet<QueryResult<BaseItem>>($"Users/{userId}/Items/Resume", filters);
        }


        public async Task<BaseItem> GetItemAsync(string userId, string itemId)
        {
            return await DoGet<BaseItem>($"Users/{userId}/Items/{itemId}");
        }

        public async Task<QueryResult<SeasonItem>> GetSeasons(string userId, string showId)
        {
            ItemFilters filters = ItemFilters.Create().FilterByUserId(userId);
            return await DoGet<QueryResult<SeasonItem>>($"Shows/{showId}/Seasons", filters);
        }

        public async Task<QueryResult<EpisodeItem>> GetEpisodes(string userId, string showId, string seasonId)
        {
            ItemFilters filters = ItemFilters.Create().FilterByUserId(userId).FilterBySeasonId(seasonId).IncludeField(nameof(EpisodeItem.Overview));
            return await DoGet<QueryResult<EpisodeItem>>($"Shows/{showId}/Episodes", filters);
        }

        public async Task<QueryResult<EpisodeItem>> GetNextEpisode(string userId, string showId)
        {
            ItemFilters filters = ItemFilters.Create().FilterByUserId(userId).FilterBySeriesId(showId);
            return await DoGet<QueryResult<EpisodeItem>>($"Shows/NextUp", filters);
        }

        public async Task<Model.PlaybackInfoResponse> GetPlaybackInfoAsync(string userId, string itemId)
        {
            return await DoGet<Model.PlaybackInfoResponse>($"Items/{itemId}/PlaybackInfo?UserId={userId}");
        }

        public async Task UpdatePlaybackStatus(string mediaSourceId, TimeSpan position)
        {
            PlaybackProgressInfo data = new PlaybackProgressInfo();
            if (Guid.TryParse(mediaSourceId, out Guid itemId))
            {
                data.ItemId = itemId;
            }

            data.MediaSourceId = mediaSourceId;
            data.PositionTicks = position.Ticks;
            await DoPost("Sessions/Playing/Progress", data);
        }

        public async Task<UserItemDataDto> SetAsWatched(string userId, string itemId, bool watched)
        {
            if (watched)
            {
                //20200517081414
                return await DoPost<UserItemDataDto>($"Users/{userId}/PlayedItems/{itemId}", new object());
            }
            return await DoDelete<UserItemDataDto>($"Users/{userId}/PlayedItems/{itemId}");
        }
    }
}
