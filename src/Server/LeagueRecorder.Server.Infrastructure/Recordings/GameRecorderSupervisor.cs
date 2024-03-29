﻿using System;
using System.Collections.Concurrent;
using System.Timers;
using Anotar.NLog;
using JetBrains.Annotations;
using LeagueRecorder.Server.Contracts.LeagueApi;
using LeagueRecorder.Server.Contracts.Recordings;
using LeagueRecorder.Server.Contracts.Storage;
using LeagueRecorder.Shared.League.Api;
using LeagueRecorder.Shared.League.Recordings;
using LiteGuard;

namespace LeagueRecorder.Server.Infrastructure.Recordings
{
    public class GameRecorderSupervisor : IGameRecorderSupervisor, IDisposable
    {
        #region Fields
        private readonly ILeagueSpectatorApiClient _spectatorApiClient;
        private readonly ILeagueApiClient _leagueApiClient;
        private readonly IRecordingStorage _recordingStorage;

        private readonly ConcurrentDictionary<RiotRecording, GameRecorder> _recordings;
        private readonly Timer _timer;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GameRecorderSupervisor" /> class.
        /// </summary>
        /// <param name="spectatorApiClient">The spectator API client.</param>
        /// <param name="leagueApiClient">The league API client.</param>
        /// <param name="recordingStorage">The recording manager.</param>
        public GameRecorderSupervisor([NotNull]ILeagueSpectatorApiClient spectatorApiClient, [NotNull]ILeagueApiClient leagueApiClient, [NotNull]IRecordingStorage recordingStorage)
        {
            Guard.AgainstNullArgument("spectatorApiClient", spectatorApiClient);
            Guard.AgainstNullArgument("leagueApiClient", leagueApiClient);
            Guard.AgainstNullArgument("RecordingStorage", recordingStorage);

            this._spectatorApiClient = spectatorApiClient;
            this._leagueApiClient = leagueApiClient;
            this._recordingStorage = recordingStorage;
            
            this._recordings = new ConcurrentDictionary<RiotRecording, GameRecorder>();

            this._timer = new Timer();
            this._timer.Interval = TimeSpan.FromSeconds(30).TotalMilliseconds;
            this._timer.Elapsed += this.TimerOnElapsed;

            this._timer.Start();
        }
        #endregion

        #region Methods
        public void Record(RiotSpectatorGameInfo gameInfo)
        {
            var recordingData = new RiotRecording(gameInfo);

            if (this._recordings.ContainsKey(recordingData) == false)
            {
                LogTo.Info("Recording game {0} {1}.", gameInfo.Region, gameInfo.GameId);

                var recorder = new GameRecorder(this._spectatorApiClient, this._leagueApiClient, this._recordingStorage, this, recordingData);
                this._recordings.TryAdd(recordingData, recorder);
            }
        }

        public void RemoveRecording(RiotSpectatorGameInfo gameInfo)
        {
            var recordingData = new RiotRecording(gameInfo);

            GameRecorder recorder;
            if (this._recordings.TryRemove(recordingData, out recorder))
            {
                LogTo.Info("Throwing game away {0} {1}.", gameInfo.Region, gameInfo.GameId);

                recorder.Dispose();
            }
        }
        #endregion

        #region Private Methods
        private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            LogTo.Info("Currently recording {0} games.", this._recordings.Keys.Count);
        }
        #endregion

        #region Implementation of IDisposable
        public void Dispose()
        {
            this._timer.Dispose();
        }
        #endregion
    }
}