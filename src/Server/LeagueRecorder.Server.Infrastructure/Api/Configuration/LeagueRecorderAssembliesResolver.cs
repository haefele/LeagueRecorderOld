using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Web.Http.Dispatcher;

namespace LeagueRecorder.Server.Infrastructure.Api.Configuration
{
    public class LeagueRecorderAssembliesResolver : DefaultAssembliesResolver
    {
        public override ICollection<Assembly> GetAssemblies()
        {
            return new Collection<Assembly>
            {
                this.GetType().Assembly
            };
        }
    }
}