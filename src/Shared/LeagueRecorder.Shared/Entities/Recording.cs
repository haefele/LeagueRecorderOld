using LeagueRecorder.Shared.League;

namespace LeagueRecorder.Shared.Entities
{
    public class Recording : AggregateRoot
    {
        public long GameId { get; set; }
        public string Region { get; set; }

        public string LeagueVersion { get; set; }
        public string SpectatorVersion { get; set; }

        public GameInformations GameInformations { get; set; }
        public ReplayInformations ReplayInformations { get; set; }
    }
}