namespace LeagueRecorder.Shared.Entities
{
    public class GlobalConfiguration : AggregateRoot
    {
        public string LatestSpectatorClientVersion { get; set; }
    }
}