using System;

namespace LeagueRecorder.Shared.Entities
{
    public class Summoner : AggregateRoot
    {
        public static string CreateId(string region, long summonerId)
        {
            return string.Format("Summoners/{0}/{1}", region, summonerId);
        }

        public string Region { get; set; }
        public long SummonerId { get; set; }

        public string SummonerName { get; set; }

        public DateTimeOffset LastCheckIfInGameDate { get; set; }
    }
}