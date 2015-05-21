using System;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using LeagueRecorder.Server.Contracts.League;
using LeagueRecorder.Server.Localization;
using LeagueRecorder.Shared.League;
using LeagueRecorder.Shared.Results;
using Microsoft.Owin;
using Newtonsoft.Json.Linq;

namespace LeagueRecorder.Server.Infrastructure.League
{
    public class LeagueSpectatorApiClient : ILeagueSpectatorApiClient
    {
        public async Task<Result<RiotGameMetaData>> GetGameMetaData(Region region, long gameId)
        {
            HttpResponseMessage response = await this.GetClient(region)
                .GetAsync(string.Format("observer-mode/rest/consumer/getGameMetaData/{0}/{1}/1/token", region.SpectatorPlatformId, gameId))
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var responseJson = JObject.Parse(responseString);

                var gameMetaData = new RiotGameMetaData
                {
                    EndGameChunkId = responseJson.Value<int>("endGameChunkId"),
                    EndGameKeyFrameId = responseJson.Value<int>("endGameKeyFrameId"),
                    EndStartupChunkId = responseJson.Value<int>("endStartupChunkId"),
                    GameId = responseJson.Value<JObject>("gameKey").Value<long>("gameId"),
                    GameLength = TimeSpan.FromMilliseconds(responseJson.Value<int>("gameLength")),
                    LastChunkId = responseJson.Value<int>("lastChunkId"),
                    LastKeyFrameId = responseJson.Value<int>("lastKeyFrameId"),
                    StartGameChunkId = responseJson.Value<int>("startGameChunkId"),
                    GameEnded = responseJson.Value<bool>("gameEnded"),
                    Region = region.ToString(),
                    OriginalJsonResponse = responseString
                };

                return Result.AsSuccess(gameMetaData);
            }
            else
            {
                return Result.AsError(Messages.UnexpectedError);
            }
        }

        public async Task<Result<RiotLastChunkInfo>> GetLastChunkInfo(Region region, long gameId)
        {
            var response = await this.GetClient(region)
                .GetAsync(string.Format("observer-mode/rest/consumer/getLastChunkInfo/{0}/{1}/1/token", region.SpectatorPlatformId, gameId))
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var responseJson = JObject.Parse(responseString);

                var lastChunkInfo = new RiotLastChunkInfo
                {
                    CurrentChunkId = responseJson.Value<int>("chunkId"),
                    OriginalJsonResponse = responseString
                };

                return Result.AsSuccess(lastChunkInfo);
            }
            else
            {
                return Result.AsError(Messages.UnexpectedError);
            }
        }

        public async Task<Result<RiotChunk>> GetChunk(Region region, long gameId, int chunkId)
        {
            HttpResponseMessage response = await this.GetClient(region)
                .GetAsync(string.Format("observer-mode/rest/consumer/getGameDataChunk/{0}/{1}/{2}/token", region.SpectatorPlatformId, gameId, chunkId))
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var responseData = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                var chunk = new RiotChunk
                {
                    Id = chunkId,
                    Data = responseData
                };

                return Result.AsSuccess(chunk);
            }
            else
            {
                return Result.AsError(Messages.UnexpectedError);
            }
        }

        public async Task<Result<RiotKeyFrame>> GetKeyFrame(Region region, long gameId, int keyFrameId)
        {
            HttpResponseMessage response = await this.GetClient(region)
                .GetAsync(string.Format("observer-mode/rest/consumer/getKeyFrame/{0}/{1}/{2}/token", region.SpectatorPlatformId, gameId, keyFrameId))
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var responseData = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                var keyFrame = new RiotKeyFrame
                {
                    Id = keyFrameId,
                    Data = responseData
                };

                return Result.AsSuccess(keyFrame);
            }
            else
            {
                return Result.AsError(Messages.UnexpectedError);
            }
        }

        private HttpClient GetClient(Region region)
        {
            var client = HttpClientFactory.Create();
            client.BaseAddress = new Uri(string.Format("{0}://{1}:{2}/", Uri.UriSchemeHttp, region.SpectatorUrl, region.SpectatorPort));

            return client;
        }
    }
}