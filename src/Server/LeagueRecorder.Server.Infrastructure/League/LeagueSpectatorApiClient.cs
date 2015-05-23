using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using LeagueRecorder.Server.Contracts.League;
using LeagueRecorder.Server.Localization;
using LeagueRecorder.Shared.League;
using LeagueRecorder.Shared.Results;
using Newtonsoft.Json.Linq;

namespace LeagueRecorder.Server.Infrastructure.League
{
    public class LeagueSpectatorApiClient : ILeagueSpectatorApiClient
    {
        public async Task<Result<Version>> GetSpectatorVersion(Region region)
        {
            var response = await this.GetClient(region)
                .GetAsync("observer-mode/rest/consumer/version")
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var version = Version.Parse(responseString);

                return Result.AsSuccess(version);
            }
            else
            {
                return Result.AsError(Messages.UnexpectedError);
            }
        }

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

        public async Task<Result<RiotLastGameInfo>> GetLastGameInfo(Region region, long gameId)
        {
            var response = await this.GetClient(region)
                .GetAsync(string.Format("observer-mode/rest/consumer/getLastChunkInfo/{0}/{1}/1/token", region.SpectatorPlatformId, gameId))
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var responseJson = JObject.Parse(responseString);

                var lastChunkInfo = new RiotLastGameInfo
                {
                    CurrentChunkId = responseJson.Value<int>("chunkId"),
                    CurrentKeyFrameId = responseJson.Value<int>("keyFrameId"),
                    OriginalJsonResponse = responseString,
                    EndGameChunkId = responseJson.Value<int>("endGameChunkId")
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