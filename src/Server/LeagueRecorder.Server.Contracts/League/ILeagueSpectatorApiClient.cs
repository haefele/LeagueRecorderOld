using System.Threading.Tasks;
using LeagueRecorder.Shared.League;
using LeagueRecorder.Shared.Results;

namespace LeagueRecorder.Server.Contracts.League
{
    public interface ILeagueSpectatorApiClient
    {
        Task<Result<GameMetaData>> GetGameMetaData(Region region, long gameId);
        Task<Result<LastChunkInfo>> GetLastChunkInfo(Region region, long gameId);
    }
}