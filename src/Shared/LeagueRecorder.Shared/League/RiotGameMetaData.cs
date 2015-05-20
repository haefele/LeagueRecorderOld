using System;

namespace LeagueRecorder.Shared.League
{
    public class RiotGameMetaData
    {
        public long GameId { get; set; }
        public string Region { get; set; }
        public TimeSpan GameLength { get; set; }
        public int StartGameChunkId { get; set; }
        public int EndGameChunkId { get; set; }
        public int EndGameKeyFrameId { get; set; }
        public int EndStartupChunkId { get; set; }
        public int LastChunkId { get; set; }
        public int LastKeyFrameId { get; set; }
        public bool GameEnded { get; set; }

        public string OriginalJsonResponse { get; set; }
    }
}