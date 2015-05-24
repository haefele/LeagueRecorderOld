using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using LeagueRecorder.Server.Contracts.League;
using LeagueRecorder.Server.Infrastructure.League;

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
                Component.For<IGameRecorder>().ImplementedBy<GameRecorder>().LifestyleSingleton(),
                Component.For<IRecordingManager>().ImplementedBy<RecordingManager>().LifestyleSingleton()
            );
        }
    }
}