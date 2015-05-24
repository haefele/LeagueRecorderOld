using System.IO;
using System.Threading.Tasks;
using LeagueRecorder.Shared.Entities;
using LeagueRecorder.Shared.League;
using LeagueRecorder.Shared.Results;

namespace LeagueRecorder.Server.Contracts.League
{
    public interface IRecordingManager
    {
        Task<Result> SaveGameRecordingAsync(RiotRecording recording);

        Task<Result<Recording>> GetRecordingAsync(Region region, long gameId);

        Task<Result<Stream>> GetChunkAsync(Region region, long gameId, int chunkId);

        Task<Result<Stream>> GetKeyFrameAsync(Region region, long gameId, int keyFrameId);
    }
}