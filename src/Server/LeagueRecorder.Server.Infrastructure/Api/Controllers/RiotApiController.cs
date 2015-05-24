using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Anotar.NLog;
using JetBrains.Annotations;
using LeagueRecorder.Server.Contracts.League;
using LeagueRecorder.Server.Infrastructure.Extensions;
using LeagueRecorder.Shared;
using LeagueRecorder.Shared.Entities;
using LeagueRecorder.Shared.Files;
using LeagueRecorder.Shared.League;
using LeagueRecorder.Shared.Results;
using LiteGuard;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Raven.Client;
using Raven.Client.FileSystem;
using Raven.Database.Util;

namespace LeagueRecorder.Server.Infrastructure.Api.Controllers
{
    public class RiotApiController : BaseController
    {
        private readonly IRecordingManager _recordingManager;

        public RiotApiController([NotNull] IAsyncDocumentSession documentSession, [NotNull] IAsyncFilesSession filesSession, [NotNull]IRecordingManager recordingManager)
            : base(documentSession, filesSession)
        {
            Guard.AgainstNullArgument("recordingManager", recordingManager);

            this._recordingManager = recordingManager;
        }

        [HttpGet]
        [Route("observer-mode/rest/consumer/version")]
        public HttpResponseMessage GetVersion()
        {
            var message = this.Request.GetMessage(HttpStatusCode.OK);
            message.Content = new StringContent("1.82.89");

            return message;
        }

        [HttpGet]
        [Route("observer-mode/rest/consumer/getGameMetaData/{spectatorRegionId}/{gameId}/{token}/token")]
        public async Task<HttpResponseMessage> GetGameMetaData(string spectatorRegionId, long gameId)
        {
            var region = Region.FromString(spectatorRegionId);

            Result<Recording> recordingResult = await this._recordingManager.GetRecordingAsync(region, gameId).ConfigureAwait(false);

            if (recordingResult.IsError)
                return this.Request.GetMessage(HttpStatusCode.NotFound);

            var metaData = new
            {
                gameKey = new
                {
                    gameId = recordingResult.Data.GameId,
                    PlatformID = region.SpectatorPlatformId
                },
                gameServerAddress = string.Empty,
                port = 0,
                encryptionKey = string.Empty,
                chunkTimeInterval = recordingResult.Data.ReplayInformations.ChunkTimeInterval.TotalMilliseconds,
                startTime = recordingResult.Data.GameInformations.StartTime.ToLeagueTime(),
                endTime = recordingResult.Data.GameInformations.EndTime.ToLeagueTime(),
                gameEnded = true,
                lastChunkId = recordingResult.Data.ReplayInformations.EndGameChunkId,
                lastKeyFrameId = recordingResult.Data.ReplayInformations.EndGameKeyFrameId,
                endStartupChunkId = recordingResult.Data.ReplayInformations.EndStartupChunkId,
                delayTime = recordingResult.Data.ReplayInformations.DelayTime.TotalMilliseconds,
                pendingAvailableChunkInfo = (object)null,
                pendingAvailableKeyFrameInfo = (object)null,
                keyFrameTimeInterval = recordingResult.Data.ReplayInformations.KeyFrameTimeInterval.TotalMilliseconds,
                decodedEncryptionKey = string.Empty,
                startGameChunkId = recordingResult.Data.ReplayInformations.StartGameChunkId,
                gameLength = recordingResult.Data.GameInformations.GameLength.TotalMilliseconds,
                clientAddedLag = recordingResult.Data.ReplayInformations.ChunkTimeInterval.TotalMilliseconds,
                clientBackFetchingEnabled = true,
                clientBackFetchingFreq = 50,
                interestScore = recordingResult.Data.GameInformations.InterestScore,
                featuredGame = false,
                createTime = recordingResult.Data.ReplayInformations.CreateTime.ToLeagueTime(),
                endGameChunkId = recordingResult.Data.ReplayInformations.EndGameChunkId,
                endGameKeyFrameId = recordingResult.Data.ReplayInformations.EndGameKeyFrameId
            };

            var result = this.Request.GetMessage(HttpStatusCode.OK);
            result.Content = new StringContent(JsonConvert.SerializeObject(metaData), Encoding.UTF8, "application/json");

            return result;
        }

        [HttpGet]
        [Route("observer-mode/rest/consumer/getLastChunkInfo/{spectatorRegionId}/{gameId}/{token}/token")]
        public async Task<HttpResponseMessage> GetLastChunkInfo(string spectatorRegionId, long gameId)
        {
            var region = Region.FromString(spectatorRegionId);

            Result<Recording> recordingResult = await this._recordingManager.GetRecordingAsync(region, gameId).ConfigureAwait(false);

            if (recordingResult.IsError)
                return this.Request.GetMessage(HttpStatusCode.NotFound);

            var lastChunkInfo = new
            {
                chunkId = recordingResult.Data.ReplayInformations.EndGameChunkId,
                availableSince = 30000,
                nextAvailableChunk = 0,
                keyFrameId = recordingResult.Data.ReplayInformations.EndGameKeyFrameId,
                nextChunkId = recordingResult.Data.ReplayInformations.EndGameChunkId,
                endStartupChunkId = recordingResult.Data.ReplayInformations.EndStartupChunkId,
                startGameChunkId = recordingResult.Data.ReplayInformations.StartGameChunkId,
                endGameChunkId = recordingResult.Data.ReplayInformations.EndGameChunkId,
                duration = 30000
            };

            var result = this.Request.GetMessage(HttpStatusCode.OK);
            result.Content = new StringContent(JsonConvert.SerializeObject(lastChunkInfo), Encoding.UTF8, "application/json");

            return result;
        }

        [HttpGet]
        [Route("observer-mode/rest/consumer/getGameDataChunk/{spectatorRegionId}/{gameId}/{chunkId}/token")]
        public async Task<HttpResponseMessage> GetChunk(string spectatorRegionId, long gameId, int chunkId)
        {
            var region = Region.FromString(spectatorRegionId);

            Result<Recording> recordingResult = await this._recordingManager.GetRecordingAsync(region, gameId).ConfigureAwait(false);

            if (recordingResult.IsError)
                return this.Request.GetMessage(HttpStatusCode.NotFound);

            Result<Stream> chunkResult = await this._recordingManager.GetChunkAsync(region, gameId, chunkId).ConfigureAwait(false);

            if (chunkResult.IsError)
                return this.Request.GetMessage(HttpStatusCode.NotFound);

            var result = this.Request.GetMessage(HttpStatusCode.OK);
            result.Content = new StreamContent(chunkResult.Data);

            return result;
        }

        [HttpGet]
        [Route("observer-mode/rest/consumer/getKeyFrame/{spectatorRegionId}/{gameId}/{keyFrameId}/token")]
        public async Task<HttpResponseMessage> GetKeyFrame(string spectatorRegionId, long gameId, int keyFrameId)
        {
            var region = Region.FromString(spectatorRegionId);

            Result<Recording> recordingResult = await this._recordingManager.GetRecordingAsync(region, gameId).ConfigureAwait(false);

            if (recordingResult.IsError)
                return this.Request.GetMessage(HttpStatusCode.NotFound);

            Result<Stream> keyFrameResult = await this._recordingManager.GetKeyFrameAsync(region, gameId, keyFrameId).ConfigureAwait(false);

            if (keyFrameResult.IsError)
                return this.Request.GetMessage(HttpStatusCode.NotFound);

            var result = this.Request.GetMessage(HttpStatusCode.OK);
            result.Content = new StreamContent(keyFrameResult.Data);

            return result;
        }
    }
}