using System;

namespace LeagueRecorder.Shared.Entities
{
    public class Recording : AggregateRoot
    {
        public static string CreateId(string region, long gameId)
        {
            return string.Format("Recordings/{0}/{1}", region, gameId);
        }

        public long GameId { get; set; }
        public string Region { get; set; }
        public string EncryptionKey { get; set; }

        public string LeagueVersion { get; set; }
        public string SpectatorVersion { get; set; }

        public string OriginalLastGameInfoJsonResponse { get; set; }
        public string OriginalGameMetaDataJsonResponse { get; set; }
    }
}