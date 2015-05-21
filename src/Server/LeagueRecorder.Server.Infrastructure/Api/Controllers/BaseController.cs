using System.Web.Http;
using JetBrains.Annotations;
using LiteGuard;
using Raven.Client;
using Raven.Client.Connection.Async;
using Raven.Client.FileSystem;

namespace LeagueRecorder.Server.Infrastructure.Api.Controllers
{
    public class BaseController : ApiController
    {
        public IDocumentStore DocumentStore
        {
            get { return this.DocumentSession.Advanced.DocumentStore; }
        }
        public IAsyncDocumentSession DocumentSession { get; private set; }

        public IFilesStore FilesStore
        {
            get { return this.FilesSession.Advanced.FilesStore; }
        }

        public IAsyncFilesSession FilesSession { get; set; }

        public BaseController([NotNull]IAsyncDocumentSession documentSession, [NotNull]IAsyncFilesSession filesSession)
        {
            Guard.AgainstNullArgument("documentSession", documentSession);
            Guard.AgainstNullArgument("filesSession", filesSession);

            this.DocumentSession = documentSession;
            this.FilesSession = filesSession;
        }
    }
}