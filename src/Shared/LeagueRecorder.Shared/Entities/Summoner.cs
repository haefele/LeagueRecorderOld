using System;

namespace LeagueRecorder.Shared.Entities
{
    public class Summoner : AggregateRoot
    {
        public string Region { get; set; }
        public long SummonerId { get; set; }

        public string SummonerName { get; set; }

        public DateTimeOffset LastCheckIfInGameDate { get; set; }
    }
}