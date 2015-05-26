using LeagueRecorder.Shared.League.Api;

namespace LeagueRecorder.Server.Contracts.Recording
{
    public interface IGameRecorderSupervisor
    {
        void Record(RiotSpectatorGameInfo gameInfo);
        void RemoveRecording(RiotSpectatorGameInfo gameInfo);
    }
}