﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Anotar.NLog;
using JetBrains.Annotations;
using LeagueRecorder.Server.Contracts.League;
using LeagueRecorder.Shared.League;
using LeagueRecorder.Shared.Results;
using LiteGuard;

namespace LeagueRecorder.Server.Infrastructure.League
{
    public class GameRecorder : IGameRecorder, IDisposable
    {
        #region Fields
        private readonly ILeagueSpectatorApiClient _spectatorApiClient;
        private readonly ILeagueApiClient _leagueApiClient;
        private readonly IRecordingManager _recordingManager;

        private readonly Timer _timer;
        private readonly ConcurrentDictionary<RiotRecording, object> _recordings;
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

            this._timer = new Timer();
            this._timer.Interval = TimeSpan.FromSeconds(30).TotalMilliseconds;
            this._timer.Elapsed += TimerOnElapsed;

            this._timer.Start();

            this._recordings = new ConcurrentDictionary<RiotRecording, object>();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Records the specified game information.
        /// </summary>
        /// <param name="gameInfo">The game information.</param>
        public void Record(RiotSpectatorGameInfo gameInfo)
        {
            LogTo.Debug("Recording game {0} {1}.", gameInfo.Region, gameInfo.GameId);

            var recording = new RiotRecording(gameInfo);

            if (this._recordings.ContainsKey(recording) == false)
            {
                this._recordings.TryAdd(recording, null);
            }
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
            this._timer.Stop();

            await this.DownloadSpectatorInfo().ConfigureAwait(false);
            await this.SaveFinishedGames().ConfigureAwait(false);

            this._timer.Start();
        }

        private async Task DownloadSpectatorInfo()
        {
            foreach (RiotRecording recording in new List<RiotRecording>(this._recordings.Keys))
            {
                if (recording.LeagueVersion == null)
                {
                    LogTo.Debug("Updating league version of game {0} {1}.", recording.Game.Region, recording.Game.GameId);

                    Result<Version> leagueVersionResult = await this._leagueApiClient.GetLeagueVersion(Region.FromString(recording.Game.Region)).ConfigureAwait(false);

                    if (leagueVersionResult.IsSuccess)
                    {
                        LogTo.Debug("The current league version for game {0} {1} is {2}.", recording.Game.Region, recording.Game.GameId, leagueVersionResult.Data);
                        recording.LeagueVersion = leagueVersionResult.Data;
                    }
                }

                if (recording.SpectatorVersion == null)
                {
                    LogTo.Debug("Updating spectator version of game {0} {1}.", recording.Game.Region, recording.Game.GameId);

                    Result<Version> spectatorVersionResult = await this._spectatorApiClient.GetSpectatorVersion(Region.FromString(recording.Game.Region)).ConfigureAwait(false);

                    if (spectatorVersionResult.IsSuccess)
                    {
                        LogTo.Debug("The current spectator version for game {0} {1} is {2}.", recording.Game.Region, recording.Game.GameId, spectatorVersionResult.Data);
                        recording.SpectatorVersion = spectatorVersionResult.Data;
                    }
                }

                Result<RiotLastGameInfo> lastGameInfo = await this._spectatorApiClient.GetLastGameInfo(Region.FromString(recording.Game.Region), recording.Game.GameId).ConfigureAwait(false);

                if (lastGameInfo.IsError)
                {
                    LogTo.Debug("Error while retrieving the last game info for game {0} {1}. Throwing it away.", recording.Game.Region, recording.Game.GameId);

                    object output;
                    this._recordings.TryRemove(recording, out output);

                    continue;
                }

                recording.GameInfo = lastGameInfo.Data;

                int maxRecordedChunkId = recording.Chunks.Any() 
                    ? recording.Chunks.Max(f => f.Id) 
                    : 0;
                
                LogTo.Debug("The current max chunk-id for game {0} {1} is {2}.", recording.Game.Region, recording.Game.GameId, maxRecordedChunkId);

                while (maxRecordedChunkId < lastGameInfo.Data.CurrentChunkId)
                {
                    maxRecordedChunkId++;

                    LogTo.Debug("Downloading chunk {0} for game {1} {2}.", maxRecordedChunkId, recording.Game.Region, recording.Game.GameId);

                    Result<RiotChunk> chunkResult = await this._spectatorApiClient.GetChunk(Region.FromString(recording.Game.Region), recording.Game.GameId, maxRecordedChunkId).ConfigureAwait(false);

                    if (chunkResult.IsError)
                        continue;

                    recording.Chunks.Add(chunkResult.Data);
                }

                int maxRecordedKeyFrameId = recording.KeyFrames.Any() 
                    ? recording.KeyFrames.Max(f => f.Id) 
                    : 0;

                LogTo.Debug("The current max keyframe-id for game {0} {1} is {2}.", recording.Game.Region, recording.Game.GameId, maxRecordedKeyFrameId);

                while (maxRecordedKeyFrameId < lastGameInfo.Data.CurrentKeyFrameId)
                {
                    maxRecordedKeyFrameId++;

                    LogTo.Debug("Downloading keyframe {0} for game {1} {2}.", maxRecordedKeyFrameId, recording.Game.Region, recording.Game.GameId);

                    Result<RiotKeyFrame> keyFrameResult = await this._spectatorApiClient.GetKeyFrame(Region.FromString(recording.Game.Region), recording.Game.GameId, maxRecordedKeyFrameId).ConfigureAwait(false);

                    if (keyFrameResult.IsError)
                        continue;

                    recording.KeyFrames.Add(keyFrameResult.Data);
                }

                if (lastGameInfo.Data.EndGameChunkId > 0)
                {
                    LogTo.Debug("The game {0} {1} has ended in chunk {2}. Downloading game meta data now.", recording.Game.Region, recording.Game.GameId, lastGameInfo.Data.EndGameChunkId);

                    Result<RiotGameMetaData> metaDataResult = await this._spectatorApiClient.GetGameMetaData(Region.FromString(recording.Game.Region), recording.Game.GameId).ConfigureAwait(false);

                    if (metaDataResult.IsSuccess)
                    {
                        recording.GameMetaData = metaDataResult.Data;
                    }
                }
            }
        }

