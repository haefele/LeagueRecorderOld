using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using LeagueRecorder.Server.Infrastructure.Extensions;
using LiteGuard;

namespace LeagueRecorder.Server.Infrastructure.Api.Configuration
{
    public class LeagueRecorderExceptionHandler : ExceptionHandler
    {
        public override Task HandleAsync(ExceptionHandlerContext context, CancellationToken cancellationToken)
        {
            return base.HandleAsync(context, cancellationToken);
        }

        #region Internal
        private class ExceptionResult : IHttpActionResult
        {
            #region Fields
            private readonly HttpRequestMessage _request;
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="LeagueRecorderExceptionHandler.ExceptionResult"/> class.
            /// </summary>
            /// <param name="request">The request.</param>
            public ExceptionResult(HttpRequestMessage request)
            {
                Guard.AgainstNullArgument("request", request);

                this._request = request;
            }
            #endregion

            #region Methods
            /// <summary>
            /// Creates an <see cref="T:System.Net.Http.HttpResponseMessage" /> asynchronously.
            /// </summary>
            /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
            public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
            {
                var response = this._request.GetMessageWithError(HttpStatusCode.InternalServerError, "Internal server error.");

                return Task.FromResult(response);
            }
            #endregion
        }
        #endregion
    }
}