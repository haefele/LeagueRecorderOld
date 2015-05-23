namespace LeagueRecorder.Shared.League
{
    public class RiotLastGameInfo
    {
        public int CurrentChunkId { get; set; }
        public int CurrentKeyFrameId { get; set; }
        public string OriginalJsonResponse { get; set; }

        public int EndGameChunkId { get; set; }
    }
}