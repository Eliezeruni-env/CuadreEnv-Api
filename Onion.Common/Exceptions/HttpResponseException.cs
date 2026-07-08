using System;
using System.Net;
using Onion.Common.Models;

namespace Onion.Common.Exceptions
{
    public class HttpResponseException : Exception
    {
        public Error[] Errors { get; set; } = Array.Empty<Error>();
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.BadRequest;
    }
}
