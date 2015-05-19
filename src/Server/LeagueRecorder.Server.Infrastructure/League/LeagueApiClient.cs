using System;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using LeagueRecorder.Server.Contracts.League;
using LeagueRecorder.Shared.League;
using LeagueRecorder.Shared.Results;
using LiteGuard;
using Newtonsoft.Json.Linq;

namespace LeagueRecorder.Server.Infrastructure.League
{
    public class LeagueApiClient : ILeagueApiClient
    {
        #region Fields
        private readonly ApiKeyMessageHandler _apiKeyMessageHandler;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="LeagueApiClient"/> class.
        /// </summary>
        /// <param name="apiKey">The API key.</param>
        public LeagueApiClient(string apiKey)
        {
            Guard.AgainstNullArgument("apiKey", apiKey);

            this._apiKeyMessageHandler = new ApiKeyMessageHandler(apiKey);
        }
        #endregion

        #region Methods
        public async Task<Result<Summoner>> GetSummonerBySummonerNameAsync(Region region, string summonerName)
        {
            Guard.AgainstNullArgument("summonerName", summonerName);

            HttpResponseMessage response = await this.GetClient(region)
                .GetAsync(string.Format("api/lol/{0}/v1.4/summoner/by-name/{1}", region.RiotApiPlatformId, summonerName))
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var responseJson = JObject.Parse(responseString).Value<JObject>(summonerName);

                var summoner = new Summoner
                {
                    Id = responseJson.Value<long>("id"),
                    Name = responseJson.Value<string>("name"),
                    Region = region.ToString()
                };

                return Result.AsSuccess(summoner);
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return Result.AsError("Summoner not found.");
            }
            else
            {
                return Result.AsError("Unexpected error.");
            }
        }

        public async Task<Result<SpectatorGameInfo>> GetCurrentGame(Region region, long summonerId)
        {
            HttpResponseMessage response = await this.GetClient(region)
                .GetAsync(string.Format("observer-mode/rest/consumer/getSpectatorGameInfo/{0}/{1}", region.SpectatorPlatformId, summonerId))
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var responseJson = JObject.Parse(responseString);

                var gameInfo = new SpectatorGameInfo
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
                return Result.AsError("Summoner is currently not ingame.");
            }
            else
            {
                return Result.AsError("Unexpected error.");
            }
        }
        #endregion

        #region Private Methods
        private HttpClient GetClient(Region region)
        {
            var client = HttpClientFactory.Create(this._apiKeyMessageHandler);
            client.BaseAddress = new Uri(string.Format("{0}://{1}.api.pvp.net/", Uri.UriSchemeHttps, region.RiotApiPlatformId));

            return client;
        }
        #endregion
    }
}