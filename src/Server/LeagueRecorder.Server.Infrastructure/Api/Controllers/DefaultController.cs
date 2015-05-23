using System.Net;
using System.Net.Http;
using System.Web.Http;
using Anotar.NLog;
using LeagueRecorder.Server.Infrastructure.Extensions;

namespace LeagueRecorder.Server.Infrastructure.Api.Controllers
{
    public class DefaultController : ApiController
    {
        public HttpResponseMessage Get()
        {
            LogTo.Debug("Request to uri: {0}", this.Request.RequestUri);
            return this.Request.GetMessage(HttpStatusCode.NotFound);
        }
    }
}