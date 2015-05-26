using System;

namespace LeagueRecorder.Shared.League.Api
{
    public class RiotGameMetaData
    {
        public long GameId { get; set; }
        public string Region { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int InterestScore { get; set; }
        public TimeSpan GameLength { get; set; }
        public DateTime CreateTime { get; set; }
        public int StartGameChunkId { get; set; }
        public int EndGameChunkId { get; set; }
        public int EndGameKeyFrameId { get; set; }
        public int EndStartupChunkId { get; set; }
        public TimeSpan ChunkTimeInterval { get; set; }
        public TimeSpan KeyFrameTimeInterval { get; set; }
        public TimeSpan ClientAddedLag { get; set; }
        public TimeSpan DelayTime { get; set; }
    }
}