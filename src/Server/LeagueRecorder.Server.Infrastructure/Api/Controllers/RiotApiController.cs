using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Anotar.NLog;
using JetBrains.Annotations;
using LeagueRecorder.Server.Infrastructure.Extensions;
using LeagueRecorder.Shared.Entities;
using LeagueRecorder.Shared.Files;
using LeagueRecorder.Shared.League;
using Raven.Client;
using Raven.Client.FileSystem;

namespace LeagueRecorder.Server.Infrastructure.Api.Controllers
{
    public class RiotApiController : BaseController
    {
        public RiotApiController([NotNull] IAsyncDocumentSession documentSession, [NotNull] IAsyncFilesSession filesSession)
            : base(documentSession, filesSession)
        {
        }

        [HttpGet]
        [Route("observer-mode/rest/consumer/version")]
        public HttpResponseMessage GetVersion()
        {
            var message = this.Request.GetMessage(HttpStatusCode.OK);
            message.Content = new StringContent("1.82.80");

            LogTo.Debug("Version");

            return message;
        }

        [HttpGet]
        [Route("observer-mode/rest/consumer/getGameMetaData/{region}/{gameId}/{token}/token")]
        public async Task<HttpResponseMessage> GetGameMetaData(string region, long gameId)
        {
            var actualRegion = Region.FromString(region);

            Recording game = await this.DocumentSession.LoadAsync<Recording>(Recording.CreateId(actualRegion.ToString(), gameId));

            if (game == null)
                return this.Request.GetMessage(HttpStatusCode.NotFound);

            LogTo.Debug("GameMetaData");

            var result = this.Request.GetMessage(HttpStatusCode.OK);
            result.Content = new StringContent(game.OriginalGameMetaDataJsonResponse, Encoding.UTF8, "application/json");

            return result;
        }

        [HttpGet]
        [Route("observer-mode/rest/consumer/getLastChunkInfo/{region}/{gameId}/{token}/token")]
        public async Task<HttpResponseMessage> GetLastChunkInfo(string region, long gameId)
        {
            var actualRegion = Region.FromString(region);

            Recording game = await this.DocumentSession.LoadAsync<Recording>(Recording.CreateId(actualRegion.ToString(), gameId));

            if (game == null)
                return this.Request.GetMessage(HttpStatusCode.NotFound);

            LogTo.Debug("LastChunkInfo");

            var result = this.Request.GetMessage(HttpStatusCode.OK);
            result.Content = new StringContent(game.OriginalLastGameInfoJsonResponse, Encoding.UTF8, "application/json");

            return result;
        }

        [HttpGet]
        [Route("observer-mode/rest/consumer/getGameDataChunk/{region}/{gameId}/{chunkId}/token")]
        public async Task<HttpResponseMessage> GetChunk(string region, long gameId, int chunkId)
        {
            var actualRegion = Region.FromString(region);

            Recording game = await this.DocumentSession.LoadAsync<Recording>(Recording.CreateId(actualRegion.ToString(), gameId));

            if (game == null)
                return this.Request.GetMessage(HttpStatusCode.NotFound);

            LogTo.Debug("Chunk {0}", chunkId);

            var stream = await this.FilesSession.DownloadAsync(Chunk.CreateId(actualRegion.ToString(), gameId, chunkId));

            var result = this.Request.GetMessage(HttpStatusCode.OK);
            result.Content = new StreamContent(stream);

            return result;
        }

        [HttpGet]
        [Route("observer-mode/rest/consumer/getKeyFrame/{region}/{gameId}/{keyFrameId}/token")]
        public async Task<HttpResponseMessage> GetKeyFrame(string region, long gameId, int keyFrameId)
        {
            var actualRegion = Region.FromString(region);

            Recording game = await this.DocumentSession.LoadAsync<Recording>(Recording.CreateId(actualRegion.ToString(), gameId));

            if (game == null)
                return this.Request.GetMessage(HttpStatusCode.NotFound);

            LogTo.Debug("Keyframe {0}", keyFrameId);

            var stream = await this.FilesSession.DownloadAsync(KeyFrame.CreateId(actualRegion.ToString(), gameId, keyFrameId));

            var result = this.Request.GetMessage(HttpStatusCode.OK);
            result.Content = new StreamContent(stream);

            return result;
        }
    }
}