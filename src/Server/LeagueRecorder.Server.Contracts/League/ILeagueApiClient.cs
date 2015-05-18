using System.Threading.Tasks;
using LeagueRecorder.Shared.League;
using LeagueRecorder.Shared.Results;

namespace LeagueRecorder.Server.Contracts.League
{
    public interface ILeagueApiClient
    {
        Task<Result<Summoner>> GetSummonerBySummonerNameAsync(string summonerName);
    }
}