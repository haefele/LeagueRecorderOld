using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Anotar.NLog;
using JetBrains.Annotations;
using LeagueRecorder.Server.Contracts.League;
using LeagueRecorder.Shared;
using LeagueRecorder.Shared.League;
using LeagueRecorder.Shared.Results;
using LiteGuard;

namespace LeagueRecorder.Server.Infrastructure.League
{
    public class GameRecorder : IGameRecorder
    {
        #region Fields
        private readonly ILeagueSpectatorApiClient _spectatorApiClient;
        private readonly ILeagueApiClient _leagueApiClient;
        private readonly IRecordingManager _recordingManager;

        private readonly ConcurrentDictionary<RiotRecording, SingleGameRecorder> _recordings;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="GameRecorder" /> class.
        /// </summary>
        /// <param name="spectatorApiClient">The spectator API client.</param>
        /// <param name="leagueApiClient">The league API client.</param>
        /// <param name="recordingManager">The recording manager.</param>
        public GameRecorder([NotNull]ILeagueSpectatorApiClient spectatorApiClient, [NotNull]ILeagueApiClient leagueApiClient, [NotNull]IRecordingManager recordingManager)
        {
            Guard.AgainstNullArgument("spectatorApiClient", spectatorApiClient);
            Guard.AgainstNullArgument("leagueApiClient", leagueApiClient);
            Guard.AgainstNullArgument("recordingManager", recordingManager);

            this._spectatorApiClient = spectatorApiClient;
            this._leagueApiClient = leagueApiClient;
            this._recordingManager = recordingManager;
            
            this._recordings = new ConcurrentDictionary<RiotRecording, SingleGameRecorder>();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Records the specified game information.
        /// </summary>
        /// <param name="gameInfo">The game information.</param>
        public void Record(RiotSpectatorGameInfo gameInfo)
        {
            var recordingData = new RiotRecording(gameInfo);

            if (this._recordings.ContainsKey(recordingData) == false)
            {
                LogTo.Info("Recording game {0} {1}.", gameInfo.Region, gameInfo.GameId);

                var recorder = new SingleGameRecorder(this._spectatorApiClient, this._leagueApiClient, this._recordingManager, this, recordingData);
                this._recordings.TryAdd(recordingData, recorder);
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Removes the recording.
        /// </summary>
        /// <param name="recording">The recording.</param>
        private void RemoveRecording(RiotRecording recording)
        {
            SingleGameRecorder recorder;
            if (this._recordings.TryRemove(recording, out recorder))
            {
                recorder.Dispose();
            }
        }
        #endregion

        #region Internal
        private class SingleGameRecorder : IDisposable
        {
            #region Fields
            private readonly ILeagueSpectatorApiClient _spectatorApiClient;
            private readonly ILeagueApiClient _leagueApiClient;
            private readonly IRecordingManager _recordingManager;

            private readonly GameRecorder _gameRecorder;

            private Timer _timer;
            #endregion

            #region Properties
            /// <summary>
            /// Gets or sets the recording.
            /// </summary>
            public RiotRecording Recording { get; private set; }
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="SingleGameRecorder"/> class.
            /// </summary>
            /// <param name="spectatorApiClient">The spectator API client.</param>
            /// <param name="leagueApiClient">The league API client.</param>
            /// <param name="recordingManager">The recording manager.</param>
            /// <param name="gameRecorder">The game recorder.</param>
            /// <param name="recording">The recording.</param>
            public SingleGameRecorder([NotNull]ILeagueSpectatorApiClient spectatorApiClient, [NotNull]ILeagueApiClient leagueApiClient, [NotNull]IRecordingManager recordingManager, [NotNull]GameRecorder gameRecorder, [NotNull]RiotRecording recording)
            {
                Guard.AgainstNullArgument("spectatorApiClient", spectatorApiClient);
                Guard.AgainstNullArgument("leagueApiClient", leagueApiClient);
                Guard.AgainstNullArgument("recordingManager", recordingManager);
                Guard.AgainstNullArgument("gameRecorder", gameRecorder);

                this._spectatorApiClient = spectatorApiClient;
                this._leagueApiClient = leagueApiClient;
                this._recordingManager = recordingManager;
                this._gameRecorder = gameRecorder;

                this.Recording = recording;

                this._timer = new Timer();
                this._timer.Interval = TimeSpan.FromSeconds(30).TotalMilliseconds;
                this._timer.Elapsed += TimerOnElapsed;

                this._timer.Start();
            }
            #endregion

            #region Private Methods
            /// <summary>
            /// Timers the on elapsed.
            /// </summary>
            /// <param name="sender">The sender.</param>
            /// <param name="elapsedEventArgs">The <see cref="ElapsedEventArgs"/> instance containing the event data.</param>
            private async void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
            {
                if (this._timer != null)
                    this._timer.Stop();

                await this.DownloadSpectatorInfo().ConfigureAwait(false);
                await this.SaveGameIfFinished().ConfigureAwait(false);

                if (this._timer != null)
                    this._timer.Start();
            }
            /// <summary>
            /// Downloads the spectator information.
            /// </summary>
            private async Task DownloadSpectatorInfo()
            {
                if (this.Recording.LeagueVersion == null)
                {
                    LogTo.Debug("Updating league version of game {0} {1}.", this.Recording.Game.Region, this.Recording.Game.GameId);

                    Result<Version> leagueVersionResult = await this._leagueApiClient.GetLeagueVersion(Region.FromString(this.Recording.Game.Region)).ConfigureAwait(false);

                    if (leagueVersionResult.IsSuccess)
                    {
                        LogTo.Debug("The current league version for game {0} {1} is {2}.", this.Recording.Game.Region, this.Recording.Game.GameId, leagueVersionResult.Data);
                        this.Recording.LeagueVersion = leagueVersionResult.Data;
                    }
                }

                if (this.Recording.SpectatorVersion == null)
                {
                    LogTo.Debug("Updating spectator version of game {0} {1}.", this.Recording.Game.Region, this.Recording.Game.GameId);

                    Result<Version> spectatorVersionResult = await this._spectatorApiClient.GetSpectatorVersion(Region.FromString(this.Recording.Game.Region)).ConfigureAwait(false);

                    if (spectatorVersionResult.IsSuccess)
                    {
                        LogTo.Debug("The current spectator version for game {0} {1} is {2}.", this.Recording.Game.Region, this.Recording.Game.GameId, spectatorVersionResult.Data);
                        this.Recording.SpectatorVersion = spectatorVersionResult.Data;
                    }
                }

                Result<RiotLastGameInfo> lastGameInfo = await this._spectatorApiClient.GetLastGameInfo(Region.FromString(this.Recording.Game.Region), this.Recording.Game.GameId).ConfigureAwait(false);

                if (lastGameInfo.IsError)
                {
                    LogTo.Error("Error while retrieving the last game info for game {0} {1}. Throwing it away.", this.Recording.Game.Region, this.Recording.Game.GameId);

                    this._gameRecorder.RemoveRecording(this.Recording);
                    return;
                }

                this.Recording.GameInfo = lastGameInfo.Data;

                int maxRecordedChunkId = this.Recording.Chunks.Any()
                    ? this.Recording.Chunks.Max(f => f.Id)
                    : 0;

                LogTo.Debug("Downloading chunks {0} to {1} for game {2} {3}.", maxRecordedChunkId + 1, lastGameInfo.Data.CurrentChunkId, this.Recording.Game.Region, this.Recording.Game.GameId);

                while (maxRecordedChunkId < lastGameInfo.Data.CurrentChunkId)
                {
                    maxRecordedChunkId++;

                    LogTo.Debug("Downloading chunk {0} for game {1} {2}.", maxRecordedChunkId, this.Recording.Game.Region, this.Recording.Game.GameId);

                    Result<RiotChunk> chunkResult = await this._spectatorApiClient.GetChunk(Region.FromString(this.Recording.Game.Region), this.Recording.Game.GameId, maxRecordedChunkId).ConfigureAwait(false);

                    if (chunkResult.IsError)
                    {
                        LogTo.Error("Error while downloading chunk {0} for game {1} {2}.", maxRecordedChunkId, this.Recording.Game.Region, this.Recording.Game.GameId);
                        continue;
                    }

                    this.Recording.Chunks.Add(chunkResult.Data);
                }

                int maxRecordedKeyFrameId = this.Recording.KeyFrames.Any()
                    ? this.Recording.KeyFrames.Max(f => f.Id)
                    : 0;

                LogTo.Debug("Downloading keyframes {0} to {1} for game {2} {3}.", maxRecordedKeyFrameId + 1, lastGameInfo.Data.CurrentKeyFrameId, this.Recording.Game.Region, this.Recording.Game.GameId);
                
                while (maxRecordedKeyFrameId < lastGameInfo.Data.CurrentKeyFrameId)
                {
                    maxRecordedKeyFrameId++;

                    LogTo.Debug("Downloading keyframe {0} for game {1} {2}.", maxRecordedKeyFrameId, this.Recording.Game.Region, this.Recording.Game.GameId);

                    Result<RiotKeyFrame> keyFrameResult = await this._spectatorApiClient.GetKeyFrame(Region.FromString(this.Recording.Game.Region), this.Recording.Game.GameId, maxRecordedKeyFrameId).ConfigureAwait(false);

                    if (keyFrameResult.IsError)
                    {
                        LogTo.Error("Error while downloading keyframe {0} for game {1} {2}.", maxRecordedKeyFrameId, this.Recording.Game.Region, this.Recording.Game.GameId);
                        continue;
                    }

                    this.Recording.KeyFrames.Add(keyFrameResult.Data);
                }

                if (lastGameInfo.Data.EndGameChunkId > 0)
                {
                    LogTo.Debug("The game {0} {1} has ended in chunk {2}. Downloading game meta data now.", this.Recording.Game.Region, this.Recording.Game.GameId, lastGameInfo.Data.EndGameChunkId);

                    Result<RiotGameMetaData> metaDataResult = await this._spectatorApiClient.GetGameMetaData(Region.FromString(this.Recording.Game.Region), this.Recording.Game.GameId).ConfigureAwait(false);

                    if (metaDataResult.IsSuccess)
                    {
                        this.Recording.GameMetaData = metaDataResult.Data;
                    }
                }
            }
            /// <summary>
            /// Saves the game if it finished.
            /// </summary>
            private async Task SaveGameIfFinished()
            {
                bool hasFinishedRecording =
                    this.Recording.GameMetaData != null &&
                    this.Recording.Chunks.Any(d => d.Id == this.Recording.GameMetaData.EndGameChunkId) &&
                    this.Recording.KeyFrames.Any(d => d.Id == this.Recording.GameMetaData.EndGameKeyFrameId);

                if (hasFinishedRecording)
                {
                    bool isComplete =
                        this.Recording.Chunks.Count == this.Recording.GameMetaData.EndGameChunkId &&
                        this.Recording.KeyFrames.Count == this.Recording.GameMetaData.EndGameKeyFrameId;

                    if (isComplete)
                    {
                        LogTo.Info("The recording for game {0} {1} has finished. Saving it into the database.", this.Recording.Game.Region, this.Recording.Game.GameId);
                        await this._recordingManager.SaveGameRecordingAsync(this.Recording).ConfigureAwait(false);

                        this._gameRecorder.RemoveRecording(this.Recording);
                    }
                    else
                    {
                        LogTo.Info("Sadly there are chunks or keyframes missing for game {0} {1}. Will not save it into the database. Chunks: {2} of {3}. KeyFrames: {4} of {5}.", this.Recording.Game.Region, this.Recording.Game.GameId, this.Recording.Chunks.Count, this.Recording.GameMetaData.EndGameChunkId, this.Recording.KeyFrames.Count, this.Recording.GameMetaData.EndGameKeyFrameId);
                        this._gameRecorder.RemoveRecording(this.Recording);
                    }
                }
            }
            #endregion

            #region Implementation of IDisposable
            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                this._timer.Dispose();
                this._timer = null;
            }
            #endregion
        }
        #endregion
    }
}