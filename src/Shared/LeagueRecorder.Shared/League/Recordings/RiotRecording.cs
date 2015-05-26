using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using LeagueRecorder.Shared.League.Api;
using LiteGuard;

namespace LeagueRecorder.Shared.League.Recordings
{
    public class RiotRecording
    {
        public RiotRecording([NotNull]RiotSpectatorGameInfo gameInfo)
        {
            Guard.AgainstNullArgument("gameInfo", gameInfo);

            this.Game = gameInfo;

            this.Chunks = new List<RiotChunk>();
            this.KeyFrames = new List<RiotKeyFrame>();
        }

        public Version LeagueVersion { get; set; }
        public Version SpectatorVersion { get; set; }

        public RiotSpectatorGameInfo Game { get; set; }
        public RiotGameMetaData GameMetaData { get; set; }
        public RiotLastGameInfo GameInfo { get; set; }
        public IList<RiotChunk> Chunks { get; set; }
        public IList<RiotKeyFrame> KeyFrames { get; set; }

        #region Equality Members
        protected bool Equals(RiotRecording other)
        {
            return Equals(Game, other.Game);
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(null, obj)) return false;
            if (object.ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RiotRecording)obj);
        }

        public override int GetHashCode()
        {
            return this.Game.GetHashCode();
        }
        #endregion
    }
}