using System;
using System.Net;
using LeagueRecorder.Shared.Results;

namespace LeagueRecorder.Server.Infrastructure.Extensions
{
    public static class ResultExtensions
    {
        private const string HttpStatusCodeField = "HttpStatusCode";

        public static Result WithStatusCode(this Result result, HttpStatusCode statusCode)
        {
            result.AdditionalData[HttpStatusCodeField] = statusCode;

            return result;
        }

        public static Result<T> WithStatusCode<T>(this Result<T> result, HttpStatusCode statusCode)
        {
            result.AdditionalData[HttpStatusCodeField] = statusCode;

            return result;
        }

        public static HttpStatusCode GetStatusCode(this Result result)
        {
            if (result.AdditionalData.ContainsKey(HttpStatusCodeField) == false)
                throw new ArgumentException();

            return (HttpStatusCode)result.AdditionalData[HttpStatusCodeField];
        }

        public static HttpStatusCode GetStatusCode<T>(this Result<T> result)
        {
            if (result.AdditionalData.ContainsKey(HttpStatusCodeField) == false)
                throw new ArgumentException();

            return (HttpStatusCode)result.AdditionalData[HttpStatusCodeField];
        }
    }
}