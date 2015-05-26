using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeagueRecorder.Shared.Entities;
using LeagueRecorder.Shared.Results;

namespace LeagueRecorder.Server.Contracts.Storage
{
    public interface ISummonerStorage
    {
        Task<Result> SaveSummonerAsync(Summoner summonerToStore);

        Task<Result<IList<Summoner>>> GetSummonersForInGameCheckAsync();
    }
}