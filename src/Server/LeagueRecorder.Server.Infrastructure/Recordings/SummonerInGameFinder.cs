using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using Anotar.NLog;
using JetBrains.Annotations;
using LeagueRecorder.Server.Contracts.LeagueApi;
using LeagueRecorder.Server.Contracts.Recordings;
using LeagueRecorder.Server.Infrastructure.Extensions;
using LeagueRecorder.Shared;
using LeagueRecorder.Shared.Entities;
using LeagueRecorder.Shared.League.Api;
using LeagueRecorder.Shared.Results;
using LiteGuard;
using LeagueRecorder.Server.Contracts.Storage;

namespace LeagueRecorder.Server.Infrastructure.Recordings
{
    public class SummonerInGameFinder : ISummonersInGameFinder
    {
        #region Fields
        private readonly IConfig _config;
        private readonly ISummonerStorage _summonerStorage;
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
        /// <param name="summonerStorage">The summoner storage.</param>
        /// <param name="leagueApiClient">The league API client.</param>
        /// <param name="gameRecorderSupervisor">The recording manager.</param>
        public SummonerInGameFinder([NotNull]IConfig config, [NotNull]ISummonerStorage summonerStorage, [NotNull]ILeagueApiClient leagueApiClient, [NotNull]IGameRecorderSupervisor gameRecorderSupervisor)
        {
            Guard.AgainstNullArgument("config", config);
            Guard.AgainstNullArgument("summonerStorage", summonerStorage);
            Guard.AgainstNullArgument("leagueApiClient", leagueApiClient);
            Guard.AgainstNullArgument("GameRecorderSupervisor", gameRecorderSupervisor);

            this._config = config;
            this._summonerStorage = summonerStorage;
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
                var watch = Stopwatch.StartNew();

                if (this._config.RecordGames == false)
                    return;

                string[] regions = this.GetRegionsThatAreAvailable();
                Result<IList<Summoner>> summoners = await this._summonerStorage.GetSummonersForInGameCheckAsync(regions);

                if (summoners.IsError)
                    return;

                var tasks = summoners.Data.Select(async summoner =>
                {
                    Result<RiotSpectatorGameInfo> currentGameResult = await this._leagueApiClient.GetCurrentGameAsync(Region.FromString(summoner.Region), summoner.SummonerId);

                    if (currentGameResult.IsSuccess)
                    {
                        LogTo.Info("The summoner {0} ({1} {2}) is currently in game {3} {4}.", summoner.SummonerName, summoner.Region, summoner.SummonerId, currentGameResult.Data.Region, currentGameResult.Data.GameId);

                        this._gameRecorderSupervisor.Record(currentGameResult.Data);

                        summoner.NextDateToCheckIfSummonerIsIngame = DateTimeOffset.Now.AddSeconds(this._config.DurationToIgnoreSummonersThatAreIngame);
                        await this._summonerStorage.SaveSummonerAsync(summoner);
                    }
                    else if (currentGameResult.IsWarning)
                    {
                        summoner.NextDateToCheckIfSummonerIsIngame = DateTimeOffset.Now.AddSeconds(this._config.IntervalToCheckIfOneSummonerIsIngame);
                        await this._summonerStorage.SaveSummonerAsync(summoner);
                    }
                    else
                    {
                        LogTo.Error("Error while retrieving the info if summoner {0} ({1} {2}) is ingame: {3}", summoner.SummonerName, summoner.Region, summoner.SummonerId, currentGameResult.Message);
                    }

                    if (currentGameResult.GetStatusCode() == HttpStatusCode.ServiceUnavailable)
                    {
                        this.RememberRegionIsUnavailable(summoner);
                        return;
                    }
                });

                await Task.WhenAll(tasks);

                watch.Stop();
                LogTo.Debug("One timer tick took {0}.", watch.Elapsed);
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