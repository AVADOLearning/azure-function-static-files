using Microsoft.AspNetCore.Mvc;
using System;

namespace AzureFunctionStaticFiles
{
    /// <summary>
    /// HTTP status code and message result.
    /// </summary>
    public class HttpStatusMessageResult : ObjectResult
    {
        /// <summary>
        /// Get the message for the specified status code.
        /// </summary>
        /// <param name="statusCode">
        /// Status code (e.g. 200, 404).
        /// </param>
        /// <exception cref="NotImplementedException">
        /// Thrown for unknown status codes.
        /// </exception>
        private static string GetStatusCodeMessage(int statusCode)
        {
            switch (statusCode)
            {
                case 404:
                    return "404 Not Found";
                case 500:
                    return "500 Internal Server Error";
                default:
                    throw new NotImplementedException($"Status code {statusCode} not known");
            }
        }

        /// <summary>
        /// Status code.
        /// </summary>
        /// <param name="statusCode">
        /// Status code (e.g. 200, 404).
        /// </param>
        public HttpStatusMessageResult(int statusCode)
                : base(GetStatusCodeMessage(statusCode))
        {
            StatusCode = statusCode;
        }
    }
}
