using LeagueRecorder.Shared.League.Api;

namespace LeagueRecorder.Server.Contracts.Recordings
{
    public interface IGameRecorderSupervisor
    {
        void Record(RiotSpectatorGameInfo gameInfo);
        void RemoveRecording(RiotSpectatorGameInfo gameInfo);
    }
}