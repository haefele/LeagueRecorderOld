namespace LeagueRecorder.Server.Infrastructure
{
    public interface IConfig
    {
        string Url { get; }
        bool CompressResponses { get; }
    }
}