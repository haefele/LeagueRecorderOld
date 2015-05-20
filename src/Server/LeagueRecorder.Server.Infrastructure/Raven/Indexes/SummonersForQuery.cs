using System.Linq;
using LeagueRecorder.Shared.Entities;
using Raven.Client.Indexes;

namespace LeagueRecorder.Server.Infrastructure.Raven.Indexes
{
    public class SummonersForQuery : AbstractIndexCreationTask<Summoner>
    {
        public SummonersForQuery()
        {
            this.Map = summoners => 
                from summoner in summoners
                select new
                {
                    summoner.LastCheckIfInGameDate
                };
        }

        public override string IndexName
        {
            get { return "Summoners/ForQuery"; }
        }
    }
}