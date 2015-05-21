namespace LeagueRecorder.Shared.Entities
{
    public class GlobalConfiguration : AggregateRoot
    {
        public static string CreateId()
        {
            return "System/GlobalConfiguration";
        }

        public string LatestSpectatorClientVersion { get; set; }
    }
}