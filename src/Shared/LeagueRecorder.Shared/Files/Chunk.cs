namespace LeagueRecorder.Shared.Files
{
    public class Chunk
    {
        public static string CreateId(string region, long gameId, int chunkId)
        {
            return string.Format("Recording/{0}/{1}/Chunks/{2}", region, gameId, chunkId);
        }
    }

    public class KeyFrame
    {
        public static string CreateId(string region, long gameId, int keyFrameId)
        {
            return string.Format("Recording/{0}/{1}/KeyFrames/{2}", region, gameId, keyFrameId);
        }
    }
}