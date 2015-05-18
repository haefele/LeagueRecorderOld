using System.Web.Http.ExceptionHandling;
using Anotar.NLog;

namespace LeagueRecorder.Server.Infrastructure.Api.Configuration
{
    public class LeagueRecorderExceptionLogger : ExceptionLogger
    {
        /// <summary>
        /// When overridden in a derived class, logs the exception synchronously.
        /// </summary>
        /// <param name="context">The exception logger context.</param>
        public override void Log(ExceptionLoggerContext context)
        {
            LogTo.ErrorException(string.Format("Unhandled exception. Returning 501 Internal Server Error. Catch block: {0}", context.CatchBlock), context.Exception);
        }
    }
}