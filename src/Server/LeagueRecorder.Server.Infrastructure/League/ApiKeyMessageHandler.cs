using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using LiteGuard;

namespace LeagueRecorder.Server.Infrastructure.League
{
    public class ApiKeyMessageHandler : DelegatingHandler
    {
        private readonly string _apiKey;

        public ApiKeyMessageHandler(string apiKey)
        {
            Guard.AgainstNullArgument("apiKey", apiKey);

            this._apiKey = apiKey;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.RequestUri = this.AppendApiKeyToQueryString(request.RequestUri);

            return base.SendAsync(request, cancellationToken);
        }

        private Uri AppendApiKeyToQueryString(Uri uri)
        {
            var builder = new UriBuilder(uri);

            var query = HttpUtility.ParseQueryString(builder.Query);
            query["api_key"] = _apiKey;

            builder.Query = query.ToString();

            return builder.Uri;
        }
    }
}