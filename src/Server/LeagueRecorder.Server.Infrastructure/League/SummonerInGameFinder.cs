using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
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
        public SummonerInGameFinder(IConfig config, IDocumentStore documentStore, ILeagueApiClient leagueApiClient)
        {
            Guard.AgainstNullArgument("config", config);
            Guard.AgainstNullArgument("documentStore", documentStore);
            Guard.AgainstNullArgument("leagueApiClient", leagueApiClient);

            this._config = config;
            this._documentStore = documentStore;
            this._leagueApiClient = leagueApiClient;
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

                    if (currentGameResult.IsSuccess)
                    {
                        //TODO: Spectate the Game
                    }

                    summoner.LastCheckIfInGameDate = DateTimeOffset.Now;
                }

                await session.SaveChangesAsync();
            }

            this._timer.Start();
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