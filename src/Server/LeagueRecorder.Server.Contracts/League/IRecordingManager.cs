using LeagueRecorder.Shared.League;

namespace LeagueRecorder.Server.Contracts.League
{
    public interface IRecordingManager
    {
        void Record(RiotSpectatorGameInfo gameInfo);
    }
}