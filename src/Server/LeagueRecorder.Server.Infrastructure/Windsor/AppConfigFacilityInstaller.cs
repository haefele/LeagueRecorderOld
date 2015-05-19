using AppConfigFacility;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace LeagueRecorder.Server.Infrastructure.Windsor
{
    public class AppConfigFacilityInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.AddFacility<AppConfigFacility.AppConfigFacility>();
        }
    }
}