using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Anotar.NLog;
using JetBrains.Annotations;
using LeagueRecorder.Server.Contracts.League;
using LeagueRecorder.Server.Infrastructure.Raven.Indexes;
using LeagueRecorder.Shared.Entities;
using LeagueRecorder.Shared.League;
using LeagueRecorder.Shared.Results;
using LiteGuard;
using Raven.Abstractions.Extensions;
using Raven.Client;
using Raven.Client.Linq;

namespace LeagueRecorder.Server.Infrastructure.League
{
    public class SummonerInGameFinder : ISummonersInGameFinder
    {
        #region Fields
        private readonly IConfig _config;
        private readonly IDocumentStore _documentStore;
        private readonly ILeagueApiClient _leagueApiClient;
        private readonly IRecordingManager _recordingManager;

        private readonly object _isStartedLock = new object();

        private Timer _timer;
        #endregion
        
        #region Properties
        /// <summary>
        /// Gets a value indicating whether this instance started looking for summoners that are in game.
        /// </summary>
        public bool IsStarted { get; private set; }
        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SummonerInGameFinder"/> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <param name="documentStore">The document store.</param>
        /// <param name="leagueApiClient">The league API client.</param>
        /// <param name="recordingManager">The recording manager.</param>
        public SummonerInGameFinder([NotNull]IConfig config, [NotNull]IDocumentStore documentStore, [NotNull]ILeagueApiClient leagueApiClient, [NotNull]IRecordingManager recordingManager)
        {
            Guard.AgainstNullArgument("config", config);
            Guard.AgainstNullArgument("documentStore", documentStore);
            Guard.AgainstNullArgument("leagueApiClient", leagueApiClient);
            Guard.AgainstNullArgument("recordingManager", recordingManager);

            this._config = config;
            this._documentStore = documentStore;
            this._leagueApiClient = leagueApiClient;
            this._recordingManager = recordingManager;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Instructs this instance to start looking for summoners that are in game.
        /// </summary>
        public void Start()
        {
            lock(this._isStartedLock)
            {
                if (this.IsStarted)
                    return;

                this.IsStarted = true;
            }

            this._timer = new Timer();
            this._timer.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
            this._timer.Elapsed += this.TimerOnElapsed;

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
            try
            {
                this._timer.Stop();

                using (var session = this._documentStore.OpenAsyncSession())
                {
                    //Order the results here so we always get the "oldest" summoners
                    IList<Summoner> summoners = await session.Query<Summoner, SummonersForQuery>()
                        .Where(f => f.LastCheckIfInGameDate <= DateTimeOffset.Now.AddSeconds(-this._config.IntervalToCheckForSummonersThatAreIngameInSeconds))
                        .OrderBy(f => f.LastCheckIfInGameDate)
                        .Take(50)
                        .ToListAsync();

                    foreach(var summoner in summoners)
                    {
                        Result<RiotSpectatorGameInfo> currentGameResult = await this._leagueApiClient.GetCurrentGameAsync(Region.FromString(summoner.Region), summoner.SummonerId);

                        if (currentGameResult.IsSuccess || currentGameResult.IsWarning)
                        {
                            summoner.LastCheckIfInGameDate = DateTimeOffset.Now;   
                        }

                        if (currentGameResult.IsSuccess)
                        {
                            LogTo.Debug("The summoner {0} ({1} {2}) is currently in game {3} {4}.", summoner.SummonerName, summoner.Region, summoner.SummonerId, currentGameResult.Data.Region, currentGameResult.Data.GameId);
                            
                            this._recordingManager.Record(currentGameResult.Data);
                        }
                        else
                        {
                            LogTo.Debug("The summoner {0} ({1} {2}) is currently NOT in game: {3}.", summoner.SummonerName, summoner.Region, summoner.SummonerId, currentGameResult.Message);
                        }
                    }

                    await session.SaveChangesAsync();
                }

                this._timer.Start();
            }
            catch (Exception exception)
            {
                LogTo.ErrorException("Exception while checking if summoners are ingame.", exception);
            }
        }
        #endregion

        #region Implementation of IDisposable
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (this._timer != null)
            { 
                this._timer.Stop();
                this._timer.Dispose();
            }
        }
        #endregion
    }
}