        private async Task SaveFinishedGames()
        {
            var finishedRecordings = this._recordings.Keys
                .Where(f => f.GameMetaData != null &&
                            f.Chunks.Any(d => d.Id == f.GameMetaData.EndGameChunkId) &&
                            f.KeyFrames.Any(d => d.Id == f.GameMetaData.EndGameKeyFrameId))
                .ToList();

            if (finishedRecordings.Any())
                LogTo.Debug("Saving all finished recordings.");

            foreach (RiotRecording recording in finishedRecordings)
            {
                LogTo.Debug("The recording for game {0} {1} has finished. Saving it into the database.", recording.Game.Region, recording.Game.GameId);

                object output;
                this._recordings.TryRemove(recording, out output);

                await this.SaveGameRecordingIntoDatabase(recording).ConfigureAwait(false);
            }
        }

        private async Task SaveGameRecordingIntoDatabase(RiotRecording recording)
        {
            if (recording.Chunks.Count != recording.GameMetaData.EndGameChunkId ||
                recording.KeyFrames.Count != recording.GameMetaData.EndGameKeyFrameId)
            {
                LogTo.Debug("Sadly there are chunks or keyframes missing for game {0} {1}. Will not save it into the database. Chunks: {2} of {3}. KeyFrames: {4} of {5}.", recording.Game.Region, recording.Game.GameId, recording.Chunks.Count, recording.GameMetaData.EndGameChunkId, recording.KeyFrames.Count, recording.GameMetaData.EndGameKeyFrameId);
                return;
            }

            await this._recordingManager.SaveGameRecordingAsync(recording).ConfigureAwait(false);
        }
        #endregion

        #region Implementation of IDisposable
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this._timer.Dispose();
        }
        #endregion
    }
}