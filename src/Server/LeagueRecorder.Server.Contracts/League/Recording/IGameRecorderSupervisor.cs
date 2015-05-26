using LeagueRecorder.Shared.League;
using LeagueRecorder.Shared.League.Api;

namespace LeagueRecorder.Server.Contracts.League.Recording
{
    public interface IGameRecorderSupervisor
    {
        void Record(RiotSpectatorGameInfo gameInfo);
        void RemoveRecording(RiotSpectatorGameInfo gameInfo);
    }
}