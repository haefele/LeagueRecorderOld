using System;
using System.Collections.Generic;
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

            this.RememberToReturnStartupChunkInfo(this.Request, recordingResult.Data);

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
                gameEnded = false,
                lastChunkId = -1,
                lastKeyFrameId = -1,
                endStartupChunkId = 0,
                delayTime = recordingResult.Data.ReplayInformations.DelayTime.TotalMilliseconds,
                pendingAvailableChunkInfo = (object)null,
                pendingAvailableKeyFrameInfo = (object)null,
                keyFrameTimeInterval = recordingResult.Data.ReplayInformations.KeyFrameTimeInterval.TotalMilliseconds,
                decodedEncryptionKey = string.Empty,
                startGameChunkId = 0,
                gameLength = 0,
                clientAddedLag = recordingResult.Data.ReplayInformations.ChunkTimeInterval.TotalMilliseconds,
                clientBackFetchingEnabled = false,
                clientBackFetchingFreq = 1000,
                interestScore = recordingResult.Data.GameInformations.InterestScore,
                featuredGame = false,
                createTime = recordingResult.Data.ReplayInformations.CreateTime.ToLeagueTime(),
                endGameChunkId = -1,
                endGameKeyFrameId = -1,
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

            var returnStartupChunkInfo = this.ShouldReturnStartupChunkInfo(this.Request, recordingResult.Data);
            
            var lastChunkInfo = new
            {
                chunkId = returnStartupChunkInfo 
                    ? recordingResult.Data.ReplayInformations.StartGameChunkId 
                    : recordingResult.Data.ReplayInformations.EndGameChunkId,
                availableSince = 30000,
                nextAvailableChunk = returnStartupChunkInfo 
                    ? 1000 
                    : 0,
                keyFrameId = returnStartupChunkInfo 
                    ? 1 
                    : recordingResult.Data.ReplayInformations.EndGameKeyFrameId,
                nextChunkId = returnStartupChunkInfo 
                    ? recordingResult.Data.ReplayInformations.StartGameChunkId 
                    : recordingResult.Data.ReplayInformations.EndGameChunkId,
                endStartupChunkId = recordingResult.Data.ReplayInformations.EndStartupChunkId,
                startGameChunkId = recordingResult.Data.ReplayInformations.StartGameChunkId,
                endGameChunkId = returnStartupChunkInfo 
                    ? 0 
                    : recordingResult.Data.ReplayInformations.EndGameChunkId,
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

        #region Private Methods
        private static readonly Dictionary<Tuple<string, long, string>, int> _clientsThatNeedStartupChunkInfo = new Dictionary<Tuple<string, long, string>, int>(); 

        private void RememberToReturnStartupChunkInfo(HttpRequestMessage request, Recording recording)
        {
            var clientIp = request.GetOwinContext().Request.RemoteIpAddress;
            var client = Tuple.Create(recording.Region, recording.GameId, clientIp);

            _clientsThatNeedStartupChunkInfo[client] = 0;
        }

        private bool ShouldReturnStartupChunkInfo(HttpRequestMessage request, Recording recording)
        {
            var clientIp = request.GetOwinContext().Request.RemoteIpAddress;
            var client = Tuple.Create(recording.Region, recording.GameId, clientIp);

            bool returnStartupChunkInfo = _clientsThatNeedStartupChunkInfo.ContainsKey(client);

            if (returnStartupChunkInfo)
            {
                _clientsThatNeedStartupChunkInfo[client]++;

                double amountOfChunksTheLoadingScreenTook = recording.ReplayInformations.StartGameChunkId - recording.ReplayInformations.EndStartupChunkId - 1;
                int estimatedTimeInSecondsOnLoadingScreen = (int)(amountOfChunksTheLoadingScreenTook * recording.ReplayInformations.ChunkTimeInterval.TotalSeconds);
                
                if (_clientsThatNeedStartupChunkInfo[client] >= estimatedTimeInSecondsOnLoadingScreen)
                {
                    _clientsThatNeedStartupChunkInfo.Remove(client);
                }
            }
            return returnStartupChunkInfo;
        }
        #endregion
    }
}