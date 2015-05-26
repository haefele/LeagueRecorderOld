using System;
using System.Threading.Tasks;
using LeagueRecorder.Shared;
using LeagueRecorder.Shared.League;
using LeagueRecorder.Shared.League.Api;
using LeagueRecorder.Shared.Results;

namespace LeagueRecorder.Server.Contracts.League.Api
{
    public interface ILeagueSpectatorApiClient
    {
        Task<Result<Version>> GetSpectatorVersion(Region region);
        Task<Result<RiotGameMetaData>> GetGameMetaData(Region region, long gameId);
        Task<Result<RiotLastGameInfo>> GetLastGameInfo(Region region, long gameId);
        Task<Result<RiotChunk>> GetChunk(Region region, long gameId, int chunkId);
        Task<Result<RiotKeyFrame>> GetKeyFrame(Region region, long gameId, int keyFrameId);
    }
}