using System.IO;
using System.Threading.Tasks;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using LeagueRecorder.Shared.Entities;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.FileSystem;
using Raven.Database.Config;
using Raven.Server;

namespace LeagueRecorder.Server.Infrastructure.Windsor
{
    public class RavenInstaller : IWindsorInstaller
    {
        #region Implementation of IWindsorInstaller
        /// <summary>
        /// Performs the installation in the <see cref="T:Castle.Windsor.IWindsorContainer" />.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="store">The configuration store.</param>
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<RavenDbServer>().UsingFactoryMethod((kernel, context) => this.CreateRavenDbServer(kernel)).LifestyleSingleton(),
                Component.For<IDocumentStore>().UsingFactoryMethod((kernel, context) => kernel.Resolve<RavenDbServer>().DocumentStore).LifestyleSingleton(),
                Component.For<IAsyncDocumentSession>().UsingFactoryMethod((kernel, context) => kernel.Resolve<IDocumentStore>().OpenAsyncSession()).LifestyleScoped(),
                Component.For<IFilesStore>().UsingFactoryMethod((kernel, context) => kernel.Resolve<RavenDbServer>().FilesStore).LifestyleSingleton(),
                Component.For<IAsyncFilesSession>().UsingFactoryMethod((kernel, context) => kernel.Resolve<IFilesStore>().OpenAsyncSession()).LifestyleScoped());
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Creates the RavenDB server.
        /// </summary>
        private RavenDbServer CreateRavenDbServer(IKernel kernel)
        {
            var config = kernel.Resolve<IConfig>();

            var ravenConfig = new RavenConfiguration
            {
                Port = config.RavenHttpServerPort,

                AssembliesDirectory = Path.Combine(".", "Database", "Assemblies"),
                EmbeddedFilesDirectory = Path.Combine(".", "Database", "Files"),
                DataDirectory = Path.Combine(".", "Database", "Data"),
                CompiledIndexCacheDirectory = Path.Combine(".", "Database", "Raven", "CompiledIndexCache"),
                PluginsDirectory = Path.Combine(".", "Database", "Plugins"),
                MaxSecondsForTaskToWaitForDatabaseToLoad = 20,
            };
            ravenConfig.Settings.Add("Raven/CompiledIndexCacheDirectory", ravenConfig.CompiledIndexCacheDirectory);

            var ravenDbServer = new RavenDbServer(ravenConfig);

            ravenDbServer.Initialize();

            ravenDbServer.DocumentStore.DefaultDatabase = config.RavenName;
            ravenDbServer.DocumentStore.DatabaseCommands.GlobalAdmin.EnsureDatabaseExists(ravenDbServer.DocumentStore.DefaultDatabase);

            ravenDbServer.FilesStore.DefaultFileSystem = config.RavenName;
            ravenDbServer.FilesStore.AsyncFilesCommands.Admin.EnsureFileSystemExistsAsync(ravenDbServer.FilesStore.DefaultFileSystem).Wait();

            if (config.EnableRavenHttpServer)
            {
                ravenDbServer.EnableHttpServer();
            }

            this.CustomizeRavenDocumentStore(ravenDbServer.DocumentStore);

            return ravenDbServer;
        }
        /// <summary>
        /// Customizes the raven database.
        /// </summary>
        /// <param name="documentStore">The document store.</param>
        private void CustomizeRavenDocumentStore(DocumentStore documentStore)
        {
            documentStore.Conventions.RegisterAsyncIdConvention<Summoner>((databaseName, commands, entity) =>
            {
                return Task.FromResult(Summoner.CreateId(entity.Region, entity.SummonerId));
            });
        }
        #endregion
    }
}