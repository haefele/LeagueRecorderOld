using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueRecorder.Server.Infrastructure.League;
using LeagueRecorder.Shared.League;
using Xunit;

namespace LeagueRecorder.Tests.Integration
{
    public class LeagueApiClientTests
    {
        public const string ApiKey = "4ad9233d-1f05-47f5-a5be-219adb783147";

        [Fact]
        public async Task GetLatestLeagueVersion()
        {
            var leagueApiClient = new LeagueApiClient(ApiKey);
            var result = await leagueApiClient.GetLeagueVersion(Region.EuropeWest);
        }

        [Fact]
        public async Task GetASummonerForSummonerName()
        {
            var leagueApiClient = new LeagueApiClient(ApiKey);
            var result = await leagueApiClient.GetSummonerBySummonerNameAsync(Region.EuropeWest, "haefele");
        }

        [Fact]
        public async Task GetCurrentGameInfoShouldWorkForSummonerThatIsIngame()
        {
            var leagueApiClient = new LeagueApiClient(ApiKey);
            var result = await leagueApiClient.GetCurrentGameAsync(Region.EuropeWest, 21762912);
        }
    }
}
