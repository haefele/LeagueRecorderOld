using System;

namespace LeagueRecorder.Shared.Entities
{
    public class ReplayInformations
    {
        public string EncryptionKey { get; set; }
        
        public DateTime CreateTime { get; set; }

        public int EndStartupChunkId { get; set; }
        public int StartGameChunkId { get; set; }
        public int EndGameChunkId { get; set; }
        public int EndGameKeyFrameId { get; set; }

        public TimeSpan ChunkTimeInterval { get; set; }
        public TimeSpan KeyFrameTimeInterval { get; set; }
        public TimeSpan ClientAddedLag { get; set; }
        public TimeSpan DelayTime { get; set; }
    }
}