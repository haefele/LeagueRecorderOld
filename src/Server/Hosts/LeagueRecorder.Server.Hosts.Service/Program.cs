using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace LeagueRecorder.Server.Hosts.Service
{
    public class Program
    {
        public static void Main()
        {
            HostFactory.Run(f =>
            {
                f.Service<LeagueRecorderService>(d =>
                {
                    d.ConstructUsing(x => new LeagueRecorderService());
                    d.WhenStarted(x => x.Start());
                    d.WhenStopped(x => x.Stop());
                });

                f.RunAsLocalSystem();
                f.StartAutomatically();

                f.SetDescription("The LeagueRecorder HTTP Api.");
                f.SetDisplayName("LeagueRecorder HTTP Api");
                f.SetServiceName("LeagueRecorderHTTPApi");
            });
        }
    }
}
