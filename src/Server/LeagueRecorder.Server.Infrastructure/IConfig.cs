namespace LeagueRecorder.Server.Infrastructure
{
    public interface IConfig
    {
        string Url { get; }
        bool CompressResponses { get; }
        int IntervalToCheckForSummonersThatAreIngameInSeconds { get; }
        bool EnableRavenHttpServer { get; }
        int RavenHttpServerPort { get; }
        string RavenName { get; }
        string RiotApiKey { get; }
    }
}