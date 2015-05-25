using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using LeagueRecorder.Server.Contracts.League;
using LeagueRecorder.Server.Localization;
using LeagueRecorder.Shared;
using LeagueRecorder.Shared.League;
using LeagueRecorder.Shared.Results;
using LiteGuard;
using Newtonsoft.Json.Linq;

namespace LeagueRecorder.Server.Infrastructure.League
{
    public class LeagueApiClient : ILeagueApiClient
    {
        #region Fields
        private readonly string _apiKey;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="LeagueApiClient"/> class.
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        public LeagueApiClient(string apiKey)
        {
            Guard.AgainstNullArgument("apiKey", apiKey);

            this._apiKey = apiKey;
        }
        #endregion

        #region Methods

        public async Task<Result<Version>> GetLeagueVersion(Region region)
        {
            HttpResponseMessage response = await this.GetClient()
                .GetAsync(string.Format("api/lol/static-data/{0}/v1.2/versions", region.RiotApiPlatformId))
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var responseJson = JArray.Parse(responseString);

                var largestVersion = responseJson
                    .Select(f => f.ToObject<string>())
                    .Select(Version.Parse)
                    .OrderByDescending(f => f)
                    .First();

                return Result.AsSuccess(largestVersion);
            }
            else
            {
                return Result.AsError(Messages.UnexpectedError);
            }
        }

        public async Task<Result<RiotSummoner>> GetSummonerBySummonerNameAsync(Region region, string summonerName)
        {
            Guard.AgainstNullArgument("summonerName", summonerName);

            HttpResponseMessage response = await this.GetClient(region)
                .GetAsync(string.Format("api/lol/{0}/v1.4/summoner/by-name/{1}", region.RiotApiPlatformId, summonerName))
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var responseJson = JObject.Parse(responseString).First.First;

                var summoner = new RiotSummoner
                {
                    Id = responseJson.Value<long>("id"),
                    Name = responseJson.Value<string>("name"),
                    Region = region.ToString()
                };

                return Result.AsSuccess(summoner);
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return Result.AsError(Messages.SummonerNotFound);
            }
            else if (response.StatusCode == (HttpStatusCode)429)
            {
                return Result.AsError(Messages.RateLimitExceeded);
            }
            else
            {
                return Result.AsError(Messages.UnexpectedError);
            }
        }

        public async Task<Result<RiotSpectatorGameInfo>> GetCurrentGameAsync(Region region, long summonerId)
        {
            HttpResponseMessage response = await this.GetClient(region)
                .GetAsync(string.Format("observer-mode/rest/consumer/getSpectatorGameInfo/{0}/{1}", region.SpectatorPlatformId, summonerId))
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var responseJson = JObject.Parse(responseString);

                var gameInfo = new RiotSpectatorGameInfo
                {
                    GameId = responseJson.Value<long>("gameId"),
                    GameLength = TimeSpan.FromSeconds(responseJson.Value<int>("gameLength")),
                    Region = region.ToString(),
                    EncryptionKey = responseJson.Value<JObject>("observers").Value<string>("encryptionKey")
                };

                return Result.AsSuccess(gameInfo);
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return Result.AsWarning(Messages.SummonerNotInGame);
            }
            else if (response.StatusCode == (HttpStatusCode)429)
            {
                return Result.AsError(Messages.RateLimitExceeded);
            }
            else
            {
                return Result.AsError(Messages.UnexpectedError);
            }
        }
        #endregion

        #region Private Methods
        private HttpClient GetClient([CanBeNull]Region region = null)
        {
            var client = HttpClientFactory.Create(new ApiKeyMessageHandler(this._apiKey));
            client.BaseAddress = new Uri(string.Format("{0}://{1}.api.pvp.net/", Uri.UriSchemeHttps, region != null ? region.RiotApiPlatformId : "global"));

            return client;
        }
        #endregion
    }
}