using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using JetBrains.Annotations;
using LeagueRecorder.Server.Contracts.League;
using LeagueRecorder.Server.Contracts.League.Api;
using LeagueRecorder.Server.Infrastructure.Extensions;
using LeagueRecorder.Server.Localization;
using LeagueRecorder.Shared;
using LeagueRecorder.Shared.Entities;
using LeagueRecorder.Shared.League;
using LeagueRecorder.Shared.League.Api;
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

        [HttpPost]
        [Route("Summoners/{region}/{summonerName}")]
        public async Task<HttpResponseMessage> AddSummonerAsync(string region, string summonerName)
        {
            Region actualRegion = Region.FromString(region);

            if (actualRegion == null || string.IsNullOrWhiteSpace(summonerName))
                return this.Request.GetMessageWithError(HttpStatusCode.BadRequest, Messages.InvalidArguments);

            Result<RiotSummoner> summoner = await this._leagueApiClient.GetSummonerBySummonerNameAsync(actualRegion, summonerName).ConfigureAwait(false);

            if (summoner.IsError)
                return this.Request.GetMessageWithError(HttpStatusCode.InternalServerError, summoner.Message);

            var summonerToStore = new Summoner
            {
                Region = actualRegion.ToString(),
                SummonerId = summoner.Data.Id,
                SummonerName = summoner.Data.Name
            };
            summonerToStore.Id = Summoner.CreateId(summonerToStore.Region, summonerToStore.SummonerId);

            await this.DocumentSession.StoreAsync(summonerToStore).ConfigureAwait(false);
            await this.DocumentSession.SaveChangesAsync().ConfigureAwait(false);

            var result = new
            {
                Region = actualRegion.ToString(), 
                SummonerId = summonerToStore.SummonerId, 
                SummonerName = summonerToStore.SummonerName
            };

            return this.Request.GetMessageWithObject(HttpStatusCode.Created, result);
        }

        [HttpPost]
        [Route("Summoners/Challengers/{region}")]
        public async Task<HttpResponseMessage> AddChallengersAsync(string region)
        {
            Region actualRegion = Region.FromString(region);

            if (actualRegion == null)
                return this.Request.GetMessageWithError(HttpStatusCode.BadRequest, Messages.InvalidArguments);

            Result<IList<RiotSummoner>> challengerResult = await _leagueApiClient.GetChallengerSummonersAsync(actualRegion).ConfigureAwait(false);

            if (challengerResult.IsError)
                return this.Request.GetMessageWithError(HttpStatusCode.InternalServerError, challengerResult.Message);

            foreach (RiotSummoner summoner in challengerResult.Data)
            {
                var summonerToStore = new Summoner
                {
                    Region = actualRegion.ToString(),
                    SummonerId = summoner.Id,
                    SummonerName = summoner.Name
                };
                summonerToStore.Id = Summoner.CreateId(summonerToStore.Region, summonerToStore.SummonerId);

                await this.DocumentSession.StoreAsync(summonerToStore).ConfigureAwait(false);
            }

            await this.DocumentSession.SaveChangesAsync().ConfigureAwait(false);

            return this.Request.GetMessage(HttpStatusCode.Created);
        }
    }
}