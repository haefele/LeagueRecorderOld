using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using JetBrains.Annotations;
using LeagueRecorder.Server.Contracts.LeagueApi;
using LeagueRecorder.Server.Contracts.Storage;
using LeagueRecorder.Server.Infrastructure.Extensions;
using LeagueRecorder.Server.Localization;
using LeagueRecorder.Shared;
using LeagueRecorder.Shared.Entities;
using LeagueRecorder.Shared.League.Api;
using LeagueRecorder.Shared.Results;
using LiteGuard;

namespace LeagueRecorder.Server.Infrastructure.Api.Controllers
{
    public class SummonersController : BaseController
    {
        private readonly ILeagueApiClient _leagueApiClient;
        private readonly ISummonerStorage _summonerStorage;

        public SummonersController([NotNull]ILeagueApiClient leagueApiClient, [NotNull]ISummonerStorage summonerStorage)
        {
            Guard.AgainstNullArgument("leagueApiClient", leagueApiClient);
            Guard.AgainstNullArgument("summonerStorage", summonerStorage);

            this._leagueApiClient = leagueApiClient;
            this._summonerStorage = summonerStorage;
        }

        [HttpPost]
        [Route("Summoners/{region}/{summonerName}")]
        public async Task<HttpResponseMessage> AddSummonerAsync(string region, string summonerName)
        {
            Region actualRegion = Region.FromString(region);

            if (actualRegion == null || string.IsNullOrWhiteSpace(summonerName))
                return this.Request.GetMessageWithError(HttpStatusCode.BadRequest, Messages.InvalidArguments);

            Result<RiotSummoner> summoner = await this._leagueApiClient.GetSummonerBySummonerNameAsync(actualRegion, summonerName);

            if (summoner.IsError)
                return this.Request.GetMessageWithError(HttpStatusCode.InternalServerError, summoner.Message);

            var summonerToStore = new Summoner
            {
                Region = actualRegion.ToString(),
                SummonerId = summoner.Data.Id,
                SummonerName = summoner.Data.Name,
                LastCheckIfInGameDate = DateTimeOffset.UtcNow
            };
            await this._summonerStorage.SaveSummonerAsync(summonerToStore);
            
            var result = new
            {
                Region = actualRegion.ToString(), 
                SummonerId = summonerToStore.SummonerId, 
                SummonerName = summonerToStore.SummonerName
            };

            return this.Request.GetMessageWithObject(HttpStatusCode.Created, result);
        }

        [HttpPost]
        [Route("Summoners/{region}/Challengers")]
        public async Task<HttpResponseMessage> AddChallengersAsync(string region)
        {
            Region actualRegion = Region.FromString(region);

            if (actualRegion == null)
                return this.Request.GetMessageWithError(HttpStatusCode.BadRequest, Messages.InvalidArguments);

            Result<IList<RiotSummoner>> challengerResult = await _leagueApiClient.GetChallengerSummonersAsync(actualRegion);

            if (challengerResult.IsError)
                return this.Request.GetMessageWithError(HttpStatusCode.InternalServerError, challengerResult.Message);

            foreach (RiotSummoner summoner in challengerResult.Data)
            {
                var summonerToStore = new Summoner
                {
                    Region = actualRegion.ToString(),
                    SummonerId = summoner.Id,
                    SummonerName = summoner.Name,
                    LastCheckIfInGameDate = DateTimeOffset.UtcNow,
                };

                var saveResult = await this._summonerStorage.SaveSummonerAsync(summonerToStore);

                if (saveResult.IsError)
                    return this.Request.GetMessageWithError(HttpStatusCode.InternalServerError, saveResult.Message);
            }

            return this.Request.GetMessage(HttpStatusCode.Created);
        }
    }
}