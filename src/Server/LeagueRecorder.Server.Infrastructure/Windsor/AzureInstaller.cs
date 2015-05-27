using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;

namespace LeagueRecorder.Server.Infrastructure.Windsor
{
    public class AzureInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<CloudStorageAccount>().UsingFactoryMethod((kernel, context) => this.CreateStorageAccount(kernel.Resolve<IConfig>())).LifestyleSingleton(),
                Component.For<CloudBlobClient>().UsingFactoryMethod((kernel, context) => this.CreateBlobClient(kernel.Resolve<CloudStorageAccount>())).LifestyleSingleton());
        }

        private CloudStorageAccount CreateStorageAccount(IConfig config)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(config.AzureStorageConnectionString);
            return storageAccount;
        }

        private CloudBlobClient CreateBlobClient(CloudStorageAccount storageAccount)
        {
            return storageAccount.CreateCloudBlobClient();
        }
    }
}