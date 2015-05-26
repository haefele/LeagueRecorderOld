using System;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.Http.ExceptionHandling;
using Anotar.NLog;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using LeagueRecorder.Server.Contracts.League;
using LeagueRecorder.Server.Contracts.Recording;
using LeagueRecorder.Server.Infrastructure.Api.Configuration;
using LeagueRecorder.Server.Infrastructure.Windsor;
using LeagueRecorder.Shared.Entities;
using Microsoft.AspNet.WebApi.MessageHandlers.Compression;
using Microsoft.AspNet.WebApi.MessageHandlers.Compression.Compressors;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Owin;
using Raven.Client;
using Raven.Client.Indexes;
using Raven.Client.Linq;

namespace LeagueRecorder.Server.Infrastructure
{
    public class Bootstrapper : IDisposable
    {
        #region Fields
        private IDisposable _webApp;
        #endregion

        #region Methods
        /// <summary>
        /// Starts the league recorder server.
        /// </summary>
        public void Start()
        {
            LogTo.Debug("Creating the windsor container.");

            var container = new WindsorContainer();
            container.Install(FromAssembly.This());

            LogTo.Debug("Creating RavenDB indexes.");

            var documentStore = container.Resolve<IDocumentStore>();
            IndexCreation.CreateIndexes(this.GetType().Assembly, documentStore);

            LogTo.Debug("Ensuring system configuration document exists.");

            using (var documentSession = documentStore.OpenSession())
            {
                var config = documentSession.Load<GlobalConfiguration>(GlobalConfiguration.CreateId());
                
                if (config == null)
                {
                    config = new GlobalConfiguration
                    {
                        Id = GlobalConfiguration.CreateId()
                    };
                    documentSession.Store(config);

                    documentSession.SaveChanges();
                }
            }

            LogTo.Debug("Starting the http api.");

            var startOptions = new StartOptions(container.Resolve<IConfig>().Url);
            this._webApp = WebApp.Start(startOptions, f => this.StartHttpApi(f, container));

            if (container.Resolve<IConfig>().RecordGames)
            {
                LogTo.Debug("Starting the summoners in game finder.");

                var summonersInGameFinder = container.Resolve<ISummonersInGameFinder>();
                summonersInGameFinder.Start();
            }

            LogTo.Debug("Finished startup.");
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Configures the web application.
        /// </summary>
        /// <param name="appBuilder">The application builder.</param>
        /// <param name="container">The container.</param>
        private void StartHttpApi(IAppBuilder appBuilder, WindsorContainer container)
        {
            this.UseCors(appBuilder);
            this.UseWebApi(appBuilder, container);
        }
        /// <summary>
        /// Instructs the http api to allow all CORS requests.
        /// </summary>
        /// <param name="appBuilder">The application builder.</param>
        private void UseCors(IAppBuilder appBuilder)
        {
            appBuilder.UseCors(CorsOptions.AllowAll);
        }
        /// <summary>
        /// Instructs the http api to use ASP.NET WebAPI. 
        /// </summary>
        /// <param name="appBuilder">The application builder.</param>
        /// <param name="container">The container.</param>
        private void UseWebApi(IAppBuilder appBuilder, WindsorContainer container)
        {
            var httpConfiguration = new HttpConfiguration();

            this.ConfigureWindsor(httpConfiguration, container);
            this.ConfigureFilters(httpConfiguration, container);
            this.ConfigureMessageHandlers(httpConfiguration, container);
            this.ConfigureServices(httpConfiguration, container);
            this.ConfigureRoutes(httpConfiguration, container);
            
            appBuilder.UseWebApi(httpConfiguration);
        }
        /// <summary>
        /// Configures the castle windsor container.
        /// </summary>
        /// <param name="httpConfiguration">The HTTP configuration.</param>
        /// <param name="container">The container.</param>
        private void ConfigureWindsor(HttpConfiguration httpConfiguration, WindsorContainer container)
        {
            httpConfiguration.DependencyResolver = new WindsorResolver(container);
        }
        /// <summary>
        /// Configures the default filters.
        /// </summary>
        /// <param name="httpConfiguration">The HTTP configuration.</param>
        /// <param name="container">The container.</param>
        private void ConfigureFilters(HttpConfiguration httpConfiguration, WindsorContainer container)
        {
        }
        /// <summary>
        /// Configures the default message handlers.
        /// </summary>
        /// <param name="httpConfiguration">The HTTP configuration.</param>
        /// <param name="container">The container.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void ConfigureMessageHandlers(HttpConfiguration httpConfiguration, WindsorContainer container)
        {
            if (container.Resolve<IConfig>().CompressResponses)
            {
                httpConfiguration.MessageHandlers.Add(new ServerCompressionHandler(new GZipCompressor(), new DeflateCompressor()));
            }
        }
        /// <summary>
        /// Configures the ASP.NET WebAPI services.
        /// </summary>
        /// <param name="httpConfiguration">The HTTP configuration.</param>
        /// <param name="container">The container.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void ConfigureServices(HttpConfiguration httpConfiguration, WindsorContainer container)
        {
            LogTo.Debug("Replacing some WebAPI services. (IAssembliesResolver, IExceptionHandler, IExceptionLogger)");

            httpConfiguration.Services.Replace(typeof(IAssembliesResolver), new LeagueRecorderAssembliesResolver());
            httpConfiguration.Services.Replace(typeof(IExceptionHandler), new LeagueRecorderExceptionHandler());
            httpConfiguration.Services.Replace(typeof(IExceptionLogger), new LeagueRecorderExceptionLogger());
        }
        /// <summary>
        /// Configures the routes.
        /// </summary>
        /// <param name="httpConfiguration">The HTTP configuration.</param>
        /// <param name="container">The container.</param>
        private void ConfigureRoutes(HttpConfiguration httpConfiguration, WindsorContainer container)
        {
            httpConfiguration.MapHttpAttributeRoutes();

            httpConfiguration.Routes.MapHttpRoute(
                name: "DefaultRoute",
                routeTemplate: "{*uri}",
                defaults: new {controller = "Default", uri = RouteParameter.Optional});
        }
        #endregion

        #region Implementation of IDisposable
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (this._webApp != null)
                this._webApp.Dispose();
        }
        #endregion
    }
}