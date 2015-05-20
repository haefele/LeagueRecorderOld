using System.Threading.Tasks;
using LeagueRecorder.Shared.League;
using LeagueRecorder.Shared.Results;

namespace LeagueRecorder.Server.Contracts.League
{
    public interface ILeagueApiClient
    {
        Task<Result<RiotSummoner>> GetSummonerBySummonerNameAsync(Region region, string summonerName);

        Task<Result<RiotSpectatorGameInfo>> GetCurrentGameAsync(Region region, long summonerId);
    }
}