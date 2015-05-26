using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Timers;
using Anotar.NLog;
using JetBrains.Annotations;
using LeagueRecorder.Server.Contracts.LeagueApi;
using LeagueRecorder.Server.Contracts.Recording;
using LeagueRecorder.Server.Infrastructure.Extensions;
using LeagueRecorder.Server.Infrastructure.Raven.Indexes;
using LeagueRecorder.Shared;
using LeagueRecorder.Shared.Entities;
using LeagueRecorder.Shared.League.Api;
using LeagueRecorder.Shared.Results;
using LiteGuard;
using Raven.Client;
using Raven.Client.Linq;

namespace LeagueRecorder.Server.Infrastructure.Recording
{
    public class SummonerInGameFinder : ISummonersInGameFinder
    {
        #region Fields
        private readonly IConfig _config;
        private readonly IDocumentStore _documentStore;
        private readonly ILeagueApiClient _leagueApiClient;
        private readonly IGameRecorderSupervisor _gameRecorderSupervisor;

        private readonly Dictionary<Region, DateTime> _regionIsUnavailableToLastTimeChecked; 

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
        /// <param name="gameRecorderSupervisor">The recording manager.</param>
        public SummonerInGameFinder([NotNull]IConfig config, [NotNull]IDocumentStore documentStore, [NotNull]ILeagueApiClient leagueApiClient, [NotNull]IGameRecorderSupervisor gameRecorderSupervisor)
        {
            Guard.AgainstNullArgument("config", config);
            Guard.AgainstNullArgument("documentStore", documentStore);
            Guard.AgainstNullArgument("leagueApiClient", leagueApiClient);
            Guard.AgainstNullArgument("GameRecorderSupervisor", gameRecorderSupervisor);

            this._config = config;
            this._documentStore = documentStore;
            this._leagueApiClient = leagueApiClient;
            this._gameRecorderSupervisor = gameRecorderSupervisor;

            this._regionIsUnavailableToLastTimeChecked = new Dictionary<Region, DateTime>();
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
            this._timer.Interval = TimeSpan.FromSeconds(this._config.IntervalToCheckForSummonersThatAreIngameInSeconds).TotalMilliseconds;
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
                    string[] regions = this.GetRegionsThatAreAvailable();

                    //Order the results here so we always get the "oldest" summoners
                    IList<Summoner> summoners = await session.Query<Summoner, SummonersForQuery>()
                        .Where(f => f.LastCheckIfInGameDate <= DateTimeOffset.Now.AddSeconds(-this._config.IntervalToCheckIfOneSummonerIsIngame))
                        .Where(f => f.Region.In(regions))
                        .OrderBy(f => f.LastCheckIfInGameDate)
                        .Take(this._config.CountOfSummonersToCheckIfIngame)
                        .ToListAsync();

                    foreach (var summoner in summoners)
                    {
                        Result<RiotSpectatorGameInfo> currentGameResult = await this._leagueApiClient.GetCurrentGameAsync(Region.FromString(summoner.Region), summoner.SummonerId);

                        if (currentGameResult.IsSuccess || currentGameResult.IsWarning)
                        {
                            summoner.LastCheckIfInGameDate = DateTimeOffset.Now;
                        }

                        if (currentGameResult.GetStatusCode() == HttpStatusCode.ServiceUnavailable)
                        {
                            this.RememberRegionIsUnavailable(summoner);
                            break;
                        }

                        if (currentGameResult.IsSuccess)
                        {
                            LogTo.Debug("The summoner {0} ({1} {2}) is currently in game {3} {4}.", summoner.SummonerName, summoner.Region, summoner.SummonerId, currentGameResult.Data.Region, currentGameResult.Data.GameId);

                            this._gameRecorderSupervisor.Record(currentGameResult.Data);
                        }
                        else
                        {
                            LogTo.Debug("The summoner {0} ({1} {2}) is currently NOT in game: {3}", summoner.SummonerName, summoner.Region, summoner.SummonerId, currentGameResult.Message);
                        }
                    }

                    await session.SaveChangesAsync();
                }
            }
            catch (Exception exception)
            {
                LogTo.ErrorException("Exception while checking if summoners are ingame.", exception);
            }
            finally
            {
                this._timer.Start();
            }
        }
        private void RememberRegionIsUnavailable(Summoner summoner)
        {
            LogTo.Debug("The region {0} is unavailable. Ignoring it for {1} seconds.", summoner.Region, this._config.DurationRegionsAreMarkedAsUnavailableInSeconds);

            this._regionIsUnavailableToLastTimeChecked[Region.FromString(summoner.Region)] = DateTime.Now;
        }

        private string[] GetRegionsThatAreAvailable()
        {
            var outdatedRegions = this._regionIsUnavailableToLastTimeChecked
                .Where(f => f.Value.AddSeconds(this._config.DurationRegionsAreMarkedAsUnavailableInSeconds) <= DateTime.Now)
                .ToList();

            foreach (KeyValuePair<Region, DateTime> outdatedRegion in outdatedRegions)
            {
                this._regionIsUnavailableToLastTimeChecked.Remove(outdatedRegion.Key);
            }

            LogTo.Debug("The regions {0} are unavailable. Ignoring players from them.", string.Join(", ", this._regionIsUnavailableToLastTimeChecked.Keys));

            return Region.All
                .Where(f => this._regionIsUnavailableToLastTimeChecked.Keys.Contains(f) == false)
                .Select(f => f.ToString())
                .ToArray();
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