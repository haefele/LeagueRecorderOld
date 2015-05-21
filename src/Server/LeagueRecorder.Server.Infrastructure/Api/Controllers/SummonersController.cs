using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using JetBrains.Annotations;
using LeagueRecorder.Server.Contracts.League;
using LeagueRecorder.Server.Infrastructure.Extensions;
using LeagueRecorder.Shared.Entities;
using LeagueRecorder.Shared.League;
using LeagueRecorder.Shared.Results;
using LiteGuard;
using Raven.Client;
using Raven.Client.FileSystem;

namespace LeagueRecorder.Server.Infrastructure.Api.Controllers
{
    public class SummonersController : BaseController
    {
        private readonly ILeagueApiClient _leagueApiClient;

        public SummonersController([NotNull] IAsyncDocumentSession documentSession, [NotNull] IAsyncFilesSession filesSession, [NotNull]ILeagueApiClient leagueApiClient)
            : base(documentSession, filesSession)
        {
            Guard.AgainstNullArgument("leagueApiClient", leagueApiClient);

            this._leagueApiClient = leagueApiClient;
        }

        [Route("Summoners/{region}/{summonerName}")]
        [HttpPost]
        public async Task<HttpResponseMessage> AddSummonerAsync(string region, string summonerName)
        {
            Region actualRegion = Region.FromString(region);

            if (actualRegion == null || string.IsNullOrWhiteSpace(summonerName))
                return this.Request.GetMessageWithError(HttpStatusCode.BadRequest, "");

            Result<RiotSummoner> summoner = await this._leagueApiClient.GetSummonerBySummonerNameAsync(actualRegion, summonerName);

            if (summoner.IsError)
                return this.Request.GetMessageWithError(HttpStatusCode.InternalServerError, summoner.Message);

            var summonerToStore = new Summoner
            {
                Region = actualRegion.ToString(),
                SummonerId = summoner.Data.Id,
                SummonerName = summoner.Data.Name
            };
            summonerToStore.Id = Summoner.CreateId(summonerToStore.Region, summonerToStore.SummonerId);

            await this.DocumentSession.StoreAsync(summonerToStore);
            await this.DocumentSession.SaveChangesAsync();

            var result = new
            {
                Region = actualRegion.ToString(), 
                SummonerId = summonerToStore.SummonerId, 
                SummonerName = summonerToStore.SummonerName
            };

            return this.Request.GetMessageWithObject(HttpStatusCode.Created, result);
        }
    }
}