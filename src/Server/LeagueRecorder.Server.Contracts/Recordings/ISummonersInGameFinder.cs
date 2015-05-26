using System;

namespace LeagueRecorder.Server.Contracts.Recordings
{
    public interface ISummonersInGameFinder : IDisposable
    {
        #region Properties
        /// <summary>
        /// Gets a value indicating whether this instance started looking for summoners that are in game.
        /// </summary>
        bool IsStarted { get; }
        #endregion

        #region Methods
        /// <summary>
        /// Instructs this instance to start looking for summoners that are in game.
        /// </summary>
        void Start();
        #endregion
    }
}