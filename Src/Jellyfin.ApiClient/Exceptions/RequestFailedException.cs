using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Jellyfin.ApiClient.Exceptions
{
    public class RequestFailedException : Exception
    {
        public HttpStatusCode Status { get; }

        public RequestFailedException(HttpStatusCode status)
            : base()
        {
            this.Status = status;
        }

        public RequestFailedException(HttpStatusCode status, string message) 
            : base(message)
        {
            this.Status = status;
        }

        public RequestFailedException(HttpStatusCode status, string message, Exception innerException)
            : base (message, innerException)
        {
            this.Status = status;
        }
    }
}
