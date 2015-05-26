using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using LeagueRecorder.Server.Contracts.Storage;
using LeagueRecorder.Server.Localization;
using LeagueRecorder.Shared;
using LeagueRecorder.Shared.Entities;
using LeagueRecorder.Shared.League.Recordings;
using LeagueRecorder.Shared.Results;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace LeagueRecorder.Server.Infrastructure.Storage
{
    public class AzureRecordingStorage : IRecordingStorage
    {
        #region Fields
        private readonly CloudBlobClient _blobClient;
        private readonly CloudTableClient _tableClient;
        private readonly IConfig _config;
        #endregion

        #region Constructors
        public AzureRecordingStorage([NotNull]CloudBlobClient blobClient, [NotNull]CloudTableClient tableClient, [NotNull]IConfig config)
        {
            this._blobClient = blobClient;
            this._tableClient = tableClient;
            this._config = config;
        }
        #endregion

        #region Methods
        public async Task<Result> SaveGameRecordingAsync(RiotRecording recording)
        {
            await this.SaveRecording(recording);

            var container = await this.GetContainerAsync(Region.FromString(recording.Game.Region));
            foreach (var chunk in recording.Chunks)
            {
                string fileName = this.CreateChunkFileName(recording.Game.Region, recording.Game.GameId, chunk.Id);
                var blob = await container.GetBlobReferenceFromServerAsync(fileName);

                await blob.UploadFromByteArrayAsync(chunk.Data, 0, chunk.Data.Length);
            }

            foreach (var keyFrame in recording.KeyFrames)
            {
                string fileName = this.CreateKeyFrameFileName(recording.Game.Region, recording.Game.GameId, keyFrame.Id);
                var blob = await container.GetBlobReferenceFromServerAsync(fileName);

                await blob.UploadFromByteArrayAsync(keyFrame.Data, 0, keyFrame.Data.Length);
            }

            return Result.AsSuccess();
        }

        public async Task<Result<Recording>> GetRecordingAsync(Region region, long gameId)
        {
            var tableReference = await this.GetTableReferenceAsync<RecordingEntity>();
            var entityResult = await tableReference.ExecuteAsync(TableOperation.Retrieve<RecordingEntity>(region.ToString(), gameId.ToString()));

            if (entityResult.Result == null)
                return Result.AsError(Messages.GameNotFound);

            string json = ((RecordingEntity)entityResult.Result).Data;
            var recording = JsonConvert.DeserializeObject<Recording>(json);

            return Result.AsSuccess(recording);
        }

        public async Task<Result<Stream>> GetChunkAsync(Region region, long gameId, int chunkId)
        {
            var container = await this.GetContainerAsync(region);

            var fileName = this.CreateChunkFileName(region.ToString(), gameId, chunkId);
            var chunkFile = await container.GetBlobReferenceFromServerAsync(fileName);

            bool exists = await chunkFile.ExistsAsync();
            if (exists == false)
                return Result.AsError(Messages.ChunkNotFound);

            var fileStream = await chunkFile.OpenReadAsync();
            return Result.AsSuccess(fileStream);
        }

        public async Task<Result<Stream>> GetKeyFrameAsync(Region region, long gameId, int keyFrameId)
        {
            var container = await this.GetContainerAsync(region);

            var fileName = this.CreateKeyFrameFileName(region.ToString(), gameId, keyFrameId);
            var keyFrameFile = await container.GetBlobReferenceFromServerAsync(fileName);

            bool exists = await keyFrameFile.ExistsAsync();
            if (exists == false)
                return Result.AsError(Messages.KeyFrameNotFound);

            var fileStream = await keyFrameFile.OpenReadAsync();
            return Result.AsSuccess(fileStream);
        }
        #endregion

        #region Private Methods
        private async Task<CloudBlobContainer> GetContainerAsync(Region region)
        {
            var container = this._blobClient.GetContainerReference(string.Format("{0}-{1}", region, this._config.AzureStorageContainerName));

            await container.CreateIfNotExistsAsync();

            return container;
        }

        private async Task<CloudTable> GetTableReferenceAsync<T>()
            where T : TableEntity
        {
            var tableReference = this._tableClient.GetTableReference(typeof (T).Name);
            await tableReference.CreateIfNotExistsAsync();

            return tableReference;
        }
        private async Task SaveRecording(RiotRecording recording)
        {
            var storedRecording = new Recording
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
            var json = JsonConvert.SerializeObject(storedRecording);

            var entity = new RecordingEntity(storedRecording.Region, storedRecording.GameId.ToString())
            {
                Data = json
            };

            var tableReference = await this.GetTableReferenceAsync<RecordingEntity>();
            await tableReference.ExecuteAsync(TableOperation.Insert(entity));
        }
        private string CreateChunkFileName(string region, long gameId, int chunkId)
        {
            return string.Format("{0}/{1}/Chunks/{2}", region, gameId, chunkId);
        }
        private string CreateKeyFrameFileName(string region, long gameId, int keyFrameId)
        {
            return string.Format("{0}/{1}/KeyFrames/{2}", region, gameId, keyFrameId);
        }
        #endregion

        #region Internal
        private class RecordingEntity : TableEntity
        {
            public RecordingEntity()
            {
            }

            public RecordingEntity(string partitionKey, string rowKey)
                : base(partitionKey, rowKey)
            {
            }

            public string Data { get; set; }
        }
        #endregion
    }
}