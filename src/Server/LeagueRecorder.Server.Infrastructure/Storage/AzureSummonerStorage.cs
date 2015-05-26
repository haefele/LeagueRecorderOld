using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeagueRecorder.Server.Contracts.Storage;
using LeagueRecorder.Shared.Entities;
using LeagueRecorder.Shared.Results;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Table.Queryable;

namespace LeagueRecorder.Server.Infrastructure.Storage
{
    public class AzureSummonerStorage : ISummonerStorage
    {
        private readonly CloudTableClient _tableClient;
        private readonly IConfig _config;

        public AzureSummonerStorage(CloudTableClient tableClient, IConfig config)
        {
            this._tableClient = tableClient;
            this._config = config;
        }

        public Task<Result> SaveSummonerAsync(Summoner summonerToStore)
        {
            return Result.CreateAsync(async () =>
            {
                var tableReference = await this.GetTableReferenceAsync<SummonerEntity>();

                var entity = new SummonerEntity(summonerToStore.Region, summonerToStore.SummonerId.ToString())
                {
                    LastCheckIfIngameDate = summonerToStore.LastCheckIfInGameDate.UtcDateTime,
                    SummonerName = summonerToStore.SummonerName
                };

                await tableReference.ExecuteAsync(TableOperation.InsertOrReplace(entity));
            });
        }

        public Task<Result<IList<Summoner>>> GetSummonersForInGameCheckAsync()
        {
            return Result.CreateAsync(async () =>
            {
                var tableReference = await this.GetTableReferenceAsync<SummonerEntity>();

                var referenceDate = DateTime.UtcNow.AddSeconds(-this._config.IntervalToCheckForSummonersThatAreIngameInSeconds);
                
                List<SummonerEntity> result = tableReference.CreateQuery<SummonerEntity>()
                    .Where(f => f.LastCheckIfIngameDate <= referenceDate)
                    .Take(this._config.CountOfSummonersToCheckIfIngame)
                    .ToList();
                
                IList<Summoner> converted = result
                    .Select(f => new Summoner
                        {
                            LastCheckIfInGameDate = f.LastCheckIfIngameDate, 
                            Region = f.PartitionKey, 
                            SummonerId = long.Parse(f.RowKey), 
                            SummonerName = f.SummonerName
                        })
                    .ToList();

                return converted;
            });
            
            //await session.Query<Summoner, SummonersForQuery>()
            //    .Where(f => f.LastCheckIfInGameDate <= DateTimeOffset.Now.AddSeconds(-this._config.IntervalToCheckIfOneSummonerIsIngame))
            //    .Where(f => f.Region.In(regions))
            //    .OrderBy(f => f.LastCheckIfInGameDate)
            //    .Take(this._config.CountOfSummonersToCheckIfIngame)
            //    .ToListAsync();
        }

        #region Private Methods
        private async Task<CloudTable> GetTableReferenceAsync<T>() 
            where T : TableEntity
        {
            var tableReference = this._tableClient.GetTableReference(typeof (T).Name);
            await tableReference.CreateIfNotExistsAsync();

            return tableReference;
        }
        #endregion

        #region Internal
        private class SummonerEntity : TableEntity
        {
            public SummonerEntity(string partitionKey, string rowKey)
                : base(partitionKey, rowKey)
            {
            }

            public SummonerEntity()
            {
            }

            public string SummonerName { get; set; }
            public DateTime LastCheckIfIngameDate { get; set; }
        }
        #endregion
    }
}