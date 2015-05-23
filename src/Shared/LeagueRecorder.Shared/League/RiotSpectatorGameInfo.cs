using System;

namespace LeagueRecorder.Shared.League
{
    public class RiotSpectatorGameInfo
    {
        public long GameId { get; set; }
        public TimeSpan GameLength { get; set; }
        public string Region { get; set; }
        public string EncryptionKey { get; set; }

        #region Equality Members
        protected bool Equals(RiotSpectatorGameInfo other)
        {
            return GameId == other.GameId && string.Equals(Region, other.Region);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RiotSpectatorGameInfo)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (GameId.GetHashCode() * 397) ^ (Region != null ? Region.GetHashCode() : 0);
            }
        }
        #endregion
    }
}