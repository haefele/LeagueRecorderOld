using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using LeagueRecorder.Server.Contracts.League;
using LeagueRecorder.Server.Contracts.League.Api;
using LeagueRecorder.Server.Contracts.League.Recording;
using LeagueRecorder.Server.Contracts.League.Storage;
using LeagueRecorder.Server.Infrastructure.League;
using LeagueRecorder.Server.Infrastructure.League.Api;
using LeagueRecorder.Server.Infrastructure.League.Recording;
using LeagueRecorder.Server.Infrastructure.League.Storage;

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
                Component.For<IRecordingStorage>().ImplementedBy<RecordingStorage>().LifestyleSingleton()
            );
        }
    }
}