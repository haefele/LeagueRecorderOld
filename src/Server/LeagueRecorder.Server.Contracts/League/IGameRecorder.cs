using LeagueRecorder.Shared.League;

namespace LeagueRecorder.Server.Contracts.League
{
    public interface IGameRecorder
    {
        void Record(RiotSpectatorGameInfo gameInfo);
    }
}