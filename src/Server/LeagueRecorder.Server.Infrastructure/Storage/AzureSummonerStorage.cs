using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;
using FluentNHibernate.Utils;
using LeagueRecorder.Server.Contracts.Storage;
using LeagueRecorder.Server.Infrastructure.Extensions;
using LeagueRecorder.Shared.Entities;
using LeagueRecorder.Shared.Results;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Table.Queryable;
using NHibernate;
using NHibernate.Linq;

namespace LeagueRecorder.Server.Infrastructure.Storage
{
    public class AzureSummonerStorage : ISummonerStorage
    {
        private readonly ISessionFactory _sessionFactory;
        private readonly IConfig _config;

        public AzureSummonerStorage(ISessionFactory sessionFactory, IConfig config)
        {
            this._sessionFactory = sessionFactory;
            this._config = config;
        }

        public Task<Result> SaveSummonerAsync(Summoner summonerToStore)
        {
            return Task.FromResult(Result.Create(() =>
            {
                using (var session = this._sessionFactory.OpenSession())
                using (var transaction = session.BeginTransaction())
                { 
                    var entity = new SummonerEntity()
                    {
                        Id = this.CreateSummonerId(summonerToStore.Region, summonerToStore.SummonerId),
                        Region = summonerToStore.Region,
                        SummonerId = summonerToStore.SummonerId,
                        LastCheckIfIngameDate = summonerToStore.LastCheckIfInGameDate.UtcDateTime,
                        SummonerName = summonerToStore.SummonerName
                    };

                    session.SaveOrUpdate(entity);

                    transaction.Commit();
                }
            }));
        }

        public Task<Result<IList<Summoner>>> GetSummonersForInGameCheckAsync(string[] regions)
        {
            return Task.FromResult(Result.Create(() =>
            {
                using(var session = this._sessionFactory.OpenSession())
                {
                    var referenceDate = DateTime.UtcNow.AddSeconds(-this._config.IntervalToCheckIfOneSummonerIsIngame);
                
                    List<SummonerEntity> result = session.Query<SummonerEntity>()
                        .Where(f => regions.Contains(f.Region) && f.LastCheckIfIngameDate <= referenceDate)
                        .OrderBy(f => f.LastCheckIfIngameDate)
                        .Take(this._config.CountOfSummonersToCheckIfIngame)
                        .ToList();
                
                    IList<Summoner> converted = result
                        .Select(f => new Summoner
                            {
                                LastCheckIfInGameDate = f.LastCheckIfIngameDate, 
                                Region = f.Region, 
                                SummonerId = f.SummonerId, 
                                SummonerName = f.SummonerName
                            })
                        .ToList();

                    return converted;
                }
            }));
        }

        private string CreateSummonerId(string region, long summonerId)
        {
            return string.Format("{0}/{1}", region, summonerId);
        }
        
        #region Internal
        private class SummonerEntity
        {
            public virtual string Id { get; set; }
            public virtual string Region { get; set; }
            public virtual long SummonerId { get; set; }
            public virtual string SummonerName { get; set; }
            public virtual DateTime LastCheckIfIngameDate { get; set; }
        }
        private class SummonerEntityMaps : ClassMap<SummonerEntity>
        {
            public SummonerEntityMaps()
            {
                Table("Summoners");

                Id(f => f.Id).GeneratedBy.Assigned().Length(200);

                Map(f => f.Region).Not.Nullable().MaxLength();
                Map(f => f.SummonerId).Not.Nullable();
                Map(f => f.SummonerName).Not.Nullable().MaxLength();
                Map(f => f.LastCheckIfIngameDate).Not.Nullable();
            }
        }
        #endregion
    }
}