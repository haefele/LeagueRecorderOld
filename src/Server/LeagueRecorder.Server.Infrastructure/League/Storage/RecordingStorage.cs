using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using LeagueRecorder.Server.Contracts.League.Storage;
using LeagueRecorder.Server.Localization;
using LeagueRecorder.Shared;
using LeagueRecorder.Shared.Entities;
using LeagueRecorder.Shared.Files;
using LeagueRecorder.Shared.League;
using LeagueRecorder.Shared.League.Recording;
using LeagueRecorder.Shared.Results;
using LiteGuard;
using Raven.Client;
using Raven.Client.FileSystem;

namespace LeagueRecorder.Server.Infrastructure.League.Storage
{
    public class RecordingStorage : IRecordingStorage
    {
        #region Fields
        private readonly IDocumentStore _documentStore;
        private readonly IFilesStore _filesStore;
        #endregion

        public RecordingStorage([NotNull]IDocumentStore documentStore, [NotNull]IFilesStore filesStore)
        {
            Guard.AgainstNullArgument("documentStore", documentStore);
            Guard.AgainstNullArgument("filesStore", filesStore);

            this._documentStore = documentStore;
            this._filesStore = filesStore;
        }

        public Task<Result> SaveGameRecordingAsync(RiotRecording recording)
        {
            return Result.CreateAsync(async () =>
            {
                using (var session = this._documentStore.OpenAsyncSession())
                {
                    var storedRecording = new Shared.Entities.Recording
                    {
                        GameId = recording.Game.GameId,
                        Region = recording.Game.Region,
                        LeagueVersion = recording.LeagueVersion.ToString(),
                        SpectatorVersion = recording.SpectatorVersion.ToString(),
                        ReplayInformations = new ReplayInformations
                        {
                            ChunkTimeInterval = recording.GameMetaData.ChunkTimeInterval,
                            ClientAddedLag = recording.GameMetaData.ClientAddedLag,
                            CreateTime = recording.GameMetaData.CreateTime,
                            DelayTime = recording.GameMetaData.DelayTime,
                            EncryptionKey = recording.Game.EncryptionKey,
                            EndGameChunkId = recording.GameMetaData.EndGameChunkId,
                            EndGameKeyFrameId = recording.GameMetaData.EndGameKeyFrameId,
                            EndStartupChunkId = recording.GameMetaData.EndStartupChunkId,
                            KeyFrameTimeInterval = recording.GameMetaData.KeyFrameTimeInterval,
                            StartGameChunkId = recording.GameMetaData.StartGameChunkId
                        },
                        GameInformations = new GameInformations
                        {
                            StartTime = recording.GameMetaData.StartTime,
                            EndTime = recording.GameMetaData.EndTime,
                            GameLength = recording.GameMetaData.GameLength,
                            InterestScore = recording.GameMetaData.InterestScore
                        }
                    };
                    storedRecording.Id = Shared.Entities.Recording.CreateId(recording.Game.Region, recording.Game.GameId);

                    await session.StoreAsync(storedRecording).ConfigureAwait(false);
                    await session.SaveChangesAsync().ConfigureAwait(false);
                }

                using (var filesSession = this._filesStore.OpenAsyncSession())
                {
                    foreach (var chunk in recording.Chunks)
                    {
                        var fileName = Chunk.CreateId(recording.Game.Region, recording.Game.GameId, chunk.Id);
                        var stream = new MemoryStream(chunk.Data);

                        filesSession.RegisterUpload(fileName, stream);
                    }

                    foreach (var keyFrame in recording.KeyFrames)
                    {
                        var fileName = KeyFrame.CreateId(recording.Game.Region, recording.Game.GameId, keyFrame.Id);
                        var stream = new MemoryStream(keyFrame.Data);

                        filesSession.RegisterUpload(fileName, stream);
                    }

                    await filesSession.SaveChangesAsync().ConfigureAwait(false);
                }
            });
        }

        public async Task<Result<Shared.Entities.Recording>> GetRecordingAsync(Region region, long gameId)
        {
            using (var documentSession = this._documentStore.OpenAsyncSession())
            {
                var id = Shared.Entities.Recording.CreateId(region.ToString(), gameId);
                var recording = await documentSession.LoadAsync<Shared.Entities.Recording>(id).ConfigureAwait(false);

                if (recording == null)
                    return Result.AsError(Messages.GameNotFound);

                return Result.AsSuccess(recording);
            }
        }

        public async Task<Result<Stream>> GetChunkAsync(Region region, long gameId, int chunkId)
        {
            using (var filesSession = this._filesStore.OpenAsyncSession())
            {
                var id = Chunk.CreateId(region.ToString(), gameId, chunkId);
                var chunkFile = await filesSession.LoadFileAsync(id).ConfigureAwait(false);

                if (chunkFile == null)
                    return Result.AsError(Messages.ChunkNotFound);

                var fileStream = await filesSession.DownloadAsync(chunkFile).ConfigureAwait(false);
                return Result.AsSuccess(fileStream);
            }
        }

        public async Task<Result<Stream>> GetKeyFrameAsync(Region region, long gameId, int keyFrameId)
        {
            using (var filesSession = this._filesStore.OpenAsyncSession())
            {
                var id = KeyFrame.CreateId(region.ToString(), gameId, keyFrameId);
                var keyFrameFile = await filesSession.LoadFileAsync(id).ConfigureAwait(false);

                if (keyFrameFile == null)
                    return Result.AsError(Messages.KeyFrameNotFound);

                var fileStream = await filesSession.DownloadAsync(keyFrameFile).ConfigureAwait(false);
                return Result.AsSuccess(fileStream);
            }
        }
    }
}