using System.IO;
using System.Threading.Tasks;
using LeagueRecorder.Shared;
using LeagueRecorder.Shared.League.Recording;
using LeagueRecorder.Shared.Results;

namespace LeagueRecorder.Server.Contracts.Storage
{
    public interface IRecordingStorage
    {
        Task<Result> SaveGameRecordingAsync(RiotRecording recording);

        Task<Result<Shared.Entities.Recording>> GetRecordingAsync(Region region, long gameId);

        Task<Result<Stream>> GetChunkAsync(Region region, long gameId, int chunkId);

        Task<Result<Stream>> GetKeyFrameAsync(Region region, long gameId, int keyFrameId);
    }
}