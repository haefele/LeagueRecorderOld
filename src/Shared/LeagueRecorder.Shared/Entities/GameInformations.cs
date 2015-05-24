using System;

namespace LeagueRecorder.Shared.Entities
{
    public class GameInformations
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int InterestScore { get; set; }
        public TimeSpan GameLength { get; set; }
    }
}