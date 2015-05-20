using System.Threading.Tasks;
using LeagueRecorder.Shared.League;
using LeagueRecorder.Shared.Results;

namespace LeagueRecorder.Server.Contracts.League
{
    public interface ILeagueSpectatorApiClient
    {
        Task<Result<RiotGameMetaData>> GetGameMetaData(Region region, long gameId);
        Task<Result<RiotLastChunkInfo>> GetLastChunkInfo(Region region, long gameId);
        Task<Result<RiotChunk>> GetChunk(Region region, long gameId, int chunkId);
        Task<Result<RiotKeyFrame>> GetKeyFrame(Region region, long gameId, int keyFrameId);
    }
}