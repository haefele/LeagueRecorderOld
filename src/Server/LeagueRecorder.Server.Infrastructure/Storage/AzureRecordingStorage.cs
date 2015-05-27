using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;
using JetBrains.Annotations;
using LeagueRecorder.Server.Contracts.Storage;
using LeagueRecorder.Server.Infrastructure.Extensions;
using LeagueRecorder.Server.Localization;
using LeagueRecorder.Shared;
using LeagueRecorder.Shared.Entities;
using LeagueRecorder.Shared.League.Recordings;
using LeagueRecorder.Shared.Results;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using NHibernate;

namespace LeagueRecorder.Server.Infrastructure.Storage
{
    public class AzureRecordingStorage : IRecordingStorage
    {
        #region Fields
        private readonly CloudBlobClient _blobClient;
        private readonly ISessionFactory _sessionFactory;
        private readonly IConfig _config;
        #endregion

        #region Constructors
        public AzureRecordingStorage([NotNull]CloudBlobClient blobClient, [NotNull]ISessionFactory sessionFactory, [NotNull]IConfig config)
        {
            this._blobClient = blobClient;
            this._sessionFactory = sessionFactory;
            this._config = config;
        }
        #endregion

        #region Methods
        public async Task<Result> SaveGameRecordingAsync(RiotRecording recording)
        {
            this.SaveRecording(recording);

            var container = await this.GetContainerAsync(Region.FromString(recording.Game.Region));
            foreach (var chunk in recording.Chunks)
            {
                string fileName = this.CreateChunkFileName(recording.Game.Region, recording.Game.GameId, chunk.Id);
                var blob = container.GetBlockBlobReference(fileName);

                await blob.UploadFromByteArrayAsync(chunk.Data, 0, chunk.Data.Length);
            }

            foreach (var keyFrame in recording.KeyFrames)
            {
                string fileName = this.CreateKeyFrameFileName(recording.Game.Region, recording.Game.GameId, keyFrame.Id);
                var blob = container.GetBlockBlobReference(fileName);

                await blob.UploadFromByteArrayAsync(keyFrame.Data, 0, keyFrame.Data.Length);
            }

            return Result.AsSuccess();
        }

        public async Task<Result<Recording>> GetRecordingAsync(Region region, long gameId)
        {
            using (var session = this._sessionFactory.OpenSession())
            {
                var entity = session.Get<RecordingEntity>(this.CreateRecordingId(region.ToString(), gameId));

                if (entity == null)
                    return Result.AsError(Messages.GameNotFound);

                string json = entity.DataAsJson;
                var recording = JsonConvert.DeserializeObject<Recording>(json);

                return Result.AsSuccess(recording);
            }
        }

        public async Task<Result<Stream>> GetChunkAsync(Region region, long gameId, int chunkId)
        {
            var container = await this.GetContainerAsync(region);

            var fileName = this.CreateChunkFileName(region.ToString(), gameId, chunkId);
            var chunkFile = container.GetBlockBlobReference(fileName);

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
            var keyFrameFile = container.GetBlockBlobReference(fileName);

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
            var container = this._blobClient.GetContainerReference(this._config.AzureStorageContainerName);

            await container.CreateIfNotExistsAsync();

            return container;
        }
        private void SaveRecording(RiotRecording recording)
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

            var entity = new RecordingEntity
            {
                Id = this.CreateRecordingId(recording.Game.Region, recording.Game.GameId),
                DataAsJson = json
            };

            using (var session = this._sessionFactory.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                session.SaveOrUpdate(entity);

                transaction.Commit();
            }
        }

        private string CreateRecordingId(string region, long gameId)
        {
            return string.Format("{0}/{1}", region, gameId);
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
        private class RecordingEntity
        {
            public virtual string Id { get; set; }
            public virtual string DataAsJson { get; set; }
        }
        private class RecordingEntityMaps : ClassMap<RecordingEntity>
        {
            public RecordingEntityMaps()
            {
                Table("Recordings");

                Id(f => f.Id).GeneratedBy.Assigned();
                Map(f => f.DataAsJson).Not.Nullable().MaxLength();
            }
        }
        #endregion
    }
}