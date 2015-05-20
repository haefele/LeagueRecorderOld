using System.Threading.Tasks;
using LeagueRecorder.Server.Infrastructure.League;
using LeagueRecorder.Shared.League;
using LeagueRecorder.Shared.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace LeagueRecorder.Tests.Integration
{
    public class LeagueSpectatorApiClientTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public LeagueSpectatorApiClientTests(ITestOutputHelper outputHelper)
        {
            this._outputHelper = outputHelper;
        }

        [Fact]
        public void GetGameMetaData()
        {
            var apiClient = new LeagueApiClient("4ad9233d-1f05-47f5-a5be-219adb783147");
            var spectatorApiClient = new LeagueSpectatorApiClient();

            var summoner = apiClient.GetSummonerBySummonerNameAsync(Region.EuropeWest, "NightSniper").Result;
            var currentGame = apiClient.GetCurrentGameAsync(Region.FromString(summoner.Data.Region), summoner.Data.Id).Result;

            var result = spectatorApiClient.GetGameMetaData(Region.EuropeWest, currentGame.Data.GameId).Result;

            Assert.Equal(ResultState.Success, result.State);
            Assert.NotNull(result.Data);

            _outputHelper.WriteLine(JObject.FromObject(result.Data).ToString(Formatting.Indented));
        }
    }
}