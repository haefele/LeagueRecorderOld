using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using LeagueRecorder.Server.Contracts.LeagueApi;
using LeagueRecorder.Server.Contracts.Recordings;
using LeagueRecorder.Server.Contracts.Storage;
using LeagueRecorder.Server.Infrastructure.LeagueApi;
using LeagueRecorder.Server.Infrastructure.Recordings;
using LeagueRecorder.Server.Infrastructure.Storage;

namespace LeagueRecorder.Server.Infrastructure.Windsor
{
    public class ServicesInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<ILeagueApiClient>().UsingFactoryMethod((kernel, context) => new LeagueApiClient(kernel.Resolve<IConfig>().RiotApiKey)).LifestyleTransient(),
                Component.For<ILeagueSpectatorApiClient>().ImplementedBy<LeagueSpectatorApiClient>().LifestyleTransient(),
                Component.For<ISummonersInGameFinder>().ImplementedBy<SummonerInGameFinder>().LifestyleSingleton(),
                Component.For<IGameRecorderSupervisor>().ImplementedBy<GameRecorderSupervisor>().LifestyleSingleton(),
                Component.For<IRecordingStorage>().ImplementedBy<AzureRecordingStorage>().LifestyleSingleton(),
                Component.For<ISummonerStorage>().ImplementedBy<AzureSummonerStorage>().LifestyleSingleton()
            );
        }
    }
}