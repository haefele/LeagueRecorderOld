using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Anotar.NLog;
using LeagueRecorder.Server.Contracts.LeagueApi;
using LeagueRecorder.Server.Localization;
using LeagueRecorder.Shared;
using LeagueRecorder.Shared.League.Api;
using LeagueRecorder.Shared.Results;
using Newtonsoft.Json.Linq;

namespace LeagueRecorder.Server.Infrastructure.LeagueApi
{
    public class LeagueSpectatorApiClient : ILeagueSpectatorApiClient
    {
        public async Task<Result<Version>> GetSpectatorVersion(Region region)
        {
            try
            {
                var response = await this.GetClient(region)
                    .GetAsync("observer-mode/rest/consumer/version");

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var version = Version.Parse(responseString);

                    return Result.AsSuccess(version);
                }
                else
                {
                    return Result.AsError(Messages.UnexpectedError);
                }
            }
            catch (Exception exception)
            {
                LogTo.ErrorException("Exception while retrieving the spectator version.", exception);
                return Result.FromException(exception);
            }
        }

        public async Task<Result<RiotGameMetaData>> GetGameMetaData(Region region, long gameId)
        {
            try
            {
                HttpResponseMessage response = await this.GetClient(region)
                    .GetAsync(string.Format("observer-mode/rest/consumer/getGameMetaData/{0}/{1}/1/token", region.SpectatorPlatformId, gameId));

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var responseJson = JObject.Parse(responseString);

                    var gameMetaData = new RiotGameMetaData
                    {
                        EndGameChunkId = responseJson.Value<int>("endGameChunkId"),
                        EndGameKeyFrameId = responseJson.Value<int>("endGameKeyFrameId"),
                        EndStartupChunkId = responseJson.Value<int>("endStartupChunkId"),
                        GameId = responseJson.Value<JObject>("gameKey").Value<long>("gameId"),
                        GameLength = TimeSpan.FromMilliseconds(responseJson.Value<int>("gameLength")),
                        StartGameChunkId = responseJson.Value<int>("startGameChunkId"),
                        Region = region.ToString(),
                        ChunkTimeInterval = TimeSpan.FromMilliseconds(responseJson.Value<int>("chunkTimeInterval")),
                        ClientAddedLag = TimeSpan.FromMilliseconds(responseJson.Value<int>("clientAddedLag")),
                        CreateTime = DateTime.Parse(responseJson.Value<string>("createTime")),
                        DelayTime = TimeSpan.FromMilliseconds(responseJson.Value<int>("delayTime")),
                        EndTime = DateTime.Parse(responseJson.Value<string>("endTime")),
                        InterestScore = responseJson.Value<int>("interestScore"),
                        KeyFrameTimeInterval = TimeSpan.FromMilliseconds(responseJson.Value<int>("keyFrameTimeInterval")),
                        StartTime = DateTime.Parse(responseJson.Value<string>("startTime"))
                    };

                    return Result.AsSuccess(gameMetaData);
                }
                else
                {
                    return Result.AsError(Messages.UnexpectedError);
                }
            }
            catch (Exception exception)
            {
                LogTo.ErrorException("Exception while retrieving game meta data.", exception);
                return Result.FromException(exception);
            }
        }

        public async Task<Result<RiotLastGameInfo>> GetLastGameInfo(Region region, long gameId)
        {
            try 
            { 
                var response = await this.GetClient(region)
                    .GetAsync(string.Format("observer-mode/rest/consumer/getLastChunkInfo/{0}/{1}/1/token", region.SpectatorPlatformId, gameId));

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
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
            catch (Exception exception)
            {
                LogTo.ErrorException("Exception while retrieving last game info.", exception);
                return Result.FromException(exception);
            }
        }

        public async Task<Result<RiotChunk>> GetChunk(Region region, long gameId, int chunkId)
        {
            try
            {
                HttpResponseMessage response = await this.GetClient(region)
                    .GetAsync(string.Format("observer-mode/rest/consumer/getGameDataChunk/{0}/{1}/{2}/token", region.SpectatorPlatformId, gameId, chunkId));

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var responseData = await response.Content.ReadAsByteArrayAsync();

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
            catch (Exception exception)
            {
                LogTo.ErrorException("Exception while retrieving a chunk.", exception);
                return Result.FromException(exception);
            }
        }

        public async Task<Result<RiotKeyFrame>> GetKeyFrame(Region region, long gameId, int keyFrameId)
        {
            try
            {
                HttpResponseMessage response = await this.GetClient(region)
                    .GetAsync(string.Format("observer-mode/rest/consumer/getKeyFrame/{0}/{1}/{2}/token", region.SpectatorPlatformId, gameId, keyFrameId));

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var responseData = await response.Content.ReadAsByteArrayAsync();

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
            catch (Exception exception)
            {
                LogTo.ErrorException("Exception while retrieving the spectator version.", exception);
                return Result.FromException(exception);
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