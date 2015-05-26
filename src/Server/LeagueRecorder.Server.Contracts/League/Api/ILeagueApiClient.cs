using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeagueRecorder.Shared;
using LeagueRecorder.Shared.League;
using LeagueRecorder.Shared.League.Api;
using LeagueRecorder.Shared.Results;

namespace LeagueRecorder.Server.Contracts.League.Api
{
    public interface ILeagueApiClient
    {
        Task<Result<Version>> GetLeagueVersion(Region region);

        Task<Result<RiotSummoner>> GetSummonerBySummonerNameAsync(Region region, string summonerName);

        Task<Result<RiotSpectatorGameInfo>> GetCurrentGameAsync(Region region, long summonerId);

        Task<Result<IList<RiotSummoner>>> GetChallengerSummonersAsync(Region region);
    }
}