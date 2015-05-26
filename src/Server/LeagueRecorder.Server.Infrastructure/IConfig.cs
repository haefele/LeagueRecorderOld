using JetBrains.Annotations;

namespace LeagueRecorder.Server.Infrastructure
{
    public interface IConfig
    {
        string Url { get; }
        bool CompressResponses { get; }
        int IntervalToCheckForSummonersThatAreIngameInSeconds { get; }
        string RiotApiKey { get; }
        bool RecordGames { get; }
        int CountOfSummonersToCheckIfIngame { get; }
        int IntervalToCheckIfOneSummonerIsIngame { get; }
        int DurationRegionsAreMarkedAsUnavailableInSeconds { get; }
        string AzureStorageConnectionString { get; }
        string AzureStorageContainerName { get; }
    }
}