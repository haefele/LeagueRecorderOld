using System;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;

namespace LeagueRecorder.Server.Infrastructure.Windsor
{
    public class NHibernateInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<ISessionFactory>().UsingFactoryMethod((kernel, context) => this.CreateSessionFactory(kernel.Resolve<IConfig>())).LifestyleSingleton());
        }

        private ISessionFactory CreateSessionFactory(IConfig config)
        {
            var sessionFactory = Fluently.Configure()
                .Database(MsSqlConfiguration.MsSql2012.ConnectionString(config.AzureSqlDatabaseConnectionString))
                .Mappings(f => f.FluentMappings.AddFromAssemblyOf<NHibernateInstaller>())
                .ExposeConfiguration(databaseConfig => this.CreateSchemaIfNeeded(config, databaseConfig))
                .BuildSessionFactory();
                
            return sessionFactory;
        }

        private void CreateSchemaIfNeeded(IConfig config, Configuration databaseConfig)
        {
            //new SchemaExport(databaseConfig)
            //    .Drop(false, true);

            //new SchemaExport(databaseConfig)
            //    .Create(false, true);
        }
    }
}